using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Drawing;
//using System.Windows.Shapes;
using Microsoft.Win32;
using ZXing;
using System.IO;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;

namespace BarcodeGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializePaperSizes();
            LoadTemplateNames();
            InitializeBarcodeTypes();
            ShowGrid(HomeGrid);
            ShowGrid(Generate_Grid_);
            ShowGrid(Generate_Settings_Grid);
            history_LB.SelectionChanged += history_LB_SelectionChanged;
            UpdateHistoryListBox();

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove(); // Przesuwa okno, gdy lewy przycisk myszy jest wciśnięty
            }
        }


        private void InitializePaperSizes()
        {
            var paperSizes = new List<string> { "A3", "A4", "A5", "Custom" };
            formatCb.ItemsSource = paperSizes;
            formatCb.SelectedIndex = 1; // Default to A4

        }

        private void formatCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (formatCb.SelectedItem != null)
            {
                var selectedSize = formatCb.SelectedItem.ToString();
                switch (selectedSize)
                {
                    case "A3":
                        sizeXTb.Text = "297"; // Width in millimeters
                        sizeYTb.Text = "420"; // Height in millimeters
                        break;
                    case "A4":
                        sizeXTb.Text = "210";
                        sizeYTb.Text = "297";
                        break;
                    case "A5":
                        sizeXTb.Text = "148";
                        sizeYTb.Text = "210";
                        break;
                    case "Custom":
                        // Allow user input
                        sizeXTb.IsEnabled = true;
                        sizeYTb.IsEnabled = true;
                        return;
                }
                sizeXTb.IsEnabled = false;
                sizeYTb.IsEnabled = false;
                CalculateLabelSize();
            }
        }

        private Dictionary<TextBox, string> defaultValues = new Dictionary<TextBox, string>();

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!defaultValues.ContainsKey(tb))
                {
                    defaultValues[tb] = tb.Text;  // Zapisanie domyślnej wartości, jeśli nie została wcześniej zapisana
                }

                if (!string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = "";  // Czyści pole, jeśli zawiera jakikolwiek tekst
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = defaultValues.ContainsKey(tb) ? defaultValues[tb] : "0";  // Przywraca domyślną wartość lub "0"
            }
        }




        private void InitializeBarcodeTypes()
        {
            var barcodeTypes = new List<string> { "EAN_13", "EAN_8", "CODE_128", "CODE_39", "QR_CODE" };
            barcodeTypeCB.ItemsSource = barcodeTypes;
            //barcodeTypeCB.SelectedIndex = 0; // Default to EAN_13

            // Restore last selected barcode type
            string lastSelectedType = Properties.Settings.Default.LastSelectedBarcodeType;
            if (!string.IsNullOrEmpty(lastSelectedType) && barcodeTypes.Contains(lastSelectedType))
            {
                barcodeTypeCB.SelectedItem = lastSelectedType;
            }
            else
            {
                barcodeTypeCB.SelectedIndex = 0;
            }

            // Add selection changed handler
            barcodeTypeCB.SelectionChanged += BarcodeTypeCB_SelectionChanged;
        }

        private void BarcodeTypeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (barcodeTypeCB.SelectedItem != null)
            {
                var selectedType = barcodeTypeCB.SelectedItem.ToString();
                switch (selectedType)
                {
                    case "EAN_13":
                        break;
                    case "EAN_8":
                        break;
                    case "CODE_128":
                        break;
                    case "CODE_39":
                        break;
                    case "QR_CODE":
                        break;
                }
                Properties.Settings.Default.LastSelectedBarcodeType = barcodeTypeCB.SelectedItem.ToString();
                Properties.Settings.Default.Save();
            }
        }

        private void NumberTB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, "^[0-9]*$"); // Accept only digits 0-9
        }

        private void CalculateLabelSize()
        {
            // Ensure all necessary text fields are not null and contain valid values
            if (no_labels_Tb != null && no_column_Tb != null && sizeXTb != null && sizeYTb != null &&
                mariginO_X_Tb != null && mariginO_Y_Tb != null && mariginI_X_Tb != null && mariginI_Y_Tb != null &&
                int.TryParse(no_labels_Tb.Text, out int numberOfLabels) &&
                int.TryParse(no_column_Tb.Text, out int numberOfColumns) &&
                double.TryParse(sizeXTb.Text, out double sheetWidth) &&
                double.TryParse(sizeYTb.Text, out double sheetHeight) &&
                double.TryParse(mariginO_X_Tb.Text, out double marginOX) &&
                double.TryParse(mariginO_Y_Tb.Text, out double marginOY) &&
                double.TryParse(mariginI_X_Tb.Text, out double marginIX) &&
                double.TryParse(mariginI_Y_Tb.Text, out double marginIY) &&
                numberOfLabels >= 1 && numberOfColumns >= 1)
            {
                double numberOfRows = Math.Ceiling((double)numberOfLabels / numberOfColumns);
                double effectiveSheetWidth = sheetWidth - 2 * marginOX - (numberOfColumns - 1) * marginIX;
                double effectiveSheetHeight = sheetHeight - 2 * marginOY - (numberOfRows - 1) * marginIY;

                if (effectiveSheetWidth > 0 && effectiveSheetHeight > 0)
                {
                    double labelWidth = effectiveSheetWidth / numberOfColumns;
                    double labelHeight = effectiveSheetHeight / numberOfRows;

                    label_size_X_Tb.Text = Math.Floor(labelWidth).ToString();
                    label_size_Y_Tb.Text = Math.Floor(labelHeight).ToString();
                }
                else
                {
                    MessageBox.Show("The margins are too large for the given sheet size and number of labels/columns.");
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateLabelSize();
        }

        private void addtemplBtn_Click(object sender, RoutedEventArgs e)
        {
            // Save the template information
            var templateName = label_name_Tb.Text;
            var paperSize = formatCb.SelectedItem.ToString();
            var sheetWidth = sizeXTb.Text;
            var sheetHeight = sizeYTb.Text;
            var marginOX = mariginO_X_Tb.Text;
            var marginOY = mariginO_Y_Tb.Text;
            var marginIX = mariginI_X_Tb.Text;
            var marginIY = mariginI_Y_Tb.Text;
            var labelWidth = label_size_X_Tb.Text;
            var labelHeight = label_size_Y_Tb.Text;
            var numberOfLabels = no_labels_Tb.Text;
            var numberOfColumns = no_column_Tb.Text;

            // Save these settings to the application settings or a file
            Properties.Settings.Default.TemplateName = templateName;
            Properties.Settings.Default.PaperSize = paperSize;
            Properties.Settings.Default.SheetWidth = sheetWidth;
            Properties.Settings.Default.SheetHeight = sheetHeight;
            Properties.Settings.Default.MarginOX = marginOX;
            Properties.Settings.Default.MarginOY = marginOY;
            Properties.Settings.Default.MarginIX = marginIX;
            Properties.Settings.Default.MarginIY = marginIY;
            Properties.Settings.Default.LabelWidth = labelWidth;
            Properties.Settings.Default.LabelHeight = labelHeight;
            Properties.Settings.Default.NumberOfLabels = numberOfLabels;
            Properties.Settings.Default.NumberOfColumns = numberOfColumns;

            // Add the template name to the list of template names
            if (Properties.Settings.Default.TemplateNames == null)
            {
                Properties.Settings.Default.TemplateNames = new StringCollection();
            }
            if (!Properties.Settings.Default.TemplateNames.Contains(templateName))
            {
                Properties.Settings.Default.TemplateNames.Add(templateName);
            }
            Properties.Settings.Default.Save();

            // Reload the template names into the ComboBox
            LoadTemplateNames();

            MessageBox.Show("Template saved successfully!");
        }

        private void LoadTemplateNames()
        {
            var templateNames = Properties.Settings.Default.TemplateNames;
            if (templateNames != null)
            {
                var templates = templateNames.Cast<string>().ToList();
                labeltmplCB.ItemsSource = templates;

                // Restore last selected template
                string lastSelectedTemplate = Properties.Settings.Default.LastSelectedTemplate;
                if (!string.IsNullOrEmpty(lastSelectedTemplate) && templates.Contains(lastSelectedTemplate))
                {
                    labeltmplCB.SelectedItem = lastSelectedTemplate;
                }
            }

            // Add selection changed handler
            labeltmplCB.SelectionChanged += LabelTmplCB_SelectionChanged;
        }

        private void LabelTmplCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (labeltmplCB.SelectedItem != null)
            {
                // Save the selected template
                Properties.Settings.Default.LastSelectedTemplate = labeltmplCB.SelectedItem.ToString();
                Properties.Settings.Default.Save();
            }
        }

        private void previewtemplBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateTemplateInput())
                return;

            try
            {
                // Create and show the preview window with current values
                var previewWindow = new PreviewWindow();
                previewWindow.ShowPreview(
                    double.Parse(sizeXTb.Text),
                    double.Parse(sizeYTb.Text),
                    int.Parse(no_labels_Tb.Text),
                    int.Parse(no_column_Tb.Text),
                    double.Parse(label_size_X_Tb.Text),
                    double.Parse(label_size_Y_Tb.Text),
                    double.Parse(mariginO_X_Tb.Text),
                    double.Parse(mariginO_Y_Tb.Text),
                    double.Parse(mariginI_X_Tb.Text),
                    double.Parse(mariginI_Y_Tb.Text)
                );
                previewWindow.Owner = this;
                previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                previewWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating preview: {ex.Message}");
            }
        }

        private bool ValidateTemplateInput()
        {
            if (string.IsNullOrWhiteSpace(label_name_Tb.Text))
            {
                MessageBox.Show("Please enter a template name.");
                return false;
            }

            if (!double.TryParse(mariginO_X_Tb.Text, out _) || !double.TryParse(mariginO_Y_Tb.Text, out _) ||
                !double.TryParse(mariginI_X_Tb.Text, out _) || !double.TryParse(mariginI_Y_Tb.Text, out _))
            {
                MessageBox.Show("Please enter valid margin values.");
                return false;
            }

            return true;
        }

        private bool ValidateMarginsAndSize()
        {
            if (!double.TryParse(sizeXTb.Text, out double sheetWidth) ||
                !double.TryParse(sizeYTb.Text, out double sheetHeight) ||
                !double.TryParse(mariginO_X_Tb.Text, out double marginOX) ||
                !double.TryParse(mariginO_Y_Tb.Text, out double marginOY) ||
                !double.TryParse(mariginI_X_Tb.Text, out double marginIX) ||
                !double.TryParse(mariginI_Y_Tb.Text, out double marginIY))
            {
                MessageBox.Show("Please enter valid numerical values for all dimensions.");
                return false;
            }

            // Check if margins don't exceed page size
            if ((2 * marginOX) >= sheetWidth || (2 * marginOY) >= sheetHeight)
            {
                MessageBox.Show("Outer margins are too large for the selected paper size.");
                return false;
            }

            // Check if there's enough space for labels with inner margins
            int columns = int.Parse(no_column_Tb.Text);
            int totalLabels = int.Parse(no_labels_Tb.Text);
            int rows = (int)Math.Ceiling((double)totalLabels / columns);

            double availableWidth = sheetWidth - (2 * marginOX) - ((columns - 1) * marginIX);
            double availableHeight = sheetHeight - (2 * marginOY) - ((rows - 1) * marginIY);

            if (availableWidth <= 0 || availableHeight <= 0)
            {
                MessageBox.Show("The combination of margins and number of labels doesn't fit on the selected paper size.");
                return false;
            }

            return true;
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate barcode type selection
                if (barcodeTypeCB.SelectedItem == null)
                {
                    MessageBox.Show("Please select a barcode type.");
                    return;
                }
                string barcodeType = barcodeTypeCB.SelectedItem.ToString();

                // Validate template selection
                if (labeltmplCB.SelectedItem == null)
                {
                    MessageBox.Show("Please select a label template.");
                    return;
                }
                string templateName = labeltmplCB.SelectedItem.ToString();

                List<string> barcodesToGenerate = new List<string>();

                // Handle HomeGrid scenario - Generate unique barcodes
                if (HomeGrid.Visibility == Visibility.Visible)
                {
                    if (!int.TryParse(NumberTB.Text, out int numberOfBarcodes) || numberOfBarcodes <= 0)
                    {
                        MessageBox.Show("Please enter a valid number of barcodes to generate.");
                        return;
                    }

                    barcodesToGenerate = GenerateUniqueBarcodes(barcodeType, numberOfBarcodes);

                    // Save generated barcodes to database
                    SaveGeneratedBarcodes(barcodesToGenerate);
                }
                // Handle ManualGrid scenario - Use manually entered barcodes
                else if (ManualGrid.Visibility == Visibility.Visible)
                {
                    if (string.IsNullOrWhiteSpace(manualTB_.Text))
                    {
                        MessageBox.Show("Please enter at least one barcode.");
                        return;
                    }

                    barcodesToGenerate = manualTB_.Text
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .ToList();

                    // Validate manually entered barcodes based on type
                    if (!ValidateManualBarcodes(barcodesToGenerate, barcodeType))
                    {
                        return;
                    }
                }

                if (!barcodesToGenerate.Any())
                {
                    MessageBox.Show("No valid barcodes to generate.");
                    return;
                }

                // Generate barcode images
                List<BarcodeData> barcodes = GenerateBarcodeImages(barcodesToGenerate, barcodeType);

                // Save to history only if in HomeGrid mode
                if (HomeGrid.Visibility == Visibility.Visible)
                {
                    SaveToHistory(barcodes, "Generated");
                }

                // Show result window with template
                ShowResultWindow(barcodes, templateName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating barcodes: {ex.Message}");
            }
        }

        private bool ValidateManualBarcodes(List<string> barcodes, string barcodeType)
        {
            foreach (var code in barcodes)
            {
                if (!IsValidBarcodeFormat(code, barcodeType))
                {
                    MessageBox.Show($"Invalid {barcodeType} format: {code}");
                    return false;
                }
            }
            return true;
        }

        private bool IsValidBarcodeFormat(string code, string barcodeType)
        {
            switch (barcodeType)
            {
                case "EAN_13":
                    return code.Length == 13 && code.All(char.IsDigit);
                case "EAN_8":
                    return code.Length == 8 && code.All(char.IsDigit);
                case "CODE_128":
                    return code.Length >= 1 && code.All(c => c >= 32 && c <= 126);
                case "CODE_39":
                    return code.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%".Contains(c));
                case "QR_CODE":
                    return !string.IsNullOrEmpty(code); // QR can contain any text
                default:
                    return false;
            }
        }

        private void SaveGeneratedBarcodes(List<string> barcodes)
        {
            // Initialize SavedBarcodes if null
            if (Properties.Settings.Default.SavedBarcodes == null)
            {
                Properties.Settings.Default.SavedBarcodes = new StringCollection();
            }

            // Add new barcodes to saved collection
            foreach (var barcode in barcodes)
            {
                Properties.Settings.Default.SavedBarcodes.Add(barcode);
            }

            // Save changes
            Properties.Settings.Default.Save();
        }

        private List<string> GenerateUniqueBarcodes(string barcodeType, int count)
        {
            var existingBarcodes = new HashSet<string>();

            // Load existing barcodes from settings
            if (Properties.Settings.Default.SavedBarcodes != null)
            {
                foreach (string barcode in Properties.Settings.Default.SavedBarcodes)
                {
                    existingBarcodes.Add(barcode);
                }
            }

            var random = new Random();
            var result = new List<string>();
            int maxAttempts = count * 10; // Prevent infinite loop
            int attempts = 0;

            while (result.Count < count && attempts < maxAttempts)
            {
                string barcode = GenerateBarcode(barcodeType, random);
                if (!existingBarcodes.Contains(barcode))
                {
                    result.Add(barcode);
                    existingBarcodes.Add(barcode);
                }
                attempts++;
            }

            if (result.Count < count)
            {
                MessageBox.Show($"Could only generate {result.Count} unique barcodes out of {count} requested. Please try again with a different type or fewer barcodes.");
            }

            return result;
        }


        private string GenerateBarcode(string barcodeType, Random random)
        {
            switch (barcodeType)
            {
                case "EAN_13":
                    return GenerateEAN13(random);
                case "EAN_8":
                    return GenerateEAN8(random);
                case "CODE_128":
                    return GenerateCODE128(random);
                case "CODE_39":
                    return GenerateCODE39(random);
                case "QR_CODE":
                    return GenerateQRCode(random);
                default:
                    throw new ArgumentException("Nieobsługiwany typ kodu kreskowego");
            }
        }

        private string GenerateEAN13(Random random)
        {
            var digits = new char[12];
            for (int i = 0; i < 12; i++)
                digits[i] = (char)('0' + random.Next(10));

            int checksum = 0;
            for (int i = 0; i < 12; i++)
                checksum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);

            checksum = (10 - (checksum % 10)) % 10;
            return new string(digits) + checksum.ToString();
        }

        private string GenerateEAN8(Random random)
        {
            var digits = new char[7];
            for (int i = 0; i < 7; i++)
                digits[i] = (char)('0' + random.Next(10));

            int checksum = 0;
            for (int i = 0; i < 7; i++)
                checksum += (digits[i] - '0') * (i % 2 == 0 ? 3 : 1);

            checksum = (10 - (checksum % 10)) % 10;
            return new string(digits) + checksum.ToString();
        }

        private string GenerateCODE128(Random random)
        {
            var result = new StringBuilder();
            int length = random.Next(8, 13);
            for (int i = 0; i < length; i++)
                result.Append(random.Next(10));
            return result.ToString();
        }

        private string GenerateCODE39(Random random)
        {
            const string VALID_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
            var result = new StringBuilder();
            int length = random.Next(8, 13);
            for (int i = 0; i < length; i++)
                result.Append(VALID_CHARS[random.Next(VALID_CHARS.Length)]);
            return result.ToString();
        }

        private string GenerateQRCode(Random random)
        {
            // Instead of using hardcoded timestamp and login, generate a unique identifier
            var result = new StringBuilder();
            int length = random.Next(8, 13); // Similar length as other barcode types

            // Generate random alphanumeric string
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        private List<BarcodeData> GenerateBarcodeImages(List<string> barcodeValues, string barcodeType)
        {
            var writer = new BarcodeWriter<Bitmap>
            {
                Format = (BarcodeFormat)Enum.Parse(typeof(BarcodeFormat), barcodeType),
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = barcodeType == "QR_CODE" ? 300 : 300,
                    Height = barcodeType == "QR_CODE" ? 300 : 150,
                    Margin = barcodeType == "QR_CODE" ? 1 : 0
                },
                Renderer = new BitmapRenderer() // Add this line to set the renderer
            };

            return barcodeValues.Select(value => new BarcodeData
            {
                Value = value,
                Image = ConvertToBitmapImage(writer.Write(value))
            }).ToList();
        }


        private BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }


        private void ShowResultWindow(List<BarcodeData> barcodes, string templateName)
        {
            // Get template settings
            int columns = int.Parse(Properties.Settings.Default.NumberOfColumns);
            double labelWidth = double.Parse(Properties.Settings.Default.LabelWidth);
            double labelHeight = double.Parse(Properties.Settings.Default.LabelHeight);

            var resultWindow = new ResultWindow();
            string barcodeType = barcodeTypeCB.SelectedItem?.ToString() ?? "UNKNOWN";
            resultWindow.DisplayBarcodes(barcodes, columns, labelWidth, labelHeight, barcodeType);
            resultWindow.Show();
        }
        

        private void SaveToHistory(List<BarcodeData> barcodes, string source)
        {
            if (Properties.Settings.Default.SavedBarcodes == null)
                Properties.Settings.Default.SavedBarcodes = new StringCollection();

            if (Properties.Settings.Default.BarcodeHistory == null)
                Properties.Settings.Default.BarcodeHistory = new StringCollection();

            // Add barcodes to saved collection
            foreach (var barcode in barcodes)
            {
                Properties.Settings.Default.SavedBarcodes.Add(barcode.Value);
            }

            // Add history entry
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string historyEntry = $"{timestamp} - {source} ({barcodes.Count} kodów)";
            Properties.Settings.Default.BarcodeHistory.Add(historyEntry);

            Properties.Settings.Default.Save();
            UpdateHistoryListBox();
        }


        private void barcodeTypeCB_Loaded(object sender, RoutedEventArgs e)
        {
            string lastSelected = Properties.Settings.Default.LastSelectedItem;

            // If the setting exists, find and select the item
            if (!string.IsNullOrEmpty(lastSelected))
            {
                foreach (ComboBoxItem item in barcodeTypeCB.Items)
                {
                    if (item.Content.ToString() == lastSelected)
                    {
                        barcodeTypeCB.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                // If no saved selection, set the first item as default
                barcodeTypeCB.SelectedIndex = 0;
            }
        }


        private void ShowGrid(Grid activeGrid)
        {
            //HomeGrid.Visibility = Visibility.Collapsed;
            //ManualGrid.Visibility = Visibility.Collapsed;
            SettingsGrid.Visibility = Visibility.Collapsed;
            HistoryGrid.Visibility = Visibility.Collapsed;

            activeGrid.Visibility = Visibility.Visible;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ManualGrid.Visibility = Visibility.Collapsed;
            ShowGrid(HomeGrid);
            ShowGrid(Generate_Grid_);
            ShowGrid(Generate_Settings_Grid);
            GenNumSP.Visibility = Visibility.Visible;
        }

        private void ManualButton_Click(object sender, RoutedEventArgs e)
        {
            HomeGrid.Visibility = Visibility.Collapsed;
            ShowGrid(ManualGrid);
            ShowGrid(Generate_Grid_);
            ShowGrid(Generate_Settings_Grid);
            GenNumSP.Visibility = Visibility.Hidden;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            HomeGrid.Visibility = Visibility.Collapsed;
            ManualGrid.Visibility = Visibility.Collapsed;
            Generate_Grid_.Visibility = Visibility.Collapsed;
            Generate_Settings_Grid.Visibility = Visibility.Collapsed;
            ShowGrid(SettingsGrid);

        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HomeGrid.Visibility = Visibility.Collapsed;
            ManualGrid.Visibility = Visibility.Collapsed;
            Generate_Grid_.Visibility = Visibility.Collapsed;
            Generate_Settings_Grid.Visibility = Visibility.Collapsed;
            ShowGrid(HistoryGrid);

        }

        // Close the application
        private void MainExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private List<string> ReadBarcodesFromFile(string filePath)
        {
            List<string> barcodes = new List<string>();
            string extension = Path.GetExtension(filePath).ToLower();

            try
            {
                switch (extension)
                {
                    case ".txt":
                    case ".csv":
                        barcodes.AddRange(File.ReadAllLines(filePath)
                            .Where(line => !string.IsNullOrWhiteSpace(line)));
                        break;

                    // Add more file type handling as needed
                    default:
                        MessageBox.Show($"Format pliku {extension} nie jest obecnie obsługiwany.");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas odczytu pliku: {ex.Message}");
            }

            return barcodes;
        }

        private void UpdateHistoryListBox()
        {
            if (Properties.Settings.Default.BarcodeHistory != null)
            {
                history_LB.ItemsSource = null; // Clear current items
                history_LB.ItemsSource = Properties.Settings.Default.BarcodeHistory.Cast<string>().ToList();
            }
        }

        private void history_LB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (history_LB.SelectedItem != null)
            {
                string selectedEntry = history_LB.SelectedItem?.ToString() ?? string.Empty;
                ShowBarcodesForEntry(selectedEntry);
            }
        }

        private void ShowBarcodesForEntry(string historyEntry)
        {
            try
            {
                // Extract timestamp from history entry
                string timestamp = historyEntry.Split('-')[0].Trim();

                // Create a new window to display the barcodes
                var window = new Window
                {
                    Title = $"Kody kreskowe z {timestamp}",
                    Width = 400,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.CanResize
                };

                // Create a ListBox to display the barcodes
                var listBox = new ListBox
                {
                    Margin = new Thickness(10)
                };

                // Get barcodes for this timestamp from settings
                if (Properties.Settings.Default.SavedBarcodes != null)
                {
                    var barcodes = Properties.Settings.Default.SavedBarcodes
                        .Cast<string>()
                        .ToList();

                    listBox.ItemsSource = barcodes;
                }

                window.Content = listBox;
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wyświetlania kodów: {ex.Message}");
            }
        }

        // Handle file loading
        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Wszystkie obsługiwane pliki|*.xlsx;*.xls;*.txt;*.doc;*.docx;*.odt;*.csv|" +
                        "Pliki Excel (*.xlsx, *.xls)|*.xlsx;*.xls|" +
                        "Pliki tekstowe (*.txt, *.csv)|*.txt;*.csv|" +
                        "Dokumenty Word (*.doc;*.docx;*.odt)|*.doc;*.docx;*.odt|" +
                        "Wszystkie pliki (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;
                    List<string> barcodes = ReadBarcodesFromFile(filePath);

                    if (barcodes.Count > 0)
                    {
                        // Initialize SavedBarcodes if null
                        if (Properties.Settings.Default.SavedBarcodes == null)
                        {
                            Properties.Settings.Default.SavedBarcodes = new StringCollection();
                        }

                        // Add barcodes to settings
                        Properties.Settings.Default.SavedBarcodes.AddRange(barcodes.ToArray());

                        // Initialize BarcodeHistory if null
                        if (Properties.Settings.Default.BarcodeHistory == null)
                        {
                            Properties.Settings.Default.BarcodeHistory = new StringCollection();
                        }

                        // Add history entry
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string historyEntry = $"{timestamp} - {Path.GetFileName(filePath)} ({barcodes.Count} kodów)";
                        Properties.Settings.Default.BarcodeHistory.Add(historyEntry);

                        // Save settings
                        Properties.Settings.Default.Save();

                        // Update history ListBox
                        UpdateHistoryListBox();

                        MessageBox.Show($"Pomyślnie załadowano {barcodes.Count} kodów kreskowych z pliku: {Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        MessageBox.Show("Nie znaleziono kodów kreskowych w pliku.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wystąpił błąd podczas wczytywania pliku: {ex.Message}");
                }
            }
        }
    }
    public class BarcodeData
    {
        public string Value { get; set; } = string.Empty;
        public BitmapImage Image { get; set; } = new BitmapImage();
    }
}