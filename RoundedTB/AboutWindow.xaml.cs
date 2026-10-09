using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Diagnostics;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Wpf.Ui.Controls.FluentWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica, updateAccents: false);
            // Single source of truth: <Version> in RoundedTB.csproj
            subtitleBlock.Text = "Version " + typeof(AboutWindow).Assembly.GetName().Version.ToString(3);

            // In High Contrast, drop the translucent backdrop and dimmed text so Windows' contrast colours apply unaltered
            if (SystemParameters.HighContrast)
            {
                Background = SystemColors.WindowBrush;
                foreach (TextBlock block in new[] { titleBlock, subtitleBlock, bodyBlockMain, bodyBlock0, bodyBlock1, bodyBlock2, bodyBlock3, bodyBlock4 })
                {
                    block.Opacity = 1;
                }
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenWithShell(e.Uri.ToString());
        }

        private void configButton_Click(object sender, RoutedEventArgs e)
        {
            OpenWithShell(((MainWindow)Application.Current.MainWindow).configPath);
        }

        // .NET Core defaults UseShellExecute to false, so Process.Start(url/file) throws instead of opening it
        private static void OpenWithShell(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception) { }
        }
    }
}
