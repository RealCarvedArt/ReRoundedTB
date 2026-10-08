using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Windows.Automation;



namespace RoundedTB
{
    class Taskbar
    {
        private static readonly bool isWindows11 = Environment.OSVersion.Version.Build >= 21996;
        private static readonly ConcurrentDictionary<IntPtr, AutomationElement> taskbarFrames = new ConcurrentDictionary<IntPtr, AutomationElement>();

        /// <summary>
        /// Gets the rect of the taskbar's app buttons. On recent Windows 11 builds the legacy MSTaskSwWClass window no longer tracks the
        /// XAML taskbar's icons, so the real buttons are measured via UI Automation, falling back to the legacy window's rect.
        /// </summary>
        /// <returns>
        /// the bounding rect of the app buttons.
        /// </returns>
        public static LocalPInvoke.RECT GetAppListRect(IntPtr taskbarHwnd, IntPtr appListHwnd)
        {
            if (isWindows11 && TryGetXamlAppListRect(taskbarHwnd, out LocalPInvoke.RECT xamlRect))
            {
                return xamlRect;
            }
            LocalPInvoke.GetWindowRect(appListHwnd, out LocalPInvoke.RECT appListRect);
            return appListRect;
        }

        private static bool TryGetXamlAppListRect(IntPtr taskbarHwnd, out LocalPInvoke.RECT rect)
        {
            rect = new LocalPInvoke.RECT();
            try
            {
                if (!taskbarFrames.TryGetValue(taskbarHwnd, out AutomationElement frame))
                {
                    frame = AutomationElement.FromHandle(taskbarHwnd).FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "TaskbarFrame"));
                    if (frame == null)
                    {
                        return false;
                    }
                    taskbarFrames[taskbarHwnd] = frame;
                }

                // Fetch all the properties we need in one cross-process call, as this runs every loop
                CacheRequest cacheRequest = new CacheRequest();
                cacheRequest.Add(AutomationElement.ClassNameProperty);
                cacheRequest.Add(AutomationElement.AutomationIdProperty);
                cacheRequest.Add(AutomationElement.BoundingRectangleProperty);
                cacheRequest.Add(AutomationElement.IsOffscreenProperty);
                AutomationElementCollection children;
                using (cacheRequest.Activate())
                {
                    children = frame.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
                }

                double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
                foreach (AutomationElement child in children)
                {
                    Rect bounds = child.Cached.BoundingRectangle;
                    string className = child.Cached.ClassName;
                    // The tray and widgets are separate segments, and the images are tray glyphs
                    if (className.StartsWith("SystemTray.") || className == "Image" || child.Cached.AutomationId == "WidgetsButton" || child.Cached.IsOffscreen || bounds.IsEmpty || bounds.Width <= 0)
                    {
                        continue;
                    }
                    left = Math.Min(left, bounds.Left);
                    top = Math.Min(top, bounds.Top);
                    right = Math.Max(right, bounds.Right);
                    bottom = Math.Max(bottom, bounds.Bottom);
                }

                if (left == double.MaxValue)
                {
                    return false;
                }
                rect = new LocalPInvoke.RECT
                {
                    Left = (int)Math.Floor(left),
                    Top = (int)Math.Floor(top),
                    Right = (int)Math.Ceiling(right),
                    Bottom = (int)Math.Ceiling(bottom)
                };
                return true;
            }
            catch (Exception)
            {
                // The cached frame dies when Explorer restarts; drop it so it's looked up again
                taskbarFrames.TryRemove(taskbarHwnd, out _);
                return false;
            }
        }

        /// <summary>
        /// Checks if the taskbar is centred.
        /// </summary>
        /// <returns>
        /// A bool indicating if the taskbar is centred.
        /// </returns>
        public static bool CheckIfCentred(bool isWindows11)
        {
            bool retVal;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced"))
                {
                    if (key != null)
                    {
                        // TaskbarAl is absent until the user changes alignment; Windows 11 defaults to centred
                        int val = (int)key.GetValue("TaskbarAl", isWindows11 ? 1 : 0);

                        if (val == 1)
                        {
                            retVal = true;
                        }
                        else
                        {
                            retVal = false;
                        }
                    }
                    else
                    {
                        retVal = false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return retVal;
        }

        /// <summary>
        /// Compares two taskbars' rects to see if they've changed
        /// </summary>
        /// <returns>
        /// a bool indicating if the taskbar's, applist's, and tray's rects rects have changed.
        /// </returns>
        public static bool TaskbarRefreshRequired(Types.Taskbar currentTB, Types.Taskbar newTB, bool isDynamic)
        {
            // REMINDER: newTB will only have rect & hwnd info. Everything else will be null.


            bool taskbarRectChanged = true;
            bool appListRectChanged = true;
            bool trayRectChanged = true;

            if (
                currentTB.TaskbarRect.Left == newTB.TaskbarRect.Left &&
                currentTB.TaskbarRect.Top == newTB.TaskbarRect.Top &&
                currentTB.TaskbarRect.Right == newTB.TaskbarRect.Right &&
                currentTB.TaskbarRect.Bottom == newTB.TaskbarRect.Bottom)
            {
                taskbarRectChanged = false;
            }
            if (
                currentTB.AppListRect.Left == newTB.AppListRect.Left &&
                currentTB.AppListRect.Top == newTB.AppListRect.Top &&
                currentTB.AppListRect.Right == newTB.AppListRect.Right &&
                currentTB.AppListRect.Bottom == newTB.AppListRect.Bottom)
            {
                appListRectChanged = false;
            }
            if (
                (currentTB.TrayRect.Left + 5 >= newTB.TrayRect.Left && currentTB.TrayRect.Left - 5 <= newTB.TrayRect.Left) &&
                currentTB.TrayRect.Top == newTB.TrayRect.Top &&
                currentTB.TrayRect.Right == newTB.TrayRect.Right &&
                currentTB.TrayRect.Bottom == newTB.TrayRect.Bottom)
            {
                trayRectChanged = false;
            }

            if (isDynamic && (taskbarRectChanged || appListRectChanged || trayRectChanged))
            {
                return true;
            }
            else if (!isDynamic && taskbarRectChanged)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the rects of the three components of the taskbar from their respective handles.
        /// </summary>
        /// <returns>
        /// a partial Taskbar containing just rects and handles.
        /// </returns>
        public static Types.Taskbar GetQuickTaskbarRects(IntPtr taskbarHwnd, IntPtr trayHwnd, IntPtr appListHwnd)
        {
            LocalPInvoke.GetWindowRect(taskbarHwnd, out LocalPInvoke.RECT taskbarRectCheck);
            LocalPInvoke.GetWindowRect(trayHwnd, out LocalPInvoke.RECT trayRectCheck);
            LocalPInvoke.RECT appListRectCheck = GetAppListRect(taskbarHwnd, appListHwnd);

            return new Types.Taskbar()
            {
                TaskbarHwnd = taskbarHwnd,
                TrayHwnd = trayHwnd,
                AppListHwnd = appListHwnd,
                TaskbarRect = taskbarRectCheck,
                TrayRect = trayRectCheck,
                AppListRect = appListRectCheck
            };
        }

        /// <summary>
        /// Resets the specified taskbar.
        /// </summary>
        public static void ResetTaskbar(Types.Taskbar taskbar, Types.Settings settings)
        {
            LocalPInvoke.SetWindowRgn(taskbar.TaskbarHwnd, IntPtr.Zero, true);
            LocalPInvoke.SetLayeredWindowAttributes(taskbar.TaskbarHwnd, 0, 255, LocalPInvoke.LWA_ALPHA);
            int style = LocalPInvoke.GetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE).ToInt32();
            if ((style & LocalPInvoke.WS_EX_LAYERED) == LocalPInvoke.WS_EX_LAYERED)
            {
                LocalPInvoke.SetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE, LocalPInvoke.GetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE).ToInt32() ^ LocalPInvoke.WS_EX_LAYERED);
            }
            style = LocalPInvoke.GetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE).ToInt32();
            if ((style & LocalPInvoke.WS_EX_TRANSPARENT) == LocalPInvoke.WS_EX_TRANSPARENT)
            {
                LocalPInvoke.SetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE, LocalPInvoke.GetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE).ToInt32() ^ LocalPInvoke.WS_EX_TRANSPARENT);
            }

            if (settings.CompositionCompat)
            {
                Interaction.UpdateTranslucentTB(taskbar.TaskbarHwnd);
            }
        }

        /// <summary>
        /// Creates a basic region for a specific taskbar and applies it.
        /// </summary>
        /// <returns>
        /// a bool indicating success.
        /// </returns>
        public static bool UpdateSimpleTaskbar(Types.Taskbar taskbar, Types.Settings settings)
        {
            try
            {
                // Create an effective region to be applied to the taskbar
                Types.EffectiveRegion taskbarEffectiveRegion = new Types.EffectiveRegion
                {
                    CornerRadius = Convert.ToInt32(settings.SimpleTaskbarLayout.CornerRadius * taskbar.ScaleFactor),
                    Top = Convert.ToInt32(settings.SimpleTaskbarLayout.MarginTop * taskbar.ScaleFactor),
                    Left = Convert.ToInt32(settings.SimpleTaskbarLayout.MarginLeft * taskbar.ScaleFactor),
                    Width = Convert.ToInt32(taskbar.TaskbarRect.Right - taskbar.TaskbarRect.Left - (settings.SimpleTaskbarLayout.MarginRight * taskbar.ScaleFactor)) + 1,
                    Height = Convert.ToInt32(taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top - (settings.SimpleTaskbarLayout.MarginBottom * taskbar.ScaleFactor)) + 1
                };

                IntPtr region = LocalPInvoke.CreateRoundRectRgn(taskbarEffectiveRegion.Left, taskbarEffectiveRegion.Top, taskbarEffectiveRegion.Width, taskbarEffectiveRegion.Height, taskbarEffectiveRegion.CornerRadius, taskbarEffectiveRegion.CornerRadius);
                // The system only takes ownership of the region if SetWindowRgn succeeds
                if (LocalPInvoke.SetWindowRgn(taskbar.TaskbarHwnd, region, true) == 0)
                {
                    LocalPInvoke.DeleteObject(region);
                }
                if (settings.CompositionCompat)
                {
                    Interaction.UpdateTranslucentTB(taskbar.TaskbarHwnd);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a dynamic region for a specific taskbar and applies it.
        /// </summary>
        /// <returns>
        /// a bool indicating success.
        /// </returns>
        public static bool UpdateDynamicTaskbar(Types.Taskbar taskbar, Types.Settings settings)
        {
            try
            {
                IntPtr mainRegion;
                int centredDistanceFromEdge = 0;

                // Create an effective region to be applied to the taskbar for the applist
                Types.EffectiveRegion taskbarEffectiveRegion = new Types.EffectiveRegion
                {
                    CornerRadius = Convert.ToInt32(settings.DynamicAppListLayout.CornerRadius * taskbar.ScaleFactor),
                    Top = Convert.ToInt32(settings.DynamicAppListLayout.MarginTop * taskbar.ScaleFactor),
                    Left = Convert.ToInt32(settings.DynamicAppListLayout.MarginLeft * taskbar.ScaleFactor),
                    Width = Convert.ToInt32(taskbar.TaskbarRect.Right - taskbar.TaskbarRect.Left - (settings.DynamicAppListLayout.MarginRight * taskbar.ScaleFactor)) + 1,
                    Height = Convert.ToInt32(taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top - (settings.DynamicAppListLayout.MarginBottom * taskbar.ScaleFactor)) + 1
                };

                // Create an effective region to be applied to the taskbar for the applist
                Types.EffectiveRegion centredEffectiveRegion = new Types.EffectiveRegion
                {
                    CornerRadius = Convert.ToInt32(settings.DynamicAppListLayout.CornerRadius * taskbar.ScaleFactor),
                    Top = Convert.ToInt32(settings.DynamicAppListLayout.MarginTop * taskbar.ScaleFactor),
                    Left = Convert.ToInt32(settings.DynamicAppListLayout.MarginRight * taskbar.ScaleFactor) - 1,
                    Width = Convert.ToInt32(taskbar.TaskbarRect.Right - taskbar.TaskbarRect.Left - (settings.DynamicAppListLayout.MarginRight * taskbar.ScaleFactor)) + 1,
                    Height = Convert.ToInt32(taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top - (settings.DynamicAppListLayout.MarginBottom * taskbar.ScaleFactor)) + 1
                };

                // Create an effective region to be applied to the taskbar for the tray
                Types.EffectiveRegion trayEffectiveRegion = new Types.EffectiveRegion
                {
                    CornerRadius = Convert.ToInt32(settings.DynamicTrayLayout.CornerRadius * taskbar.ScaleFactor),
                    Top = Convert.ToInt32(settings.DynamicTrayLayout.MarginTop * taskbar.ScaleFactor),
                    Left = Convert.ToInt32((settings.DynamicTrayLayout.MarginLeft * taskbar.ScaleFactor) - (3 * taskbar.ScaleFactor)), // Add extra margin for taskbar left as there's no "padding" provided by Windows and always looks weird as soon as you trim it otherwise.
                    Width = Convert.ToInt32(taskbar.TaskbarRect.Right - taskbar.TaskbarRect.Left - (settings.DynamicTrayLayout.MarginRight * taskbar.ScaleFactor)) + 1,
                    Height = Convert.ToInt32(taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top - (settings.DynamicTrayLayout.MarginBottom * taskbar.ScaleFactor)) + 1
                };

                Types.EffectiveRegion widgetsEffectiveRegion = new Types.EffectiveRegion
                {
                    CornerRadius = Convert.ToInt32(settings.DynamicWidgetsLayout.CornerRadius * taskbar.ScaleFactor),
                    Top = Convert.ToInt32(settings.DynamicWidgetsLayout.MarginTop * taskbar.ScaleFactor),
                    Left = Convert.ToInt32(settings.DynamicWidgetsLayout.MarginLeft * taskbar.ScaleFactor),
                    Width = Convert.ToInt32(168 * taskbar.ScaleFactor - (settings.DynamicWidgetsLayout.MarginRight * taskbar.ScaleFactor)) + 1,
                    Height = Convert.ToInt32(taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top - (settings.DynamicWidgetsLayout.MarginBottom * taskbar.ScaleFactor)) + 1
                };

                centredDistanceFromEdge = taskbar.TaskbarRect.Right - taskbar.AppListRect.Right - Convert.ToInt32(2 * taskbar.ScaleFactor);

                // If on Windows 10, add an extra 20 logical pixels for the grabhandle
                if (!settings.IsWindows11)
                {
                    centredDistanceFromEdge -= Convert.ToInt32(20 * taskbar.ScaleFactor);
                }

                // Create region for if the taskbar is centred by take the right-to-right distance (centredDistanceFromEdge) off from both sides, as well as the margin
                if (settings.IsCentred)
                {
                    mainRegion = LocalPInvoke.CreateRoundRectRgn(
                        centredDistanceFromEdge + centredEffectiveRegion.Left,
                        centredEffectiveRegion.Top,
                        centredEffectiveRegion.Width - centredDistanceFromEdge,
                        centredEffectiveRegion.Height,
                        centredEffectiveRegion.CornerRadius,
                        centredEffectiveRegion.CornerRadius
                        );
                }

                // Create a region for if the taskbar is left-aligned, right-to-right distance (centredDistanceFromEdge) off from the right-hand side, as well as the margin
                else
                {

                    mainRegion = LocalPInvoke.CreateRoundRectRgn(
                        taskbarEffectiveRegion.Left,
                        taskbarEffectiveRegion.Top,
                        taskbarEffectiveRegion.Width - centredDistanceFromEdge,
                        taskbarEffectiveRegion.Height,
                        taskbarEffectiveRegion.CornerRadius,
                        taskbarEffectiveRegion.CornerRadius
                        );
                }

                // If the user has it enabled and the tray handle isn't null, create a region for the system tray and merge it with the taskbar region
                if (settings.ShowTray && taskbar.TrayHwnd != IntPtr.Zero)
                {
                    IntPtr trayRegion = LocalPInvoke.CreateRoundRectRgn(
                        (taskbar.TrayRect.Left - taskbar.TaskbarRect.Left) - trayEffectiveRegion.Left,
                        trayEffectiveRegion.Top,
                        trayEffectiveRegion.Width,
                        trayEffectiveRegion.Height,
                        trayEffectiveRegion.CornerRadius,
                        trayEffectiveRegion.CornerRadius
                        );

                    // Merge into the main region (RGN_OR), then free the source as CombineRgn doesn't take ownership
                    LocalPInvoke.CombineRgn(mainRegion, mainRegion, trayRegion, 2);
                    LocalPInvoke.DeleteObject(trayRegion);
                }

                if (settings.ShowWidgets)
                {
                    IntPtr widgetsRegion = LocalPInvoke.CreateRoundRectRgn(
                        widgetsEffectiveRegion.Left,
                        widgetsEffectiveRegion.Top,
                        widgetsEffectiveRegion.Width,
                        widgetsEffectiveRegion.Height,
                        widgetsEffectiveRegion.CornerRadius,
                        widgetsEffectiveRegion.CornerRadius
                        );

                    LocalPInvoke.CombineRgn(mainRegion, mainRegion, widgetsRegion, 2);
                    LocalPInvoke.DeleteObject(widgetsRegion);
                }

                // Apply the final region to the taskbar. The system only takes ownership of the region if SetWindowRgn succeeds
                if (LocalPInvoke.SetWindowRgn(taskbar.TaskbarHwnd, mainRegion, true) == 0)
                {
                    LocalPInvoke.DeleteObject(mainRegion);
                }
                if (settings.CompositionCompat)
                {
                    Interaction.UpdateTranslucentTB(taskbar.TaskbarHwnd);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        /// <summary>
        /// Checks if there are any new taskbars, or if any taskbars are no longer present.
        /// </summary>
        /// <returns>
        /// a bool indicating success.
        /// </returns>
        public static bool TaskbarCountOrHandleChanged(int taskbarCount, IntPtr mainTaskbarHandle)
        {
            List<IntPtr> currentTaskbars = new List<IntPtr>();
            bool otherTaskbarsExist = true;
            IntPtr hwndPrevious = IntPtr.Zero;
            currentTaskbars.Add(LocalPInvoke.FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_TrayWnd", null));

            if (currentTaskbars[0] == IntPtr.Zero)
            {
                return false;
            }

            if (currentTaskbars[0] != mainTaskbarHandle)
            {
                return true;
            }

            while (otherTaskbarsExist)
            {
                IntPtr hwndCurrent = LocalPInvoke.FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_SecondaryTrayWnd", null);
                hwndPrevious = hwndCurrent;

                if (hwndCurrent == IntPtr.Zero)
                {
                    otherTaskbarsExist = false;
                }
                else
                {
                    currentTaskbars.Add(hwndCurrent);
                }
            }
            if (currentTaskbars.Count != taskbarCount)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the provided update is valid.
        /// </summary>
        /// <returns>
        /// A bool indicating if the update is valid.
        /// </returns>
        public static bool CheckDynamicUpdateIsValid(Types.Taskbar currentTB, Types.Taskbar newTB)
        {
            // REMINDER: newTB will only have rect & hwnd info. Everything else will be null.

            // Check if either of the supplied taskbars are null
            if (currentTB == null || newTB == null)
            {
                return false;
            }

            // Check if the taskbar handles are different
            if (currentTB.TaskbarHwnd != newTB.TaskbarHwnd)
            {
                return false;
            }

            // Get width of app list. Not strictly necessary as the applist is always measured from the left but doing so just in case
            int newAppListWidth = newTB.AppListRect.Right - newTB.AppListRect.Left;
            int currentAppListWidth = currentTB.AppListRect.Right - currentTB.AppListRect.Left;

            if (newTB.AppListRect.Right >= newTB.TrayRect.Left && newTB.TrayRect.Left != 0)
            {
                return false;
            }

            if (newAppListWidth == newTB.TrayRect.Left && newTB.TrayRect.Left != 0)
            {
                return false;
            }

            if (newAppListWidth <= 20 * currentTB.ScaleFactor && newAppListWidth != 0)
            {
                return false;
            }

            if (newAppListWidth >= newTB.TaskbarRect.Right - newTB.TaskbarRect.Left && newAppListWidth != 0)
            {
                return false;
            }

            Debug.WriteLine($"Old width: {currentAppListWidth}\nNew width: {newAppListWidth}");
            return true;
        }

        /// <summary>
        /// Collects information on any currently-present taskbars.
        /// </summary>
        /// <returns>
        /// A list of taskbars populated with information about their size, handles etc.
        /// </returns>
        public static List<Types.Taskbar> GenerateTaskbarInfo()
        {
            List<Types.Taskbar> retVal = new List<Types.Taskbar>();

            IntPtr hwndMain = LocalPInvoke.FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null); // Find main taskbar
            LocalPInvoke.GetWindowRect(hwndMain, out LocalPInvoke.RECT rectMain); // Get the RECT of the main taskbar
            IntPtr hrgnMain = IntPtr.Zero; // Set recovery region to IntPtr.Zero
            IntPtr hwndTray = LocalPInvoke.FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null); // Get handle to the main taskbar's tray
            LocalPInvoke.GetWindowRect(hwndTray, out LocalPInvoke.RECT rectTray); // Get the RECT for the main taskbar's tray
            IntPtr hwndAppList = LocalPInvoke.FindWindowExA(LocalPInvoke.FindWindowExA(hwndMain, IntPtr.Zero, "ReBarWindow32", null), IntPtr.Zero, "MSTaskSwWClass", null); // Get the handle to the main taskbar's app list
            LocalPInvoke.RECT rectAppList = GetAppListRect(hwndMain, hwndAppList); // Get the RECT for the main taskbar's app list

            retVal.Add(new Types.Taskbar
            {
                TaskbarHwnd = hwndMain,
                TrayHwnd = hwndTray,
                AppListHwnd = hwndAppList,
                TaskbarRect = rectMain,
                TrayRect = rectTray,
                AppListRect = rectAppList,
                RecoveryHrgn = hrgnMain,
                ScaleFactor = Convert.ToDouble(LocalPInvoke.GetDpiForWindow(hwndMain)) / 96.00,
                TaskbarRes = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}",
                Ignored = false
            });
            int style = LocalPInvoke.GetWindowLong(hwndMain, LocalPInvoke.GWL_EXSTYLE).ToInt32();
            if ((style & LocalPInvoke.WS_EX_LAYERED) != LocalPInvoke.WS_EX_LAYERED)
            {
                LocalPInvoke.SetWindowLong(hwndMain, LocalPInvoke.GWL_EXSTYLE, LocalPInvoke.GetWindowLong(hwndMain, LocalPInvoke.GWL_EXSTYLE).ToInt32() ^ LocalPInvoke.WS_EX_LAYERED);
                LocalPInvoke.SetLayeredWindowAttributes(hwndMain, 0, 255, LocalPInvoke.LWA_ALPHA);
            }




            bool i = true;
            IntPtr hwndPrevious = IntPtr.Zero;
            while (i)
            {
                IntPtr hwndCurrent = LocalPInvoke.FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_SecondaryTrayWnd", null);
                hwndPrevious = hwndCurrent;

                if (hwndCurrent == IntPtr.Zero)
                {
                    i = false;
                }
                else
                {
                    LocalPInvoke.GetWindowRect(hwndCurrent, out LocalPInvoke.RECT rectCurrent);
                    LocalPInvoke.GetWindowRgn(hwndCurrent, out IntPtr hrgnCurrent);
                    Interaction interaction = new Interaction();
                    IntPtr hwndSecTray = IntPtr.Zero;
                    if (interaction.IsWindows11())
                    {
                        IntPtr imd = LocalPInvoke.FindWindowExA(hwndCurrent, IntPtr.Zero, "Windows.UI.Composition.DesktopWindowContentBridge", null);
                        hwndSecTray = LocalPInvoke.FindWindowExA(hwndCurrent, imd, "Windows.UI.Composition.DesktopWindowContentBridge", null);
                    }
                    else
                    {
                        hwndSecTray = LocalPInvoke.FindWindowExA(hwndCurrent, IntPtr.Zero, "TrayNotifyWnd", null); // Get handle to this secondary taskbar's tray
                    }
                    LocalPInvoke.GetWindowRect(hwndSecTray, out LocalPInvoke.RECT rectSecTray); // Get the RECT for this secondary taskbar's tray
                    IntPtr hwndSecAppList = LocalPInvoke.FindWindowExA(LocalPInvoke.FindWindowExA(hwndCurrent, IntPtr.Zero, "WorkerW", null), IntPtr.Zero, "MSTaskListWClass", null); // Get the handle to the main taskbar's app list
                    LocalPInvoke.RECT rectSecAppList = GetAppListRect(hwndCurrent, hwndSecAppList); // Get the RECT for this secondary taskbar's app list
                    retVal.Add(new Types.Taskbar
                    {
                        TaskbarHwnd = hwndCurrent,
                        TrayHwnd = hwndSecTray,
                        AppListHwnd = hwndSecAppList,
                        TaskbarRect = rectCurrent,
                        TrayRect = rectSecTray,
                        AppListRect = rectSecAppList,
                        RecoveryHrgn = hrgnCurrent,
                        ScaleFactor = Convert.ToDouble(LocalPInvoke.GetDpiForWindow(hwndCurrent)) / 96.00,
                        TaskbarRes = $"{rectCurrent.Right - rectCurrent.Left} x {rectCurrent.Bottom - rectCurrent.Top}",
                        Ignored = false
                    });
                    style = LocalPInvoke.GetWindowLong(hwndCurrent, LocalPInvoke.GWL_EXSTYLE).ToInt32();
                    if ((style & LocalPInvoke.WS_EX_LAYERED) != LocalPInvoke.WS_EX_LAYERED)
                    {
                        LocalPInvoke.SetWindowLong(hwndCurrent, LocalPInvoke.GWL_EXSTYLE, LocalPInvoke.GetWindowLong(hwndCurrent, LocalPInvoke.GWL_EXSTYLE).ToInt32() ^ LocalPInvoke.WS_EX_LAYERED);
                        LocalPInvoke.SetLayeredWindowAttributes(hwndCurrent, 0, 255, LocalPInvoke.LWA_ALPHA);
                    }
                }
            }

            //foreach (var tb in retVal)
            //{
            //    TaskbarShouldBeFilled(tb.TaskbarHwnd);
            //}
            return retVal;
        }

        /// <summary>
        /// Checks if the given taskbar should be filled to the edge of the screen.
        /// </summary>
        /// <returns>
        /// A bool indicating whether or not the taskbar needs to be filled.
        /// </returns>
        public static bool TaskbarShouldBeFilled(IntPtr taskbarHwnd, Types.Settings settings)
        {
            bool retVal = false;

            if (settings.FillOnMaximise)
            {
                // Attempt to check for if alt+tab/task switcher is open (Windows 11 only)
                IntPtr topHwnd = LocalPInvoke.WindowFromPoint(new LocalPInvoke.POINT() { x = 0, y = 0 });
                StringBuilder windowClass = new StringBuilder(1024);
                try
                {
                    LocalPInvoke.GetClassName(topHwnd, windowClass, 1024);

                    if (windowClass.ToString() == "XamlExplorerHostIslandWindow" && settings.FillOnTaskSwitch)
                    {
                        return true;
                    }
                }
                catch (Exception) { }

                List<IntPtr> windowList = Interaction.GetTopLevelWindows();
                foreach (IntPtr windowHwnd in windowList)
                {
                    if (LocalPInvoke.IsWindowVisible(windowHwnd))
                    {
                        if (LocalPInvoke.MonitorFromWindow(taskbarHwnd, 2) == LocalPInvoke.MonitorFromWindow(windowHwnd, 2))
                        {
                            LocalPInvoke.DwmGetWindowAttribute(windowHwnd, LocalPInvoke.DWMWINDOWATTRIBUTE.Cloaked, out bool isCloaked, 0x4);
                            if (!isCloaked)
                            {
                                LocalPInvoke.WINDOWPLACEMENT lpwndpl = new LocalPInvoke.WINDOWPLACEMENT();
                                LocalPInvoke.GetWindowPlacement(windowHwnd, ref lpwndpl);
                                if (lpwndpl.ShowCmd == LocalPInvoke.ShowWindowCommands.ShowMaximized)
                                {
                                    retVal = true;
                                }
                            }
                        }
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Sets the appbar properties of the taskbar.
        /// </summary>
        public static void SetTaskbarState(LocalPInvoke.AppBarStates option, IntPtr hwnd)
        {
            LocalPInvoke.APPBARDATA msgData = new LocalPInvoke.APPBARDATA();
            msgData.cbSize = (uint)Marshal.SizeOf(msgData);
            msgData.hWnd = hwnd;
            msgData.lParam = (int)option;
            LocalPInvoke.SHAppBarMessage(LocalPInvoke.ABM.SetState, ref msgData);
        }


    }
}
