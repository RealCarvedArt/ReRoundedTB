using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RoundedTB
{
    public class Interaction
    {
        public MainWindow mw;

        public Interaction()
        {
            try
            {
                mw = (MainWindow)Application.Current.MainWindow;
            }
            catch (Exception)
            {
                // No idea why this was necessary but it was so it's here now. Yay. TODO - work out why this is suddenly broken and unbreak it
            }
        }

        public Types.Settings ReadJSON()
        {
            // Returns null for an empty or corrupt config so the caller falls back to defaults
            try
            {
                return ParseSettings(File.ReadAllText(mw.configPath));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                AddLog($"Failed to read config, using defaults: {ex.GetType().Name}");
                return null;
            }
        }

        /// <summary>
        /// Parses rtb.json text. Returns null for empty or corrupt content, so the caller falls back to defaults.
        /// </summary>
        public static Types.Settings ParseSettings(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Types.Settings>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public bool IsWindows11()
        {
            Debug.WriteLine(Environment.OSVersion.Version.Build);
            if (Environment.OSVersion.Version.Build >= 21996)
            {
                return true;
            }
            return false;
        }

        public void WriteJSON()
        {
            // Write to a temp file and swap it in, so a crash mid-write can't leave an empty/truncated config
            string tempPath = mw.configPath + ".tmp";
            try
            {
                File.WriteAllText(tempPath, JsonConvert.SerializeObject(mw.activeSettings, Formatting.Indented));
                if (File.Exists(mw.configPath))
                {
                    File.Replace(tempPath, mw.configPath, null);
                }
                else
                {
                    File.Move(tempPath, mw.configPath);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // e.g. antivirus or an editor holding the file. The settings stay applied in memory and are saved on the next Apply or exit.
                AddLog($"Failed to save config: {ex.GetType().Name}");
            }
        }

        public void FileSystem()
        {
            if (!File.Exists(mw.configPath))
            {
                mw.activeSettings = Types.Settings.CreateDefault(mw.isWindows11);
                WriteJSON(); // butts - Missy Quarry, 2020
            }
            // An empty or corrupt config is handled by ReadJSON returning null, which applies the OS defaults

        }

        public static bool SetWorkspace(LocalPInvoke.RECT rect)
        {
            bool result = LocalPInvoke.SystemParametersInfo(LocalPInvoke.SPI_SETWORKAREA, 0, ref rect, LocalPInvoke.SPIF_change);
            if (!result)
            {
                // Get error
                Debug.WriteLine("Error setting work area: " + Marshal.GetLastWin32Error().ToString());
            }

            return result;
        }

        public void AddLog(string message)
        {
            // Logging is disabled. Before re-enabling: keep paths/usernames out of messages, cap the file size, and document the file.
        }

        public static bool IsTranslucentTBRunning()
        {
            Mutex mutex = null;
            try
            {
                return Mutex.TryOpenExisting("344635E9-9AE4-4E60-B128-D53E25AB70A7", out mutex);
            }
            finally
            {
                mutex?.Dispose();
            }
        }

        // Request that TranslucentTB forefully refesh the taskbar
        // Times out instead of blocking, so a hung TranslucentTB (or Explorer) can't freeze ReRoundedTB or its crash recovery
        public static IntPtr UpdateTranslucentTB(IntPtr taskbarHwnd)
        {
            LocalPInvoke.SendMessageTimeout(LocalPInvoke.FindWindow("TTB_WorkerWindow", "TTB_WorkerWindow"), LocalPInvoke.RegisterWindowMessage("TTB_ForceRefreshTaskbar"), IntPtr.Zero, taskbarHwnd, LocalPInvoke.SMTO_ABORTIFHUNG, 500, out IntPtr result);
            return result;
        }

        // Attempt to forcefully refresh the taskbar
        public static void UpdateLegacyTB(IntPtr taskbarHwnd)
        {
            const int WM_DWMCOMPOSITIONCHANGED = 789;
            LocalPInvoke.SendMessageTimeout(taskbarHwnd, WM_DWMCOMPOSITIONCHANGED, new IntPtr(1), IntPtr.Zero, LocalPInvoke.SMTO_ABORTIFHUNG, 1000, out _);
        }

        /// <summary>
        /// Calculates whether or not an integer is odd or even.
        /// </summary>
        /// <param name="input">
        /// The integer to be checked for oddness.
        /// </param>
        /// <returns>
        /// A nullable bool, which represents if the provided integer is odd. If the provided integer is neither even nor odd, then returns null.
        /// </returns>
        public bool? IsOdd(int input)
        {
            // The following section declares and initialises the required variables for the caculation.
            decimal comparison = input / 2; // A decimal, representing approximately half of the user's input.
            int check = Convert.ToInt32(comparison) * 2; // An integer-representation of the user's input value.

            // The following section tests for oddness by looking for differences in the prior-initialised values.
            if (check == input) // Checks if the "check" value is equal to the input.
            {
                return false; // Return false to indicate the value is not odd.
            }
            else if (check != input) // Repeat the above check in the event that quantum tunnelling has resulted in a variable changing.
            {
                return true; // Return true to indicate the value is odd.
            }
            return null; // Finally, return null to indicate that the provided number is neither odd nor even - not currently required, added for future-proofing in the event the concept of mathematics changes significantly enough to warrant it.
        // (this is a joke to annoy sylly)
        }

        // A second launch broadcasts this to ask the running instance to show its settings
        public static readonly int ShowSettingsMessage = LocalPInvoke.RegisterWindowMessage("ReRoundedTB_ShowSettings");

        // Broadcast by Explorer when it (re)creates the taskbar
        public static readonly int TaskbarCreatedMessage = LocalPInvoke.RegisterWindowMessage("TaskbarCreated");

        // The taskbar our tray icon was last added to (the one running at startup, to begin with)
        private IntPtr trayIconTaskbar = LocalPInvoke.FindWindow("Shell_TrayWnd", null);

        public IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            // A WM_SETTEXT carrying the old-style request (sent by newer launches for compatibility) is the same request; keep our title
            const int WM_SETTEXT = 0x000C;
            bool isLegacyRequest = msg == WM_SETTEXT && lParam != IntPtr.Zero && Marshal.PtrToStringUni(lParam) == "ReRoundedTB_SettingsRequest";

            if ((ShowSettingsMessage != 0 && msg == ShowSettingsMessage) || isLegacyRequest)
            {
                if (mw.Visibility != Visibility.Visible)
                {
                    mw.ShowMenuItem_Click(null, null);
                }
                mw.Activate();
                handled = true;
                return new IntPtr(1);
            }

            // Explorer restarted: its new taskbar has no tray icons, and WPF-UI doesn't re-add ours, which would leave
            // no Pause/Close (smoke test 2026-10-09). Not handled, so other listeners still see it.
            if (TaskbarCreatedMessage != 0 && msg == TaskbarCreatedMessage)
            {
                try
                {
                    // Each re-register leaks a hidden WPF-UI window and an icon handle, and any app can broadcast this message,
                    // so only act when there really is a new taskbar (security review SR6-1)
                    IntPtr trayWnd = LocalPInvoke.FindWindow("Shell_TrayWnd", null);
                    if (trayWnd != IntPtr.Zero && trayWnd != trayIconTaskbar)
                    {
                        trayIconTaskbar = trayWnd;
                        mw.trayIcon.Register();
                        mw.TrayIconCheck();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Re-adding the tray icon failed: " + ex.GetType().Name);
                }
            }

            // Light/dark mode changes arrive as a setting change; refresh the tray icon here too, because the
            // worker's once-a-second check doesn't run while paused (not handled, so WPF still sees the message)
            const int WM_SETTINGCHANGE = 0x001A;
            if (msg == WM_SETTINGCHANGE)
            {
                // Cosmetic: a failure here mustn't reach the crash handler and close the app
                try
                {
                    mw.TrayIconCheck();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("TrayIconCheck on WM_SETTINGCHANGE failed: " + ex.GetType().Name);
                }
            }

            switch (msg)
            {
                case WM_HOTKEY:
                    Debug.WriteLine(msg);
                    switch (wParam.ToInt32())
                    {
                        case 9000:
                            int vkey = ((int)lParam >> 16) & 0xFFFF;
                            Debug.WriteLine(vkey);
                            if (vkey == 0x71)
                            {
                                if (mw.showTrayCheckBox.IsChecked == true)
                                {
                                    mw.showTrayCheckBox.IsChecked = false;
                                }
                                else
                                {
                                    mw.showTrayCheckBox.IsChecked = true;
                                }
                                mw.ApplyButton_Click(null, null);
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public static bool IsAutoHideEnabled()
        {
            return Math.Abs(SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height) > 0;
        }

        public bool IsTaskbarVisibleOnMonitor(LocalPInvoke.RECT tbRectP, LocalPInvoke.RECT monitorRectP)
        {
            Rectangle tbRect = new Rectangle(tbRectP.Left + 3, tbRectP.Top + 3, tbRectP.Right - tbRectP.Left - 3, tbRectP.Bottom - tbRectP.Top - 3);
            Rectangle monitorRect = new Rectangle(monitorRectP.Left, monitorRectP.Top, monitorRectP.Right - monitorRectP.Left, monitorRectP.Bottom - monitorRectP.Top);
            return tbRect.IntersectsWith(monitorRect);
        }

        public delegate bool CallBack(int hwnd, int lParam);

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        public static List<IntPtr> GetTopLevelWindows()
        {
            List<IntPtr> AllActiveHandles = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(AllActiveHandles);
            try
            {
                EnumWindowsProc tlProc = new EnumWindowsProc(EnumWindow);
                LocalPInvoke.EnumWindows(tlProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                {
                    listHandle.Free();
                }
            }
            return AllActiveHandles;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            if (!(gch.Target is List<IntPtr> list))
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            return true;
        }

        public static bool TaskbarOnMonitorWithMaximisedWindow(IntPtr taskbarHwnd)
        {
            return true;
        }

        public enum TaskbarPosition
        {
            Unknown = -1,
            Left,
            Top,
            Right,
            Bottom,
        }

        public sealed class Taskbar
        {
            public Rectangle Bounds
            {
                get;
                private set;
            }
            public TaskbarPosition Position
            {
                get;
                private set;
            }
            public System.Drawing.Point Location
            {
                get
                {
                    return Bounds.Location;
                }
            }
            public System.Drawing.Size Size
            {
                get
                {
                    return Bounds.Size;
                }
            }

            //Always returns false under Windows 7
            public bool AlwaysOnTop
            {
                get;
                private set;
            }
            public bool AutoHide
            {
                get;
                private set;
            }

            public Taskbar(IntPtr taskbarHandle)
            {

                LocalPInvoke.APPBARDATA data = new LocalPInvoke.APPBARDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(LocalPInvoke.APPBARDATA));
                data.hWnd = taskbarHandle;
                IntPtr result = LocalPInvoke.SHAppBarMessage(LocalPInvoke.ABM.GetTaskbarPos, ref data);
                Position = (TaskbarPosition)data.uEdge;
                Bounds = Rectangle.FromLTRB(data.rc.Left, data.rc.Top, data.rc.Right, data.rc.Bottom);

                data.cbSize = (uint)Marshal.SizeOf(typeof(LocalPInvoke.APPBARDATA));
                result = LocalPInvoke.SHAppBarMessage(LocalPInvoke.ABM.GetState, ref data);
                int state = result.ToInt32();
                AlwaysOnTop = (state & LocalPInvoke.ABS.AlwaysOnTop) == LocalPInvoke.ABS.AlwaysOnTop;
                AutoHide = (state & LocalPInvoke.ABS.Autohide) == LocalPInvoke.ABS.Autohide;
            }
        }
    }
}
