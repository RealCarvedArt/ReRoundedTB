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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Match the Windows light/dark setting; each window then follows changes via SystemThemeWatcher
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme();
        }
    }
}
