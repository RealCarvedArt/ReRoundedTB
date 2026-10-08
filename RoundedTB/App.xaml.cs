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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Match the Windows light/dark setting (each window follows changes via SystemThemeWatcher), but keep the brand accent
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme(false);
            ApplyBrandAccent(Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme());
            // Accent shades depend on light/dark, so recompute them whenever the theme changes
            Wpf.Ui.Appearance.ApplicationThemeManager.Changed += (theme, _) => ApplyBrandAccent(theme);
        }

        private static void ApplyBrandAccent(Wpf.Ui.Appearance.ApplicationTheme theme)
        {
            // Explicit shades from the icon: the single-colour overload lightens the accent heavily in dark mode, washing the blue out
            Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(
                BrandAccent,
                System.Windows.Media.Color.FromRgb(0x32, 0x80, 0xB1),
                System.Windows.Media.Color.FromRgb(0x3A, 0x8F, 0xC4),
                System.Windows.Media.Color.FromRgb(0x47, 0xA8, 0xDA));
        }
    }
}
