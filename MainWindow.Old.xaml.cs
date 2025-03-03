using System.Windows;

namespace BarcodeGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GenerateTab_Checked(object sender, RoutedEventArgs e)
        {
            SetTabVisibility(true, false, false);
        }

        private void ManualTab_Checked(object sender, RoutedEventArgs e)
        {
            SetTabVisibility(false, true, false);
        }

        private void SettingsTab_Checked(object sender, RoutedEventArgs e)
        {
            SetTabVisibility(false, false, true);
        }

        private void SetTabVisibility(bool generateVisible, bool manualVisible, bool settingsVisible)
        {
            if (GenerateTab != null)
                GenerateTab.Visibility = generateVisible ? Visibility.Visible : Visibility.Collapsed;
            if (ManualTab != null)
                ManualTab.Visibility = manualVisible ? Visibility.Visible : Visibility.Collapsed;
            if (SettingsTab != null)
                SettingsTab.Visibility = settingsVisible ? Visibility.Visible : Visibility.Collapsed;
        }


        private void GenerateBarcodes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Generowanie kodów kreskowych...");
        }

        private void GenerateManualBarcodes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Generowanie etykiet z ręcznie wprowadzonymi kodami...");
        }

        private void ImportCodes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Importowanie kodów...");
        }
    }
}
