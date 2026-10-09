using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Reflection;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Many thanks to
    ///  - FloatingMilkshake
    ///  - cardin
    ///  - cleverActon0126
    ///  for your gracious donations! 💖
    ///  
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool isWindows11;
        public List<Types.Taskbar> taskbarDetails = new List<Types.Taskbar>();
        public bool shouldReallyDieNoReally = false;
        public string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.json");
        public string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.log");
        public Types.Settings activeSettings = new Types.Settings();
        public BackgroundWorker taskbarThread = new BackgroundWorker();
        public IntPtr hwndDesktopButton = IntPtr.Zero;
        public int lastDynDistance = 0;
        public int numberToForceRefresh = 0;
        public bool isCentred = false;
        public bool isAlreadyRunning = false;
        public Background background;
        public Interaction interaction;
        private HwndSource source;
        public int selectedSegment = 0; // 0 = Simple, 1 = AppList, 2 = Tray, 3 = Widgets
        public int version = 3;
        /// <summary>
        /// Versions:
        /// -1: Canary
        ///  0: R3.0
        ///  1: P3.1B
        ///  2: R3.1
        ///  3: P3.5 and later (bump this when the About window should show once after an update)
        /// </summary>

        // For the crash handler in App, which may run on a thread that can't use Application.Current.MainWindow
        public static MainWindow Instance { get; private set; }

        // Held for the life of the process; its existence is what tells a second launch that we're running
        private static System.Threading.Mutex instanceMutex;

        // This window's handle, readable from the worker thread
        public IntPtr MainHwnd { get; private set; }

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica, updateAccents: false);
            IsVisibleChanged += (_, e) =>
            {
                if ((bool)e.NewValue)
                {
                    FitToWorkArea();
                }
            };
            // WPF applies Windows' suggested size after raising DpiChanged, so fit afterwards (security review SR6-2)
            DpiChanged += (_, _) => Dispatcher.BeginInvoke(FitToWorkArea, DispatcherPriority.Loaded);
            normalBackground = Background;
            ApplyContrastMode();
            SystemParameters.StaticPropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SystemParameters.HighContrast))
                {
                    Dispatcher.Invoke(ApplyContrastMode);
                }
            };


            // Check OS build, as behaviours rather-annoyingly differ between Windows 11 and Windows 10
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var buildNumber = registryKey.GetValue("CurrentBuild").ToString();
            if (Convert.ToInt32(buildNumber) >= 21996)
            {
                isWindows11 = true;
            }
            else
            {
                isWindows11 = false;
                activeSettings.IsWindows11 = false;
                dynamicCheckBox.Content = "Split mode";
                fillAltTabCheckBox.Content = "[Unavailable]";
            }

            // Initialise functions
            background = new Background();
            interaction = new Interaction();

            // Check if ReRoundedTB is already running, and if it is, ask it to show its settings and exit.
            // The session-local mutex is the real check; the process name catches versions from before the mutex existed.
            bool isFirstInstance;
            try
            {
                instanceMutex = new System.Threading.Mutex(true, @"Local\ReRoundedTB.Instance", out isFirstInstance);
            }
            catch (UnauthorizedAccessException)
            {
                // Another process holds the name with an ACL we can't open, so something is already there
                isFirstInstance = false;
            }
            // Only count copies in this Windows session; another signed-in user's copy shouldn't block this one
            int sessionId = Process.GetCurrentProcess().SessionId;
            int runningHere = Array.FindAll(Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName), p => p.SessionId == sessionId).Length;
            if (!isFirstInstance || runningHere > 1)
            {
                // This launch has the foreground (the user just started it); pass that on to the running copy so its settings
                // window comes to the front instead of opening behind the active app. Only to our own processes in this session.
                int ownId = Environment.ProcessId;
                foreach (Process other in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
                {
                    if (other.Id != ownId && other.SessionId == sessionId)
                    {
                        LocalPInvoke.AllowSetForegroundWindow(other.Id);
                    }
                }

                // Posted, not sent, so a busy or hung instance can't stall this one
                LocalPInvoke.PostMessage(LocalPInvoke.HWND_BROADCAST, Interaction.ShowSettingsMessage, IntPtr.Zero, IntPtr.Zero);

                // Older versions instead watch their own window title for a settings request
                foreach (IntPtr hwnd in Interaction.GetTopLevelWindows())
                {
                    StringBuilder windowClass = new StringBuilder(1024);
                    LocalPInvoke.GetClassName(hwnd, windowClass, 1024);
                    // WPF window classes are "HwndWrapper[<process name>;;<guid>]". Only read the title of our own windows, never other apps'.
                    if (!windowClass.ToString().Contains("HwndWrapper[ReRoundedTB;"))
                    {
                        continue;
                    }
                    StringBuilder windowTitle = new StringBuilder(1024);
                    LocalPInvoke.GetWindowText(hwnd, windowTitle, 1024);
                    if (windowTitle.ToString() == "ReRoundedTB")
                    {
                        LocalPInvoke.SendMessageTimeout(hwnd, LocalPInvoke.WM_SETTEXT, IntPtr.Zero, "ReRoundedTB_SettingsRequest", LocalPInvoke.SMTO_ABORTIFHUNG, 1000, out _);
                    }
                }
                shouldReallyDieNoReally = true;
                isAlreadyRunning = true;
                // Closing a window from its constructor makes StartupUri's Show() throw, so exit outright.
                // Nothing has been applied to the taskbar yet.
                Environment.Exit(0);
                return;
            }

            // The original RoundedTB has a different process name, so the check above misses it, and two copies fight over the taskbar
            if (Process.GetProcessesByName("RoundedTB").Length > 0)
            {
                System.Windows.MessageBox.Show(
                    "RoundedTB is already running. Close it from its tray icon (right-click > Close RoundedTB), then start ReRoundedTB again.",
                    "ReRoundedTB", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(0);
                return;
            }
            TrayIconCheck();

            MigrateLegacyStartupShortcut();

            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupLinkName)))
            {
                StartupMenuItem.IsChecked = true;
                ShowMenuItem.Header = "Show ReRoundedTB";
            }
            taskbarThread.WorkerSupportsCancellation = true;
            taskbarThread.WorkerReportsProgress = true;
            taskbarThread.DoWork +=background.DoWork;

            // Load settings into memory/UI
            interaction.FileSystem();
            interaction.AddLog($"ReRoundedTB started!");
            activeSettings = interaction.ReadJSON();

            activeSettings ??= Types.Settings.CreateDefault(isWindows11);
            activeSettings.IsWindows11 = isWindows11;

            activeSettings.Normalize(isWindows11, autoHideComboBox.Items.Count);

            if (version != activeSettings.Version && version != -1)
            {
                activeSettings.IsNotFirstLaunch = false;
            }
            activeSettings.Version = version;


            interaction.AddLog($"Settings loaded:");
            interaction.AddLog(
                $"SimpleTaskbarLayout: {activeSettings.SimpleTaskbarLayout}\n" +
                $"DynamicAppListLayout: {activeSettings.DynamicAppListLayout}\n" +
                $"DynamicTrayLayout: {activeSettings.DynamicTrayLayout}\n" +
                $"DynamicWidgetsLayout: {activeSettings.DynamicWidgetsLayout}\n" +
                $"IsDynamic: {activeSettings.IsDynamic}\n" +
                $"IsCentred: {activeSettings.IsCentred}\n" +
                $"ShowTray: {activeSettings.ShowTray}\n" +
                $"ShowWidgets: {activeSettings.ShowWidgets}\n" +
                $"CompositionCompat: {activeSettings.CompositionCompat}\n" +
                $"IsNotFirstLaunch: {activeSettings.IsNotFirstLaunch}\n" +
                $"FillOnMaximise: {activeSettings.FillOnMaximise}\n" +
                $"FillOnTaskSwitch: {activeSettings.FillOnTaskSwitch}\n" +
                $"ShowTrayOnHover: {activeSettings.ShowSegmentsOnHover}\n"
                );

            // Get whether or not taskbar is centred
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced"))
                {
                    if (key != null)
                    {
                        int val = (int)key.GetValue("TaskbarAl", isWindows11 ? 1 : 0);
                        if (val == 1)
                        {
                            isCentred = true;
                        }
                        else
                        {
                            isCentred = false;
                        }
                        interaction.AddLog($"Taskbar centred? {isCentred}");
                    }
                }
            }
            catch (Exception aaaa)
            {
                interaction.AddLog(aaaa.Message);
            }
            if (!isWindows11)
            {
                activeSettings.IsCentred = false;
            }

            LoadSettingsIntoUi();
            taskbarDetails = Taskbar.GenerateTaskbarInfo();

            ApplyButton_Click(null, null);


            if (!activeSettings.FillOnMaximise)
            {
                activeSettings.FillOnTaskSwitch = false;
                fillAltTabCheckBox.IsEnabled = false;
            }

            //Showhide the split mode help button
            if (!isWindows11 && activeSettings.IsDynamic)
            {
                splitHelpButton.Visibility = Visibility.Visible;
            }
            else
            {
                splitHelpButton.Visibility = Visibility.Hidden;
            }

            if (activeSettings.IsNotFirstLaunch != true)
            {
                activeSettings.IsNotFirstLaunch = true;
                AboutWindow aw = new AboutWindow();
                aw.expander0.IsExpanded = true;
                aw.ShowDialog();
                try
                {
                    Visibility = Visibility.Visible;
                }
                catch (InvalidOperationException)
                {

                }
                ShowMenuItem.Header = "Hide ReRoundedTB";
            }

            AutoHide(true, taskbarDetails);

            UpdateUi();

        }

        // Shows activeSettings in the settings window, with the first segment selected
        private void LoadSettingsIntoUi()
        {
            Types.SegmentSettings layout = activeSettings.IsDynamic ? activeSettings.DynamicAppListLayout : activeSettings.SimpleTaskbarLayout;
            selectedSegment = activeSettings.IsDynamic ? 1 : 0;
            taskbarRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            trayRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            widgetsRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            cornerRadiusInput.Text = layout.CornerRadius.ToString();
            SetRadiusSliderFromCode(layout.CornerRadius);
            mTopInput.Text = layout.MarginTop.ToString();
            mLeftInput.Text = layout.MarginLeft.ToString();
            mBottomInput.Text = layout.MarginBottom.ToString();
            mRightInput.Text = layout.MarginRight.ToString();

            // Order matters: the dynamic and fill-on-maximise handlers change the checkboxes after them.
            // Flagged so the "you just ticked this" info boxes don't pop up when Reset loads settings into a visible window.
            loadingSettingsIntoUi = true;
            try
            {
                dynamicCheckBox.IsChecked = activeSettings.IsDynamic;
                centredCheckBox.IsChecked = activeSettings.IsCentred;
                showTrayCheckBox.IsChecked = activeSettings.ShowTray;
                showWidgetsCheckBox.IsChecked = activeSettings.ShowWidgets;
                fillMaximisedCheckBox.IsChecked = activeSettings.FillOnMaximise;
                fillAltTabCheckBox.IsChecked = activeSettings.FillOnTaskSwitch;
                showSegmentsOnHoverCheckBox.IsChecked = activeSettings.ShowSegmentsOnHover;
                compositionFixCheckBox.IsChecked = activeSettings.CompositionCompat;
            }
            finally
            {
                loadingSettingsIntoUi = false;
            }
            autoHideComboBox.SelectedIndex = activeSettings.AutoHide;
        }

        private bool loadingSettingsIntoUi;

        public void UpdateUi()
        {
            if (!activeSettings.ShowTray || activeSettings.ShowSegmentsOnHover)
            {
                trayRectStandIn.Opacity = DimmedSegmentOpacity;
            }
            else
            {
                trayRectStandIn.Opacity = 1;
            }

            if (!activeSettings.ShowWidgets || activeSettings.ShowSegmentsOnHover)
            {
                widgetsRectStandIn.Opacity = DimmedSegmentOpacity;
            }
            else
            {
                widgetsRectStandIn.Opacity = 1;
            }

            UpdateSegmentLabels();

            if (activeSettings.IsCentred && activeSettings.IsWindows11 && activeSettings.IsDynamic)
            {
                taskbarRectStandIn.Margin = new Thickness(126, 0, 126, 5);
                trayRectStandIn.Visibility = Visibility.Visible;
                widgetsRectStandIn.Visibility = Visibility.Visible;
            }
            else if (activeSettings.IsDynamic)
            {
                taskbarRectStandIn.Margin = new Thickness(5, 0, 247, 5);
                trayRectStandIn.Visibility = Visibility.Visible;
                widgetsRectStandIn.Visibility = Visibility.Hidden;
            }
            else
            {
                taskbarRectStandIn.Margin = new Thickness(5, 210, 5, 5);
                trayRectStandIn.Visibility = Visibility.Hidden;
                widgetsRectStandIn.Visibility = Visibility.Hidden;

            }
        }

        // Hidden segments are dimmed; at 0.65 their text measures about 6:1 contrast on the dark theme (0.5 was 4.3:1, under WCAG's 4.5:1)
        private const double DimmedSegmentOpacity = 0.72; // 0.65 measured 4.43:1 in light mode

        // Name each segment button and its state in text, so it isn't conveyed by opacity or colour alone (screen readers, tooltips)
        private void UpdateSegmentLabels()
        {
            static string SegmentState(bool shown, bool onHover) => !shown ? " (hidden)" : onHover ? " (shown on hover)" : "";
            SetSegmentLabel(taskbarRectStandIn, activeSettings.IsDynamic ? "App list" : "Taskbar", "");
            SetSegmentLabel(trayRectStandIn, "Tray", SegmentState(activeSettings.ShowTray, activeSettings.ShowSegmentsOnHover));
            SetSegmentLabel(widgetsRectStandIn, "Widgets", SegmentState(activeSettings.ShowWidgets, activeSettings.ShowSegmentsOnHover));
        }

        private static void SetSegmentLabel(Wpf.Ui.Controls.Button button, string segment, string state)
        {
            // The selected segment is the Primary (accent-coloured) one; bold text and an item status say so without relying on colour
            bool selected = button.Appearance == Wpf.Ui.Controls.ControlAppearance.Primary;
            button.Content = segment;
            button.FontWeight = selected ? FontWeights.Bold : FontWeights.Normal;
            button.ToolTip = $"Edit the {segment.ToLower()} segment{state}";
            System.Windows.Automation.AutomationProperties.SetName(button, $"{segment} segment{state}{(selected ? ", selected" : "")}");
            System.Windows.Automation.AutomationProperties.SetItemStatus(button, selected ? "Selected" : "");
        }

        public void AutoHide(bool enabled, List<Types.Taskbar> taskbarDetails)
        {
            // No taskbars found (e.g. Explorer restarting): nothing to show or hide
            if (taskbarDetails.Count == 0)
            {
                return;
            }
            int workingHeight = Screen.PrimaryScreen.WorkingArea.Height;
            int boundsHeight = Screen.PrimaryScreen.Bounds.Height;
            int taskbarHeight = taskbarDetails[0].TaskbarRect.Bottom - taskbarDetails[0].TaskbarRect.Top;
            bool workAreaMisconfigured = false;

            if (boundsHeight - taskbarHeight > workingHeight)
            {
                workAreaMisconfigured = true;
            }

            if (activeSettings.AutoHide > 0 && enabled)
            {
                MonitorStuff.DisplayInfoCollection Displays = MonitorStuff.GetDisplays();

                foreach (MonitorStuff.DisplayInfo display in Displays)
                {
                    LocalPInvoke.RECT workArea = display.MonitorArea;
                    workArea.Bottom = workArea.Bottom - 2;
                    Interaction.SetWorkspace(workArea);
                }
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    LocalPInvoke.SetWindowPos(taskbar.TaskbarHwnd, new IntPtr(-1), 0, 0, 0, 0, LocalPInvoke.SetWindowPosFlags.IgnoreMove | LocalPInvoke.SetWindowPosFlags.IgnoreResize);
                    Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AlwaysOnTop, taskbar.TaskbarHwnd);
                }
            }
            else if (!enabled)
            {
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    LocalPInvoke.SetWindowPos(taskbar.TaskbarHwnd, new IntPtr(-1), 0, 0, 0, 0, LocalPInvoke.SetWindowPosFlags.IgnoreMove | LocalPInvoke.SetWindowPosFlags.IgnoreResize);
                    if (workAreaMisconfigured)
                    {
                        Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AutoHide, taskbar.TaskbarHwnd);
                        Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AlwaysOnTop, taskbar.TaskbarHwnd);
                    }

                    MonitorStuff.DisplayInfoCollection Displays = MonitorStuff.GetDisplays();

                    foreach (MonitorStuff.DisplayInfo display in Displays)
                    {
                        taskbarHeight = taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top;
                        LocalPInvoke.RECT workArea = display.MonitorArea;
                        workArea.Bottom = workArea.Bottom - taskbarHeight;
                        Interaction.SetWorkspace(workArea);
                    }
                }
            }
        }

        // Called about once a second: the tray tooltip says what ReRoundedTB is doing, so a taskbar that looks wrong isn't a mystery
        public void TrayIconCheck()
        {
            List<Types.Taskbar> taskbars = taskbarDetails; // the worker can swap the list out
            bool taskbarFound = taskbars.Count > 0 && LocalPInvoke.IsWindow(taskbars[0].TaskbarHwnd);
            string status = paused ? "ReRoundedTB - paused (normal taskbar)"
                : taskbarFound ? "ReRoundedTB - active"
                : "ReRoundedTB - waiting for the taskbar (Explorer may be restarting)";
            if (!hotkeyRegistered)
            {
                status += "\nWin+F2 isn't available: another app is using it";
            }
            if (trayIcon.TooltipText != status)
            {
                trayIcon.TooltipText = status;
            }

            // TrayDark.ico is a white glyph for a dark taskbar, TrayLight.ico a black one for a light taskbar
            bool lightTaskbar = IsTaskbarLight();
            if (trayIconIsLight != lightTaskbar)
            {
                trayIconIsLight = lightTaskbar;
                trayIcon.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(lightTaskbar ? "pack://application:,,,/res/TrayLight.ico" : "pack://application:,,,/res/TrayDark.ico"));
            }
        }

        private bool? trayIconIsLight;

        private static bool IsTaskbarLight()
        {
            // The taskbar follows Windows' "mode" setting, not the apps setting
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
        }

        // In High Contrast, drop the translucent backdrop and dimmed text so Windows' contrast colours apply unaltered
        private void ApplyContrastMode()
        {
            bool highContrast = SystemParameters.HighContrast;
            Background = highContrast ? SystemColors.WindowBrush : normalBackground;
            headingLabel.Opacity = highContrast ? 1 : 0.75;
            beginLabel.Opacity = highContrast ? 1 : 0.75;
        }

        private Brush normalBackground;

        private const double DesignWidth = 718;
        private const double DesignHeight = 458;

        // At large display scaling (or on a small screen) the fixed-size window can be bigger than the screen. Shrink it to the
        // work area of its monitor; the content then scrolls (U5). At normal sizes this leaves the window exactly as designed.
        private void FitToWorkArea()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }
            System.Drawing.Rectangle area = Screen.FromHandle(hwnd).WorkingArea;
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double areaLeft = area.Left / dpi.DpiScaleX;
            double areaTop = area.Top / dpi.DpiScaleY;
            double areaWidth = area.Width / dpi.DpiScaleX;
            double areaHeight = area.Height / dpi.DpiScaleY;

            Width = Math.Min(DesignWidth, areaWidth);
            Height = Math.Min(DesignHeight, areaHeight);

            // Pull it back on screen if it now hangs off an edge (Left/Top are NaN until Windows has placed the window)
            if (!double.IsNaN(Left) && (Left < areaLeft || Left + Width > areaLeft + areaWidth))
            {
                Left = areaLeft + (areaWidth - Width) / 2;
            }
            if (!double.IsNaN(Top) && (Top < areaTop || Top + Height > areaTop + areaHeight))
            {
                Top = areaTop + (areaHeight - Height) / 2;
            }
        }

        private bool hotkeyRegistered = true;

        private void trayIcon_LeftClick(Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
        {
            // Left-clicking the tray icon opens the settings, as is conventional for tray apps
            if (!IsVisible)
            {
                ShowMenuItem_Click(null, null);
            }
            Activate();
        }


        public void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int mt = 0;
            int ml = 0;
            int mb = 0;
            int mr = 0;

            // When the user clicks Apply, say which box is wrong and put the cursor in it, instead of silently not applying anything.
            // Startup and the Win+F2 hotkey (sender == null) skip this: they apply the stored settings, which only ever hold in-range values,
            // and must not pop up a dialog from a hidden window.
            if (sender != null)
            {
                (System.Windows.Controls.TextBox box, string name, int min, int max)[] fields =
                {
                    (cornerRadiusInput, "Corner radius", 0, Types.SegmentSettings.MaxCornerRadius),
                    (mTopInput, "Top margin", Types.SegmentSettings.MinMargin, Types.SegmentSettings.MaxMargin),
                    (mBottomInput, "Bottom margin", Types.SegmentSettings.MinMargin, Types.SegmentSettings.MaxMargin),
                    (mLeftInput, "Left margin", Types.SegmentSettings.MinMargin, Types.SegmentSettings.MaxMargin),
                    (mRightInput, "Right margin", Types.SegmentSettings.MinMargin, Types.SegmentSettings.MaxMargin),
                };
                foreach (var field in fields)
                {
                    if (field.box.Text != string.Empty && (!int.TryParse(field.box.Text, out int value) || value < field.min || value > field.max))
                    {
                        System.Windows.MessageBox.Show(
                            $"{field.name} must be a whole number from {field.min} to {field.max}. Nothing was applied.",
                            "ReRoundedTB", MessageBoxButton.OK, MessageBoxImage.Warning);
                        field.box.Focus();
                        field.box.SelectAll();
                        return;
                    }
                }
            }

            int.TryParse(mTopInput.Text, out mt);
            int.TryParse(mLeftInput.Text, out ml);
            int.TryParse(mBottomInput.Text, out mb);
            int.TryParse(mRightInput.Text, out mr);

            // The wait below pumps messages, so a second click (or the hotkey) could re-enter Apply mid-way
            if (applying)
            {
                return;
            }
            applying = true;
            try
            {
                ApplyCore(mt, ml, mb, mr);
            }
            finally
            {
                applying = false;
            }
        }

        private bool applying;
        private bool paused;

        // Pumps messages while waiting, because the worker uses Dispatcher.Invoke
        private void StopWorker()
        {
            if (taskbarThread.IsBusy)
            {
                taskbarThread.CancelAsync();
                while (taskbarThread.IsBusy)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        // Puts the normal taskbar back without quitting; unticking (or Apply) reshapes it again
        private void PauseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Apply and Pause both wait on the worker while pumping messages; don't let one start inside the other's wait
            if (applying)
            {
                PauseMenuItem.IsChecked = paused;
                return;
            }
            if (!PauseMenuItem.IsChecked)
            {
                ApplyButton_Click(null, null);
                TrayIconCheck();
                return;
            }

            applying = true;
            try
            {
                StopWorker();
                if (shouldReallyDieNoReally)
                {
                    return;
                }
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    try
                    {
                        Taskbar.ResetTaskbar(taskbar, activeSettings);
                    }
                    catch (InvalidOperationException) { }
                }
                if (activeSettings.AutoHide > 0)
                {
                    AutoHide(false, taskbarDetails);
                }
                paused = true;
            }
            finally
            {
                applying = false;
            }
            TrayIconCheck();
        }

        private void ResetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            const string question = "Reset all ReRoundedTB settings (corner radius, margins and options) to their defaults?\n\n" +
                "The taskbar is reshaped with the defaults straight away, and this can't be undone. Run at startup isn't changed.";
            // Opened from the tray, often with the settings window hidden: own it when visible, otherwise keep it in front
            MessageBoxResult answer = IsVisible
                ? System.Windows.MessageBox.Show(this, question, "ReRoundedTB", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                : System.Windows.MessageBox.Show(question, "ReRoundedTB", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }
            Types.Settings defaults = Types.Settings.CreateDefault(isWindows11);
            defaults.Version = activeSettings.Version;
            defaults.IsNotFirstLaunch = true;
            defaults.HasSeenTrayNotice = activeSettings.HasSeenTrayNotice;
            activeSettings = defaults;
            LoadSettingsIntoUi();
            ApplyButton_Click(null, null);
        }

        private void ApplyCore(int mt, int ml, int mb, int mr)
        {
            // Stop the worker before touching settings or redrawing: it reads and writes activeSettings too (hover mode flips
            // ShowTray/ShowWidgets) and redraws the same taskbars, so letting both run at once can leave inconsistent regions
            StopWorker();
            // The wait pumps messages, so the user may have chosen Close meanwhile; don't redraw a taskbar that's just been reset
            if (shouldReallyDieNoReally)
            {
                return;
            }
            // Applying (including the Win+F2 hotkey) ends a pause
            paused = false;
            PauseMenuItem.IsChecked = false;

            activeSettings.AutoHide = autoHideComboBox.SelectedIndex;
            activeSettings.IsDynamic = (bool)dynamicCheckBox.IsChecked;
            activeSettings.IsCentred = Taskbar.CheckIfCentred(isWindows11);
            activeSettings.ShowTray = (bool)showTrayCheckBox.IsChecked;
            activeSettings.ShowWidgets = (bool)showWidgetsCheckBox.IsChecked;
            activeSettings.CompositionCompat = (bool)compositionFixCheckBox.IsChecked;
            activeSettings.FillOnMaximise = (bool)fillMaximisedCheckBox.IsChecked;
            activeSettings.FillOnTaskSwitch = (bool)fillAltTabCheckBox.IsChecked;
            activeSettings.ShowSegmentsOnHover = (bool)showSegmentsOnHoverCheckBox.IsChecked;

            try
            {
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    int isFullTest = taskbar.TrayRect.Left - taskbar.AppListRect.Right;
                    if (!activeSettings.IsDynamic || (isFullTest <= taskbar.ScaleFactor * 25 && isFullTest > 0 && taskbar.TrayRect.Left != 0))
                    {
                        Taskbar.UpdateSimpleTaskbar(taskbar, activeSettings);
                    }
                    else
                    {
                        Taskbar.UpdateDynamicTaskbar(taskbar, activeSettings);
                    }
                }
            }
            catch (InvalidOperationException aaaa)
            {
                interaction.AddLog(aaaa.Message);
            }


            taskbarThread.RunWorkerAsync((mt, ml, mb, mr, 0));


            if (activeSettings.AutoHide < 1)
            {
                AutoHide(false, taskbarDetails);
            }
            else
            {
                AutoHide(true, taskbarDetails);
            }
            interaction.WriteJSON();
            TrayIconCheck();
            UpdateUi();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (shouldReallyDieNoReally == false)
            {
                e.Cancel = true;
                // Closing only hides the window; say so once, so the taskbar staying shaped isn't a surprise
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show ReRoundedTB";
                if (!activeSettings.HasSeenTrayNotice)
                {
                    activeSettings.HasSeenTrayNotice = true;
                    interaction.WriteJSON();
                    // Shown after OnClosing returns: a modal box pumping messages mid-close would let tray > Close re-enter Close()
                    Dispatcher.BeginInvoke(() => System.Windows.MessageBox.Show(
                        "ReRoundedTB is still running in the system tray, keeping your taskbar shaped.\n\n" +
                        "Click its tray icon to open these settings again (on Windows 11 it may be under the ^ arrow next to the clock). " +
                        "To get the normal taskbar back, right-click the icon and choose Pause or Close ReRoundedTB.",
                        "ReRoundedTB", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, System.Windows.MessageBoxOptions.DefaultDesktopOnly));
                }
            }
            else
            {


                try
                {
                    taskbarThread.CancelAsync();
                }
                catch (Exception aaaa)
                {
                    interaction.AddLog(aaaa.Message);
                }
                while (taskbarThread.IsBusy == true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }

                try
                {
                    foreach (var tbDeets in taskbarDetails)
                    {
                        Taskbar.ResetTaskbar(tbDeets, activeSettings);
                    }
                    if (activeSettings.AutoHide > 0)
                    {
                        AutoHide(false, taskbarDetails);
                    }
                }
                catch (InvalidOperationException aaaa)
                {
                    interaction.AddLog($"Taskbar structure changed on exit:\n{aaaa.Message}");
                }
                interaction.AddLog("Exiting ReRoundedTB.");
            }
            if (!isAlreadyRunning)
            {
                interaction.WriteJSON();
            }
        }

        /// <summary>
        /// Puts every taskbar back to how Windows draws it. Uses only Win32 calls and plain fields, so the crash handler can call it from any thread.
        /// </summary>
        public void RestoreTaskbars()
        {
            Types.Settings settings = activeSettings;
            if (settings == null)
            {
                return;
            }

            try
            {
                taskbarThread.CancelAsync();
            }
            catch (InvalidOperationException) { }
            // The worker checks for cancellation about every 100 ms; let it stop so it can't redraw a taskbar after the reset
            System.Threading.Thread.Sleep(500);

            foreach (Types.Taskbar taskbar in taskbarDetails.ToArray())
            {
                try
                {
                    Taskbar.ResetTaskbar(taskbar, settings);
                }
                catch (Exception) { }
            }
            if (settings.AutoHide > 0 && taskbarDetails.Count > 0)
            {
                try
                {
                    AutoHide(false, taskbarDetails);
                }
                catch (Exception) { }
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Close any popups, but not this window: closing it here takes the "hide to tray" path, which shows the one-time
            // "still running in the tray" notice and holds up the real close below until it's dismissed (smoke test 2026-10-09)
            for (int windowCount = App.Current.Windows.Count - 1; windowCount >= 0; windowCount--)
            {
                if (App.Current.Windows[windowCount] != this)
                {
                    App.Current.Windows[windowCount].Close();
                }
            }

            shouldReallyDieNoReally = true;

            Close();
        }

        public void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible == false)
            {
                Visibility = Visibility.Visible;
                ShowMenuItem.Header = "Hide ReRoundedTB";
            }
            else
            {
                // Close any popups - leave main window for now
                for (int windowCount = App.Current.Windows.Count - 1; windowCount >= 0; windowCount--)
                {
                    App.Current.Windows[windowCount].Close();
                }
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show ReRoundedTB";
            }
        }

        private void Startup_Clicked(object sender, RoutedEventArgs e)
        {
            string link = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupLinkName);
            // Follow what the user asked for (the item's new checked state), not whether the file happens to exist
            bool wanted = StartupMenuItem.IsChecked;
            bool ok = true;
            if (wanted)
            {
                ok = EnableStartup();
            }
            else
            {
                try
                {
                    System.IO.File.Delete(link);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    ok = false;
                }
            }

            // Always show the real state, so the tick can't disagree with what's on disk
            StartupMenuItem.IsChecked = System.IO.File.Exists(link);
            if (!ok)
            {
                System.Windows.MessageBox.Show(
                    wanted ? "ReRoundedTB couldn't add itself to your startup apps." : "ReRoundedTB couldn't remove itself from your startup apps.",
                    "ReRoundedTB", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public bool EnableStartup()
        {
            try
            {
                string shortcutFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!Directory.Exists(shortcutFolder))
                {
                    Directory.CreateDirectory(shortcutFolder);
                }
                // Late-bound WScript.Shell, so the build doesn't need a COM interop reference (which only Visual Studio's MSBuild can resolve)
                dynamic shellClass = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                string rtbStartupLink = Path.Combine(shortcutFolder, StartupLinkName);
                dynamic shortcut = shellClass.CreateShortcut(rtbStartupLink);
                // On .NET 6+ GetCommandLineArgs()[0] is the .dll, which Windows can't launch; ProcessPath is the .exe
                shortcut.TargetPath = Environment.ProcessPath;
                shortcut.IconLocation = Environment.ProcessPath;
                shortcut.Arguments = "";
                shortcut.Description = "Start ReRoundedTB";
                shortcut.Save();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private const string StartupLinkName = "ReRoundedTB.lnk";
        private const string LegacyStartupLinkName = "RoundedTB.lnk";

        // Before the rename the startup shortcut was RoundedTB.lnk. Move ours to the new name,
        // but leave a shortcut belonging to a separate install of the original RoundedTB alone.
        private void MigrateLegacyStartupShortcut()
        {
            try
            {
                string legacyLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), LegacyStartupLinkName);
                if (!System.IO.File.Exists(legacyLink))
                {
                    return;
                }
                dynamic shellClass = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                string target = shellClass.CreateShortcut(legacyLink).TargetPath;
                if (string.Equals(Path.GetDirectoryName(target)?.TrimEnd('\\'), AppContext.BaseDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.File.Delete(legacyLink);
                    EnableStartup();
                }
            }
            catch (Exception)
            {
            }
        }

        private void dynamicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled = true;
            showSegmentsOnHoverCheckBox.IsEnabled = true;
            showSegmentsOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled = true;
            showTrayCheckBox.IsChecked = true;
            
            if (!isWindows11)
            {
                splitHelpButton.Visibility = Visibility.Visible;
                // Only when the user ticks it, not while settings load (at startup or on Reset)
                if (IsVisible && !loadingSettingsIntoUi)
                {
                    splitHelpButton_Click(null, null);
                }
            }

        }

        private void dynamicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

            centredCheckBox.IsEnabled = false;
            centredCheckBox.IsChecked = false;
            showSegmentsOnHoverCheckBox.IsEnabled = false;
            showSegmentsOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;
            
            if (!isWindows11)
            {
                splitHelpButton.Visibility = Visibility.Hidden;
            }
        }

        private void cornerRadiusSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            SaveSliderRadius();
        }

        private void SaveSliderRadius()
        {
            int check = Convert.ToInt32(Math.Round(cornerRadiusSlider.Value));
            cornerRadiusInput.Text = check.ToString();

            switch (selectedSegment)
            {
                default:
                    break;

                case 0:
                    activeSettings.SimpleTaskbarLayout.CornerRadius = check;
                    break;

                case 1:
                    activeSettings.DynamicAppListLayout.CornerRadius = check;
                    break;

                case 2:
                    activeSettings.DynamicTrayLayout.CornerRadius = check;
                    break;

                case 3:
                    activeSettings.DynamicWidgetsLayout.CornerRadius = check;
                    break;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            Debug.WriteLine("AAAAA");
            base.OnSourceInitialized(e);


            IntPtr handle = new WindowInteropHelper(this).Handle;
            MainHwnd = handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(interaction.HwndHook);
            // Fails if another app already owns Win+F2; the tray tooltip then says so
            hotkeyRegistered = LocalPInvoke.RegisterHotKey(handle, 9000, 0x8, 0x71);
            Debug.WriteLine("KEY: " + hotkeyRegistered);
            Debug.WriteLine(handle);
            Debug.WriteLine((int)Types.KeyModifier.WinKey);
            Debug.WriteLine(System.Windows.Forms.Keys.J.GetHashCode());
            // Shown (at 0.1% opacity) and hidden straight away rather than never shown: WPF only attaches the window's
            // content on first show, and WPF-UI registers the tray icon from the first render, so a never-shown window
            // has no tray icon (security review H-1, 2026-10-09)
            Visibility = Visibility.Hidden;
            Opacity = 1;
        }

        private void splitHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Infobox ib = new Infobox();
            ib.Title = "ReRoundedTB - Split mode configuration";
            ib.titleBlock.Text = "How to use Split Mode";
            ib.bodyBlock.Text = "Split mode has a couple of limitations and requires a small amount of setup to get working properly.\n\nLimitations:\n1) Split mode doesn't resize itself automatically.\n2) Toolbars are not compatible with split mode currently, and will need to be disabled apart from one (more on that in a moment).\n3) Split mode only works when the taskbar is horizontal at the top or bottom of the screen.\n\nSetup:\n1) Right-click the taskbar and disable \"Lock the taskbar\".\n2) Right-click it again and turn off any existing toolbars.\n3) Right-click a third time, select Toolbars > Desktop.\n4) Use the small || handle to resize the taskbar as you please.";
            ib.ShowDialog();
        }

        private void compositionFixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (IsVisible && !loadingSettingsIntoUi)
            {
                Infobox ib = new Infobox();
                ib.Height = 450;
                ib.Title = "ReRoundedTB - TranslucentTB compatibility";
                ib.titleBlock.Text = "Compatibility with TranslucentTB";
                ib.bodyBlock.Text = "\nTranslucentTB is a utility that allows you to customise the opacity, blur and colour of the taskbar seamlessly with significantly finer control than other tools. Enable this option to allow ReRoundedTB and TranslucentTB to work together.\n\nThis is necessary due to a bug in Windows (it's not the fault of RoundedTB or TranslucentTB), and you might encounter some minor flickering when the taskbar \"updates\" (changes size, roundness or position). This is usually minimal, but if it bothers you, use either ReRoundedTB or TranslucentTB on its own.\n\nTranslucentTB is the original aesthetic taskbar mod for Windows 10 and the project that inspired RoundedTB. Go check it out!";
                ib.ShowDialog();
            }
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }

        private void fillMaximisedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isWindows11)
            {
                fillAltTabCheckBox.IsEnabled = true;
            }
        }

        private void fillMaximisedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            fillAltTabCheckBox.IsEnabled = false;
            fillAltTabCheckBox.IsChecked = false;

        }

        private void showSegmentsOnHoverCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;

            showWidgetsCheckBox.IsEnabled = false;
            showWidgetsCheckBox.IsChecked = false;
        }

        private void showSegmentsOnHoverCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = true;
            showTrayCheckBox.IsChecked = true;

            showWidgetsCheckBox.IsEnabled = true;
            showWidgetsCheckBox.IsChecked = true;
        }

        private void taskbarRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            trayRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            widgetsRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            dynamicCheckBox.Visibility = Visibility.Visible;
            showTrayCheckBox.Visibility = Visibility.Hidden;
            showWidgetsCheckBox.Visibility = Visibility.Hidden;

            if (activeSettings.IsDynamic)
            {
                selectedSegment = 1;

                cornerRadiusInput.Text = activeSettings.DynamicAppListLayout.CornerRadius.ToString();
                SetRadiusSliderFromCode(activeSettings.DynamicAppListLayout.CornerRadius);
                mTopInput.Text = activeSettings.DynamicAppListLayout.MarginTop.ToString();
                mLeftInput.Text = activeSettings.DynamicAppListLayout.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.DynamicAppListLayout.MarginBottom.ToString();
                mRightInput.Text = activeSettings.DynamicAppListLayout.MarginRight.ToString();
            }
            else
            {
                selectedSegment = 0;

                cornerRadiusInput.Text = activeSettings.SimpleTaskbarLayout.CornerRadius.ToString();
                SetRadiusSliderFromCode(activeSettings.SimpleTaskbarLayout.CornerRadius);
                mTopInput.Text = activeSettings.SimpleTaskbarLayout.MarginTop.ToString();
                mLeftInput.Text = activeSettings.SimpleTaskbarLayout.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.SimpleTaskbarLayout.MarginBottom.ToString();
                mRightInput.Text = activeSettings.SimpleTaskbarLayout.MarginRight.ToString();
            }
            UpdateSegmentLabels();
        }

        private void trayRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            trayRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            widgetsRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            dynamicCheckBox.Visibility = Visibility.Hidden;
            showTrayCheckBox.Visibility = Visibility.Visible;
            showWidgetsCheckBox.Visibility = Visibility.Hidden;

            selectedSegment = 2;

            cornerRadiusInput.Text = activeSettings.DynamicTrayLayout.CornerRadius.ToString();
            SetRadiusSliderFromCode(activeSettings.DynamicTrayLayout.CornerRadius);
            mTopInput.Text = activeSettings.DynamicTrayLayout.MarginTop.ToString();
            mLeftInput.Text = activeSettings.DynamicTrayLayout.MarginLeft.ToString();
            mBottomInput.Text = activeSettings.DynamicTrayLayout.MarginBottom.ToString();
            mRightInput.Text = activeSettings.DynamicTrayLayout.MarginRight.ToString();
            UpdateSegmentLabels();
        }

        private void widgetsRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            trayRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            widgetsRectStandIn.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            dynamicCheckBox.Visibility = Visibility.Hidden;
            showTrayCheckBox.Visibility = Visibility.Hidden;
            showWidgetsCheckBox.Visibility = Visibility.Visible;

            selectedSegment = 3;

            cornerRadiusInput.Text = activeSettings.DynamicWidgetsLayout.CornerRadius.ToString();
            SetRadiusSliderFromCode(activeSettings.DynamicWidgetsLayout.CornerRadius);
            mTopInput.Text = activeSettings.DynamicWidgetsLayout.MarginTop.ToString();
            mLeftInput.Text = activeSettings.DynamicWidgetsLayout.MarginLeft.ToString();
            mBottomInput.Text = activeSettings.DynamicWidgetsLayout.MarginBottom.ToString();
            mRightInput.Text = activeSettings.DynamicWidgetsLayout.MarginRight.ToString();
            UpdateSegmentLabels();
        }

        private void mTopInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mTopInput.Text, out int check) && check >= Types.SegmentSettings.MinMargin && check <= Types.SegmentSettings.MaxMargin)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginTop = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginTop = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginTop = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginTop = check;
                        break;
                }
            }
        }

        private void mBottomInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mBottomInput.Text, out int check) && check >= Types.SegmentSettings.MinMargin && check <= Types.SegmentSettings.MaxMargin)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginBottom = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginBottom = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginBottom = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginBottom = check;
                        break;
                }
            }
        }

        private void mLeftInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mLeftInput.Text, out int check) && check >= Types.SegmentSettings.MinMargin && check <= Types.SegmentSettings.MaxMargin)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginLeft = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginLeft = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginLeft = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginLeft = check;
                        break;
                }
            }
        }

        private void mRightInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mRightInput.Text, out int check) && check >= Types.SegmentSettings.MinMargin && check <= Types.SegmentSettings.MaxMargin)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginRight = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginRight = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginRight = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginRight = check;
                        break;
                }
            }
        }

        private void cornerRadiusInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(cornerRadiusInput.Text, out int check) && check >= 0 && check <= Types.SegmentSettings.MaxCornerRadius)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.CornerRadius = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.CornerRadius = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.CornerRadius = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.CornerRadius = check;
                        break;
                }

                SetRadiusSliderFromCode(check);
            }
        }

        // Code that moves the slider has already set the textbox (which may hold more than the slider's maximum of 48), so ValueChanged must ignore it
        private bool settingRadiusSliderFromCode;

        private void SetRadiusSliderFromCode(int value)
        {
            settingRadiusSliderFromCode = true;
            try
            {
                cornerRadiusSlider.Value = value;
            }
            finally
            {
                settingRadiusSliderFromCode = false;
            }
        }

        private void cornerRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (settingRadiusSliderFromCode)
            {
                return;
            }
            cornerRadiusInput.Text = Math.Round(cornerRadiusSlider.Value).ToString();
            // Arrow keys and clicks on the track don't raise DragCompleted, so save here too
            SaveSliderRadius();
        }
    }
}
