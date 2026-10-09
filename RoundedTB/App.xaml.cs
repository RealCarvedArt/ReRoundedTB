using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Brand blue from the app icon. Used instead of the Windows accent colour so the UI matches the icon and banner.
        public static readonly System.Windows.Media.Color BrandAccent = System.Windows.Media.Color.FromRgb(0x32, 0x80, 0xB1);

        private static int crashHandled;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        protected override void OnStartup(StartupEventArgs e)
        {
            // Registered first so a crash anywhere, including MainWindow's constructor, still restores the taskbar
            DispatcherUnhandledException += (_, args) =>
            {
                args.Handled = true;
                HandleCrash(args.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) => HandleCrash(args.ExceptionObject as Exception);

            base.OnStartup(e);
            // Match the Windows light/dark setting (each window follows changes via SystemThemeWatcher), but keep the brand accent
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme(false);
            ApplyBrandAccent(Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme());
            // Accent shades depend on light/dark, so recompute them whenever the theme changes
            Wpf.Ui.Appearance.ApplicationThemeManager.Changed += (theme, _) =>
            {
                if (reloadingTheme)
                {
                    return;
                }
                ApplyBrandAccent(theme);
                // This event comes after WPF-UI has swapped in the new theme's brushes, and they picked up the old theme's
                // accent shade (smoke test: light shade with dark-theme black text, 4.30:1). Swap them in again now the accent is right.
                reloadingTheme = true;
                try
                {
                    Wpf.Ui.Appearance.ApplicationThemeManager.Apply(theme, Wpf.Ui.Controls.WindowBackdropType.Mica, false);
                }
                finally
                {
                    reloadingTheme = false;
                }
            };
        }

        private static bool reloadingTheme;

        // Without this, Explorer keeps the custom region after a crash (and with auto-hide, an invisible click-through taskbar) until it's restarted
        private static void HandleCrash(Exception ex)
        {
            if (System.Threading.Interlocked.Exchange(ref crashHandled, 1) != 0)
            {
                return;
            }
            try
            {
                RoundedTB.MainWindow.Instance?.RestoreTaskbars();
            }
            catch (Exception) { }

            // Only the exception type: messages can contain local paths (and so the username), which end up in bug-report screenshots.
            // Native MessageBox so it can be topmost and in front: the app usually has no visible window to own it, and a smoke test
            // showed WPF's MessageBox opening behind other windows.
            const uint MB_ICONERROR = 0x10, MB_SETFOREGROUND = 0x10000, MB_TOPMOST = 0x40000;
            MessageBoxW(IntPtr.Zero,
                "ReRoundedTB ran into an unexpected error and has closed. Your taskbar has been put back to normal; if it still looks wrong, restart Windows Explorer from Task Manager.\n\n" +
                $"Error: {ex?.GetType().Name}",
                "ReRoundedTB", MB_ICONERROR | MB_SETFOREGROUND | MB_TOPMOST);
            // The dialog pumps messages, so the worker or the UI may have reshaped the taskbar while it was open; reset again before exiting
            try
            {
                RoundedTB.MainWindow.Instance?.RestoreTaskbars();
            }
            catch (Exception) { }
            Environment.Exit(1);
        }

        private static void ApplyBrandAccent(Wpf.Ui.Appearance.ApplicationTheme theme)
        {
            // Explicit shades from the icon: the single-colour overload lightens the accent heavily in dark mode, washing the blue out.
            // Light mode fills accent buttons with the primary shade; the icon blue gives white text only 4.32:1 (measured), so use
            // a slightly darker blue there (4.88:1)
            Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(
                BrandAccent,
                theme == Wpf.Ui.Appearance.ApplicationTheme.Light
                    ? System.Windows.Media.Color.FromRgb(0x2F, 0x77, 0xA5)
                    : System.Windows.Media.Color.FromRgb(0x32, 0x80, 0xB1),
                System.Windows.Media.Color.FromRgb(0x3A, 0x8F, 0xC4),
                System.Windows.Media.Color.FromRgb(0x47, 0xA8, 0xDA));
        }
    }
}
