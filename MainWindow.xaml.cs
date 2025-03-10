using DrawingRectangle = System.Drawing.Rectangle;
using WPFRectangle = System.Windows.Shapes.Rectangle;
using IOPath = System.IO.Path;
using ShapesPath = System.Windows.Shapes.Path;
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
using Microsoft.Win32;
using ZXing;
using System.IO;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;
using System.Xml;
using System.Xml.Linq;
using QuestPDF.Infrastructure;
using System.Runtime.ConstrainedExecution;
using System.Windows.Shapes; // For Rectangle
using System.Windows.Controls; // For Canvas



namespace BarcodeGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //RESET CODE
            // Reset settings first
           /* Properties.Settings.Default.Reset();
            Properties.Settings.Default.TemplateNames = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.SavedBarcodes = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.BarcodeHistory = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.TemplateSettingsStorage = "<Templates></Templates>";
            Properties.Settings.Default.Save();*/

            


            InitializeComponent();
            InitializePaperSizes();
            LoadTemplateNames();
            InitializeBarcodeTypes();
            CollapseAllGrids();
            ResetSettingsGrid();
            ShowGrid(Generate_Grid_);
            ShowGrid(Generate_Settings_Grid);
            ShowGrid(PreviewSPGrid);
            ShowGrid(GenNumGrid);
            ShowGrid(GenerateBtnGrid);
            history_LB.SelectionChanged += history_LB_SelectionChanged;
            UpdateHistoryListBox();
        }

        // Track the different operation modes
        private enum TemplateMode { None, Adding, Editing, Deleting }
        private TemplateMode currentTemplateMode = TemplateMode.None;
        private string originalTemplateName = string.Empty;
        private bool _formLoaded = false;


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize any components that need the visual tree to be loaded

            // Set the form loaded flag to true
            _formLoaded = true;

            // If you need to initialize any preview content after load
            if (PreviewG != null && PreviewG.Visibility == Visibility.Visible)
            {
                try
                {
                    // Initial preview rendering if needed
                    UpdateTemplatePreview();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in Window_Loaded: {ex.Message}");
                }
            }
        }

        private void UpdateTemplatePreview()
        {
            if (!_formLoaded)
                return;

            if (sizeXTb == null || sizeYTb == null || no_labels_Tb == null ||
                no_column_Tb == null || label_size_X_Tb == null || label_size_Y_Tb == null ||
                mariginO_X_Tb == null || mariginO_Y_Tb == null ||
                mariginI_X_Tb == null || mariginI_Y_Tb == null || PreviewG == null)
            {
                return;
            }

            // Try to parse the values
            if (!string.IsNullOrEmpty(sizeXTb.Text) &&
                !string.IsNullOrEmpty(sizeYTb.Text) &&
                !string.IsNullOrEmpty(no_labels_Tb.Text) &&
                !string.IsNullOrEmpty(no_column_Tb.Text) &&
                !string.IsNullOrEmpty(label_size_X_Tb.Text) &&
                !string.IsNullOrEmpty(label_size_Y_Tb.Text) &&
                !string.IsNullOrEmpty(mariginO_X_Tb.Text) &&
                !string.IsNullOrEmpty(mariginO_Y_Tb.Text) &&
                !string.IsNullOrEmpty(mariginI_X_Tb.Text) &&
                !string.IsNullOrEmpty(mariginI_Y_Tb.Text) &&
                double.TryParse(sizeXTb.Text, out double sheetWidth) &&
                double.TryParse(sizeYTb.Text, out double sheetHeight) &&
                int.TryParse(no_labels_Tb.Text, out int numberOfLabels) &&
                int.TryParse(no_column_Tb.Text, out int numberOfColumns) &&
                double.TryParse(label_size_X_Tb.Text, out double labelWidth) &&
                double.TryParse(label_size_Y_Tb.Text, out double labelHeight) &&
                double.TryParse(mariginO_X_Tb.Text, out double marginOX) &&
                double.TryParse(mariginO_Y_Tb.Text, out double marginOY) &&
                double.TryParse(mariginI_X_Tb.Text, out double marginIX) &&
                double.TryParse(mariginI_Y_Tb.Text, out double marginIY))
            {
                RenderTemplatePreviewInGrid(
                    sheetWidth, sheetHeight, numberOfLabels, numberOfColumns,
                    labelWidth, labelHeight, marginOX, marginOY, marginIX, marginIY
                );
            }
        }
        //private bool isEditingTemplate = false;
        //private string originalTemplateName = string.Empty;

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
            barcodeTypeCB.SelectedIndex = 0; // Default to EAN_13

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
                    MessageBox.Show("Marginesy są zbyt duże w stosunku do podanego rozmiaru arkusza i liczby etykiet/kolumn.");
                }
            }
        }
        private void SetInitialValues()
        {
            // Remove event handlers temporarily
            sizeXTb.TextChanged -= TextBox_TextChanged;
            sizeYTb.TextChanged -= TextBox_TextChanged;
            // Remove handlers for other TextBoxes...

            // Set initial values
            sizeXTb.Text = "210";
            sizeYTb.Text = "297";
            // Set other values...

            // Add handlers back
            sizeXTb.TextChanged += TextBox_TextChanged;
            sizeYTb.TextChanged += TextBox_TextChanged;
            // Add handlers for other TextBoxes...
        }

        // Also, update the TextBox_TextChanged method to update the preview in real-time
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_formLoaded)
                return;

            try
            {
                // First check if the control is loaded and visible
                if (!IsLoaded || PreviewG == null)
                    return;

                CalculateLabelSize();
                UpdateTemplatePreview();

                // Check if all TextBoxes are properly initialized before trying to access them
                if (sizeXTb == null || sizeYTb == null || no_labels_Tb == null ||
                    no_column_Tb == null || label_size_X_Tb == null || label_size_Y_Tb == null ||
                    mariginO_X_Tb == null || mariginO_Y_Tb == null ||
                    mariginI_X_Tb == null || mariginI_Y_Tb == null || PreviewG == null)
                {
                    return; // Exit if any control is null
                }

                // Then try to parse the values, only if all TextBoxes have text
                if (!string.IsNullOrEmpty(sizeXTb.Text) &&
                    !string.IsNullOrEmpty(sizeYTb.Text) &&
                    !string.IsNullOrEmpty(no_labels_Tb.Text) &&
                    !string.IsNullOrEmpty(no_column_Tb.Text) &&
                    !string.IsNullOrEmpty(label_size_X_Tb.Text) &&
                    !string.IsNullOrEmpty(label_size_Y_Tb.Text) &&
                    !string.IsNullOrEmpty(mariginO_X_Tb.Text) &&
                    !string.IsNullOrEmpty(mariginO_Y_Tb.Text) &&
                    !string.IsNullOrEmpty(mariginI_X_Tb.Text) &&
                    !string.IsNullOrEmpty(mariginI_Y_Tb.Text) &&
                    double.TryParse(sizeXTb.Text, out double sheetWidth) &&
                    double.TryParse(sizeYTb.Text, out double sheetHeight) &&
                    int.TryParse(no_labels_Tb.Text, out int numberOfLabels) &&
                    int.TryParse(no_column_Tb.Text, out int numberOfColumns) &&
                    double.TryParse(label_size_X_Tb.Text, out double labelWidth) &&
                    double.TryParse(label_size_Y_Tb.Text, out double labelHeight) &&
                    double.TryParse(mariginO_X_Tb.Text, out double marginOX) &&
                    double.TryParse(mariginO_Y_Tb.Text, out double marginOY) &&
                    double.TryParse(mariginI_X_Tb.Text, out double marginIX) &&
                    double.TryParse(mariginI_Y_Tb.Text, out double marginIY))
                {
                    RenderTemplatePreviewInGrid(
                        sheetWidth, sheetHeight, numberOfLabels, numberOfColumns,
                        labelWidth, labelHeight, marginOX, marginOY, marginIX, marginIY
                    );
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't show a message box to avoid annoying the user
                Console.WriteLine($"Błąd podczas aktualizacji podglądu: {ex.Message}");
                // You could also log to a file or debug output
                System.Diagnostics.Debug.WriteLine($"Błąd podczas aktualizacji podglądu: {ex.Message}");
            }
        }

        // Ensure the preview is updated when the window is resized
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Only update if the PreviewG is visible
            if (PreviewG.Visibility == Visibility.Visible && PreviewG.Children.Count > 0)
            {
                try
                {
                    // Get current values from UI or settings
                    double sheetWidth, sheetHeight;
                    int numberOfLabels, numberOfColumns;
                    double labelWidth, labelHeight;
                    double marginOX, marginOY, marginIX, marginIY;

                    // Try to get values from UI first
                    if (double.TryParse(sizeXTb.Text, out sheetWidth) &&
                        double.TryParse(sizeYTb.Text, out sheetHeight) &&
                        int.TryParse(no_labels_Tb.Text, out numberOfLabels) &&
                        int.TryParse(no_column_Tb.Text, out numberOfColumns) &&
                        double.TryParse(label_size_X_Tb.Text, out labelWidth) &&
                        double.TryParse(label_size_Y_Tb.Text, out labelHeight) &&
                        double.TryParse(mariginO_X_Tb.Text, out marginOX) &&
                        double.TryParse(mariginO_Y_Tb.Text, out marginOY) &&
                        double.TryParse(mariginI_X_Tb.Text, out marginIX) &&
                        double.TryParse(mariginI_Y_Tb.Text, out marginIY))
                    {
                        RenderTemplatePreviewInGrid(
                            sheetWidth, sheetHeight, numberOfLabels, numberOfColumns,
                            labelWidth, labelHeight, marginOX, marginOY, marginIX, marginIY
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Silently handle errors in preview - don't show a message box
                    Console.WriteLine($"Błąd podczas aktualizacji podglądu przy zmianie rozmiaru: {ex.Message}");
                }
            }
        }

        private void SaveTemplateSettings(string templateName)
        {
            try
            {
                // Get current values from UI
                var settings = new Dictionary<string, string>
        {
            { "PaperSize", formatCb.SelectedItem?.ToString() ?? "" },
            { "SheetWidth", sizeXTb.Text },
            { "SheetHeight", sizeYTb.Text },
            { "MarginOX", mariginO_X_Tb.Text },
            { "MarginOY", mariginO_Y_Tb.Text },
            { "MarginIX", mariginI_X_Tb.Text },
            { "MarginIY", mariginI_Y_Tb.Text },
            { "LabelWidth", label_size_X_Tb.Text },
            { "LabelHeight", label_size_Y_Tb.Text },
            { "NumberOfLabels", no_labels_Tb.Text },
            { "NumberOfColumns", no_column_Tb.Text }
        };

                // Load existing XML
                XDocument doc;
                if (string.IsNullOrEmpty(Properties.Settings.Default.TemplateSettingsStorage))
                {
                    doc = new XDocument(new XElement("Templates"));
                }
                else
                {
                    doc = XDocument.Parse(Properties.Settings.Default.TemplateSettingsStorage);
                }

                // Remove existing template if any
                var existingTemplate = doc.Root.Elements("Template")
                    .FirstOrDefault(e => e.Attribute("Name")?.Value == templateName);
                if (existingTemplate != null)
                {
                    existingTemplate.Remove();
                }

                // Create new template element
                var templateElement = new XElement("Template",
                    new XAttribute("Name", templateName));

                // Add settings to template
                foreach (var setting in settings)
                {
                    templateElement.Add(new XElement(setting.Key, setting.Value));
                }

                // Add template to document
                doc.Root.Add(templateElement);

                // Save back to settings
                Properties.Settings.Default.TemplateSettingsStorage = doc.ToString();
                Properties.Settings.Default.Save();

                // Debug output
                Console.WriteLine($"Saved template {templateName} with {settings.Count} settings");
                Console.WriteLine($"NumberOfColumns: {settings["NumberOfColumns"]}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania ustawień szablonu: {ex.Message}");
            }
        }

        private void LoadTemplateSettings(string templateName)
        {
            try
            {
                // Check if we have settings storage
                if (string.IsNullOrEmpty(Properties.Settings.Default.TemplateSettingsStorage))
                {
                    MessageBox.Show($"Nie znaleziono ustawień dla szablonu: {templateName}");
                    return;
                }

                // Parse XML
                XDocument doc = XDocument.Parse(Properties.Settings.Default.TemplateSettingsStorage);

                // Find template
                var template = doc.Root.Elements("Template")
                    .FirstOrDefault(e => e.Attribute("Name")?.Value == templateName);

                if (template == null)
                {
                    MessageBox.Show($"Nie znaleziono szablonu: {templateName}");
                    return;
                }

                // Debug - Show what we found
                Console.WriteLine($"Znaleziono szablon {templateName}");

                // Update application settings directly from XML elements
                // This ensures ResultWindow will use the correct values 
                Properties.Settings.Default.PaperSize = GetElementValue(template, "PaperSize");
                Properties.Settings.Default.SheetWidth = GetElementValue(template, "SheetWidth");
                Properties.Settings.Default.SheetHeight = GetElementValue(template, "SheetHeight");
                Properties.Settings.Default.MarginOX = GetElementValue(template, "MarginOX");
                Properties.Settings.Default.MarginOY = GetElementValue(template, "MarginOY");
                Properties.Settings.Default.MarginIX = GetElementValue(template, "MarginIX");
                Properties.Settings.Default.MarginIY = GetElementValue(template, "MarginIY");
                Properties.Settings.Default.LabelWidth = GetElementValue(template, "LabelWidth");
                Properties.Settings.Default.LabelHeight = GetElementValue(template, "LabelHeight");
                Properties.Settings.Default.NumberOfLabels = GetElementValue(template, "NumberOfLabels");
                Properties.Settings.Default.NumberOfColumns = GetElementValue(template, "NumberOfColumns");

                // Save settings
                Properties.Settings.Default.Save();

                // Debug - Log the loaded values
                Console.WriteLine($"Załadowano NumberOfColumns: {Properties.Settings.Default.NumberOfColumns}");

                // Update UI with loaded settings if needed
                UpdateUIFromSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania ustawień szablonu: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private string GetElementValue(XElement parent, string elementName, string defaultValue = "")
        {
            var element = parent.Element(elementName);
            return element != null ? element.Value : defaultValue;
        }

        private void UpdateUIFromSettings()
        {
            // This updates the UI based on loaded settings
            try
            {
                // Find the paper size in the ComboBox
                string paperSize = Properties.Settings.Default.PaperSize;
                if (!string.IsNullOrEmpty(paperSize))
                {
                    for (int i = 0; i < formatCb.Items.Count; i++)
                    {
                        if (formatCb.Items[i].ToString() == paperSize)
                        {
                            formatCb.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Update text boxes
                sizeXTb.Text = Properties.Settings.Default.SheetWidth;
                sizeYTb.Text = Properties.Settings.Default.SheetHeight;
                mariginO_X_Tb.Text = Properties.Settings.Default.MarginOX;
                mariginO_Y_Tb.Text = Properties.Settings.Default.MarginOY;
                mariginI_X_Tb.Text = Properties.Settings.Default.MarginIX;
                mariginI_Y_Tb.Text = Properties.Settings.Default.MarginIY;
                label_size_X_Tb.Text = Properties.Settings.Default.LabelWidth;
                label_size_Y_Tb.Text = Properties.Settings.Default.LabelHeight;
                no_labels_Tb.Text = Properties.Settings.Default.NumberOfLabels;
                no_column_Tb.Text = Properties.Settings.Default.NumberOfColumns;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas aktualizacji IU: {ex.Message}");
            }
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



        /*private void previewtemplBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateTemplateInput())
                return;

            try
            {
                // Instead of showing a new window, render the preview in the PreviewG grid
                RenderTemplatePreviewInGrid(
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating preview: {ex.Message}");
            }
        }*/

        // Add a method to render the template preview in the PreviewG grid
        private void RenderTemplatePreviewInGrid(double sheetWidth, double sheetHeight, int numberOfLabels,
    int numberOfColumns, double labelWidth, double labelHeight,
    double marginOX, double marginOY, double marginIX, double marginIY)
        {
            if (PreviewG == null)
                return;

            // Clear the existing content in PreviewG
            PreviewG.Children.Clear();

            // Calculate the preview size based on the PreviewG dimensions
            double previewG_Width = PreviewG.ActualWidth;
            double previewG_Height = PreviewG.ActualHeight;

            // Calculate scaling factor to maintain aspect ratio
            double scaleX = previewG_Width / sheetWidth;
            double scaleY = previewG_Height / sheetHeight;
            double scale = Math.Min(scaleX, scaleY) * 0.9; // 90% of the available space to add some margin

            // Create a canvas for the sheet preview
            Canvas previewCanvas = new Canvas();
            previewCanvas.Width = sheetWidth * scale;
            previewCanvas.Height = sheetHeight * scale;

            // Center the canvas in the PreviewG grid
            previewCanvas.Margin = new Thickness(
                (previewG_Width - (sheetWidth * scale)) / 2,
                (previewG_Height - (sheetHeight * scale)) / 2,
                0, 0);

            // Draw the sheet boundary (paper)
            WPFRectangle paperRect = new WPFRectangle();
            paperRect.Width = sheetWidth * scale;
            paperRect.Height = sheetHeight * scale;
            paperRect.Stroke = System.Windows.Media.Brushes.Black;
            paperRect.Fill = System.Windows.Media.Brushes.White;
            Canvas.SetLeft(paperRect, 0);
            Canvas.SetTop(paperRect, 0);
            previewCanvas.Children.Add(paperRect);

            // Calculate number of rows based on labels and columns
            int rows = (int)Math.Ceiling((double)numberOfLabels / numberOfColumns);

            // Draw each label
            int labelCounter = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < numberOfColumns; col++)
                {
                    labelCounter++;

                    if (labelCounter > numberOfLabels)
                        break;

                    // Calculate position for this label with scaling
                    double left = (marginOX + col * (labelWidth + marginIX)) * scale;
                    double top = (marginOY + row * (labelHeight + marginIY)) * scale;

                    // Draw the label
                    WPFRectangle labelRect = new WPFRectangle();
                    labelRect.Width = labelWidth * scale;
                    labelRect.Height = labelHeight * scale;
                    labelRect.Stroke = System.Windows.Media.Brushes.DarkGray;
                    labelRect.Fill = System.Windows.Media.Brushes.LightGray;
                    labelRect.StrokeThickness = 1;
                    Canvas.SetLeft(labelRect, left);
                    Canvas.SetTop(labelRect, top);
                    previewCanvas.Children.Add(labelRect);

                    // Add label number for reference
                    TextBlock labelText = new TextBlock();
                    labelText.Text = labelCounter.ToString();
                    labelText.Foreground = System.Windows.Media.Brushes.DarkGray;
                    Canvas.SetLeft(labelText, left + (labelWidth * scale / 2) - 5);
                    Canvas.SetTop(labelText, top + (labelHeight * scale / 2) - 10);
                    previewCanvas.Children.Add(labelText);
                }
            }

            // Add the canvas to the PreviewG grid
            PreviewG.Children.Add(previewCanvas);
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
                    MessageBox.Show("Proszę wybrać typ kodu kreskowego.");
                    return;
                }
                string barcodeType = barcodeTypeCB.SelectedItem.ToString();

                // Validate template selection
                if (labeltmplCB.SelectedItem == null)
                {
                    MessageBox.Show("Proszę wybrać szablon etykiety.");
                    return;
                }
                string templateName = labeltmplCB.SelectedItem.ToString();

                List<string> barcodesToGenerate = new List<string>();

                // Handle HomeGrid scenario - Generate unique barcodes
                if (ManualGrid.Visibility == Visibility.Collapsed)
                {
                    if (!int.TryParse(NumberTB.Text, out int numberOfBarcodes) || numberOfBarcodes <= 0)
                    {
                        MessageBox.Show("Proszę wprowadzić prawidłową liczbę kodów kreskowych do wygenerowania.");
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
                        MessageBox.Show("Proszę wpisać co najmniej jeden kod kreskowy.");
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
                    MessageBox.Show("Brak prawidłowych kodów kreskowych do wygenerowania.");
                    return;
                }

                // Generate barcode images
                List<BarcodeData> barcodes = GenerateBarcodeImages(barcodesToGenerate, barcodeType);

                // Save to history only if in HomeGrid mode
                if (ManualGrid.Visibility == Visibility.Collapsed)
                {
                    SaveToHistory(barcodes, "Generated");
                }

                // Show result window with template
                ShowResultWindow(barcodes, templateName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas generowania kodów kreskowych: {ex.Message}");
            }
        }

        private bool ValidateManualBarcodes(List<string> barcodes, string barcodeType)
        {
            foreach (var code in barcodes)
            {
                if (!IsValidBarcodeFormat(code, barcodeType))
                {
                    MessageBox.Show($"Nieprawidłowy format {barcodeType}: {code}");
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
                MessageBox.Show($"Udało się wygenerować tylko {result.Count} unikalnych kodów kreskowych z {count} żądanych. Spróbuj ponownie z innym typem lub mniejszą liczbą kodów kreskowych.");
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



        // Updated addtemplBtn_Click to handle all three modes
        private void addtemplBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (currentTemplateMode)
            {
                case TemplateMode.Adding:
                    HandleAddTemplate();
                    break;

                case TemplateMode.Editing:
                    HandleEditTemplate();
                    break;

                case TemplateMode.Deleting:
                    HandleDeleteTemplate();
                    break;

                default:
                    // Default behavior (should not happen)
                    MessageBox.Show("Nieoczekiwany błąd w trybie szablonu.");
                    break;
            }
        }

        // Handle adding a new template
        private void HandleAddTemplate()
        {
            var templateName = label_name_Tb.Text;

            if (string.IsNullOrWhiteSpace(templateName))
            {
                MessageBox.Show("Proszę wpisać nazwę szablonu.");
                return;
            }

            // Check if name already exists
            if (Properties.Settings.Default.TemplateNames != null &&
                Properties.Settings.Default.TemplateNames.Cast<string>().Any(name => name == templateName))
            {
                MessageBox.Show("Etykieta z taką nazwą już istnieje.");
                return;
            }

            // Add the template name to the list of template names
            if (Properties.Settings.Default.TemplateNames == null)
            {
                Properties.Settings.Default.TemplateNames = new System.Collections.Specialized.StringCollection();
            }

            Properties.Settings.Default.TemplateNames.Add(templateName);

            // Save the template settings
            SaveTemplateSettings(templateName);

            // Update LastSelectedTemplate
            Properties.Settings.Default.LastSelectedTemplate = templateName;
            Properties.Settings.Default.Save();

            // Reload the template names into the ComboBox
            LoadTemplateNames();

            MessageBox.Show("Szablon został zapisany pomyślnie!");
        }

        // Handle editing an existing template
        private void HandleEditTemplate()
        {
            var templateName = label_name_Tb.Text;

            if (string.IsNullOrWhiteSpace(templateName))
            {
                MessageBox.Show("Proszę wpisać nazwę szablonu.");
                return;
            }

            // If the name was changed, we need to update the template names collection
            if (templateName != originalTemplateName)
            {
                // Check if the new name already exists (but isn't the original name)
                if (Properties.Settings.Default.TemplateNames != null &&
                    Properties.Settings.Default.TemplateNames.Cast<string>().Any(name => name == templateName))
                {
                    MessageBox.Show("Etykieta z taką nazwą już istnieje.");
                    return;
                }

                // Remove the old name and add the new one
                if (Properties.Settings.Default.TemplateNames != null)
                {
                    var templates = Properties.Settings.Default.TemplateNames.Cast<string>().ToList();
                    int index = templates.IndexOf(originalTemplateName);
                    if (index >= 0)
                    {
                        Properties.Settings.Default.TemplateNames.RemoveAt(index);
                        Properties.Settings.Default.TemplateNames.Add(templateName);
                    }
                }
            }

            // Save the template settings
            SaveTemplateSettings(templateName);

            // Update LastSelectedTemplate
            Properties.Settings.Default.LastSelectedTemplate = templateName;
            Properties.Settings.Default.Save();

            // Reload the template names into the ComboBox
            LoadTemplateNames();

            MessageBox.Show("Szablon został zaktualizowany pomyślnie!");
        }

        // Handle deleting a template
        private void HandleDeleteTemplate()
        {
            // Confirm deletion
            MessageBoxResult result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć szablon '{originalTemplateName}'?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Remove the template from the names collection
                if (Properties.Settings.Default.TemplateNames != null)
                {
                    var templates = Properties.Settings.Default.TemplateNames.Cast<string>().ToList();
                    int index = templates.IndexOf(originalTemplateName);
                    if (index >= 0)
                    {
                        Properties.Settings.Default.TemplateNames.RemoveAt(index);

                        // Remove template settings from XML storage
                        RemoveTemplateSettings(originalTemplateName);

                        // Save changes
                        Properties.Settings.Default.Save();

                        // Update ComboBox and interface
                        PopulateTemplateSelector();

                        MessageBox.Show("Szablon został usunięty pomyślnie.");

                        // If this was the last selected template, update that setting
                        if (Properties.Settings.Default.LastSelectedTemplate == originalTemplateName)
                        {
                            if (Properties.Settings.Default.TemplateNames != null &&
                                Properties.Settings.Default.TemplateNames.Count > 0)
                            {
                                Properties.Settings.Default.LastSelectedTemplate =
                                    Properties.Settings.Default.TemplateNames[0];
                            }
                            else
                            {
                                Properties.Settings.Default.LastSelectedTemplate = string.Empty;
                            }
                            Properties.Settings.Default.Save();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nie można odnaleźć szablonu do usunięcia.");
                    }
                }
            }
        }

        // Method to remove template settings from XML storage
        private void RemoveTemplateSettings(string templateName)
        {
            try
            {
                // Check if we have settings storage
                if (!string.IsNullOrEmpty(Properties.Settings.Default.TemplateSettingsStorage))
                {
                    // Parse XML
                    XDocument doc = XDocument.Parse(Properties.Settings.Default.TemplateSettingsStorage);

                    // Find and remove template
                    var template = doc.Root.Elements("Template")
                        .FirstOrDefault(e => e.Attribute("Name")?.Value == templateName);

                    if (template != null)
                    {
                        template.Remove();

                        // Save updated XML back to settings
                        Properties.Settings.Default.TemplateSettingsStorage = doc.ToString();
                        Properties.Settings.Default.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania ustawień szablonu: {ex.Message}");
            }
        }

        // Modify the LabelTmplCB_SelectionChanged method to update the preview
        private void LabelTmplCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (labeltmplCB.SelectedItem != null)
            {
                string selectedTemplate = labeltmplCB.SelectedItem.ToString();

                // Save the selected template name
                Properties.Settings.Default.LastSelectedTemplate = selectedTemplate;
                Properties.Settings.Default.Save();

                // Load this template's specific settings
                LoadTemplateSettings(selectedTemplate);

                // Generate preview of the selected template
                try
                {
                    // Get the values from the loaded template
                    double sheetWidth = double.Parse(Properties.Settings.Default.SheetWidth);
                    double sheetHeight = double.Parse(Properties.Settings.Default.SheetHeight);
                    int numberOfLabels = int.Parse(Properties.Settings.Default.NumberOfLabels);
                    int numberOfColumns = int.Parse(Properties.Settings.Default.NumberOfColumns);
                    double labelWidth = double.Parse(Properties.Settings.Default.LabelWidth);
                    double labelHeight = double.Parse(Properties.Settings.Default.LabelHeight);
                    double marginOX = double.Parse(Properties.Settings.Default.MarginOX);
                    double marginOY = double.Parse(Properties.Settings.Default.MarginOY);
                    double marginIX = double.Parse(Properties.Settings.Default.MarginIX);
                    double marginIY = double.Parse(Properties.Settings.Default.MarginIY);

                    // Render the preview
                    RenderTemplatePreviewInGrid(
                        sheetWidth, sheetHeight, numberOfLabels, numberOfColumns,
                        labelWidth, labelHeight, marginOX, marginOY, marginIX, marginIY
                    );
                }
                catch (Exception ex)
                {
                    // Silently handle errors in preview - don't show a message box
                    Console.WriteLine($"Błąd podczas tworzenia podglądu: {ex.Message}");
                }
            }
        }



        private void ShowResultWindow(List<BarcodeData> barcodes, string templateName)
        {
            try
            {
                // Get template settings from Properties.Settings.Default
                int numberOfColumns = int.Parse(Properties.Settings.Default.NumberOfColumns);
                double labelWidth = double.Parse(Properties.Settings.Default.LabelWidth);
                double labelHeight = double.Parse(Properties.Settings.Default.LabelHeight);
                double marginIX = double.Parse(Properties.Settings.Default.MarginIX);
                double marginIY = double.Parse(Properties.Settings.Default.MarginIY);
                double pageWidth = double.Parse(Properties.Settings.Default.SheetWidth);
                double pageHeight = double.Parse(Properties.Settings.Default.SheetHeight);
                double marginOX = double.Parse(Properties.Settings.Default.MarginOX);
                double marginOY = double.Parse(Properties.Settings.Default.MarginOY);

                // Get the selected barcode type
                string barcodeType = barcodeTypeCB.SelectedItem?.ToString();

                // Verify that a barcode type was selected
                if (string.IsNullOrEmpty(barcodeType))
                {
                    MessageBox.Show("Please select a barcode type.", "Missing Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Per client requirement: Don't show barcode text below images
                bool showBarcodeText = false;

                // Create and show the result window with all necessary parameters
                var resultWindow = new ResultWindow(
                    barcodes,
                    barcodeType,
                    numberOfColumns,
                    labelWidth,
                    labelHeight,
                    marginIX,
                    marginIY,
                    pageWidth,
                    pageHeight,
                    showBarcodeText
                );

                resultWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying barcodes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SaveToHistory(List<BarcodeData> barcodes, string source)
        {
            if (Properties.Settings.Default.BarcodeHistory == null)
                Properties.Settings.Default.BarcodeHistory = new StringCollection();

            // Generate a unique ID for this history entry
            string historyId = Guid.NewGuid().ToString();

            // Add history entry
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string historyEntry = $"{timestamp} - {source} ({barcodes.Count} kodów)|{historyId}";
            Properties.Settings.Default.BarcodeHistory.Add(historyEntry);

            // Save the association between this history entry and its barcodes
            XDocument historyDoc;
            if (string.IsNullOrEmpty(Properties.Settings.Default.BarcodeHistoryAssociations))
            {
                historyDoc = new XDocument(new XElement("HistoryEntries"));
            }
            else
            {
                historyDoc = XDocument.Parse(Properties.Settings.Default.BarcodeHistoryAssociations);
            }

            // Create an element for this history entry
            var entryElement = new XElement("HistoryEntry",
                new XAttribute("Id", historyId),
                new XAttribute("Timestamp", timestamp));

            // Add each barcode to the history entry
            foreach (var barcode in barcodes)
            {
                entryElement.Add(new XElement("Barcode", barcode.Value));
            }

            // Add the entry to the document
            historyDoc.Root.Add(entryElement);

            // Save back to settings
            Properties.Settings.Default.BarcodeHistoryAssociations = historyDoc.ToString();
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

        private void CollapseAllGrids()
        {
            ManualGrid.Visibility = Visibility.Collapsed;
            SettingsGrid.Visibility = Visibility.Collapsed;
            HistoryGrid.Visibility = Visibility.Collapsed;
            TplBtnSpGrid.Visibility = Visibility.Collapsed;
            GenNumGrid.Visibility = Visibility.Collapsed;
            Generate_Grid_.Visibility = Visibility.Collapsed;
            SettingsBtnGrid.Visibility = Visibility.Collapsed;
            GenerateBtnGrid.Visibility = Visibility.Collapsed;
            Generate_Settings_Grid.Visibility = Visibility.Collapsed;
            PreviewSPGrid.Visibility = Visibility.Collapsed;
            TemplateSPGrid.Visibility = Visibility.Collapsed;
            CodesHistSp.Visibility = Visibility.Collapsed;
        }

        private void ShowGrid(Grid activeGrid)
        {
            activeGrid.Visibility = Visibility.Visible;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllGrids();
            ResetSettingsGrid();
            ShowGrid(Generate_Grid_);
            ShowGrid(Generate_Settings_Grid);
            ShowGrid(PreviewSPGrid);
            ShowGrid(GenNumGrid);
            ShowGrid(GenerateBtnGrid);
        }

        private void ManualButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllGrids();
            ResetSettingsGrid();
            ShowGrid(ManualGrid);
            ShowGrid(Generate_Grid_);
            ShowGrid(GenerateBtnGrid);
            ShowGrid(Generate_Settings_Grid);
            ShowGrid(PreviewSPGrid);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllGrids();
            ResetSettingsGrid();
            ShowGrid(SettingsGrid);
            ShowGrid(SettingsBtnGrid);

        }

        // Updated addBtn_Click for consistency with new mode enum
        private void addBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(SettingsGrid);
            SettingsBtnGrid.Visibility = Visibility.Collapsed;
            ShowGrid(TemplateSPGrid);
            ShowGrid(Generate_Grid_);
            ShowGrid(TplBtnSpGrid);
            ShowGrid(PreviewSPGrid);

            // Set mode to adding new template
            currentTemplateMode = TemplateMode.Adding;

            // Make fields editable
            SetTemplateFieldsReadOnly(false);

            // Clear template name field for new entry
            label_name_Tb.Text = string.Empty;

            // Make sure button text is correct
            addtemplBtn.Content = "Zapisz";
            addtemplBtn.FontWeight = FontWeights.Bold;

            // Make sure template name field is visible and editable
            if (FindName("___TmplNameSP_") is StackPanel templateNameStackPanel)
            {
                templateNameStackPanel.Visibility = Visibility.Visible;
            }
        }

        // Updated editBtn_Click for consistency with new mode enum
        private void editBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(SettingsGrid);
            SettingsBtnGrid.Visibility = Visibility.Collapsed;
            ShowGrid(TemplateSPGrid);
            ShowGrid(Generate_Grid_);
            ShowGrid(TplBtnSpGrid);
            ShowGrid(PreviewSPGrid);

            // Set mode to editing existing template
            currentTemplateMode = TemplateMode.Editing;

            // Make fields editable
            SetTemplateFieldsReadOnly(false);

            // Make sure button text is correct
            addtemplBtn.Content = "Zapisz";
            addtemplBtn.FontWeight = FontWeights.Bold;

            // Change format title to show we're in edit mode with bold text
            TextBlock formatTitleTextBlock = FindName("___Name_FormatTB_") as TextBlock;
            if (formatTitleTextBlock != null)
            {
                formatTitleTextBlock.Text = "Szablon";
                formatTitleTextBlock.FontWeight = FontWeights.Bold;
            }

            // Change template name text block to "Nowa nazwa szablonu" with bold text
            TextBlock tmplNameTextBlock = FindName("TmplName") as TextBlock;
            if (tmplNameTextBlock != null)
            {
                tmplNameTextBlock.Text = "Nowa nazwa szablonu";
                tmplNameTextBlock.FontWeight = FontWeights.Bold;
            }

            // Populate formatCb with template names instead of paper sizes
            PopulateTemplateSelector();

            // Add SelectionChanged handler to load template settings when template is selected
            formatCb.SelectionChanged -= formatCb_SelectionChanged; // Remove the paper size handler
            formatCb.SelectionChanged += EditTemplateSelectionChanged; // Add template selection handler
        }

        private void PopulateTemplateSelector()
        {
            // Get template names from settings
            var templateNames = Properties.Settings.Default.TemplateNames;
            if (templateNames != null)
            {
                var templates = templateNames.Cast<string>().ToList();
                formatCb.ItemsSource = templates;

                if (templates.Count > 0)
                {
                    formatCb.SelectedIndex = 0; // Select first template by default
                }
            }
        }

        private void EditTemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected template name
            if (formatCb.SelectedItem != null)
            {
                string selectedTemplate = formatCb.SelectedItem.ToString();

                // Store the original template name for later comparison
                originalTemplateName = selectedTemplate;

                // Load this template's settings into the UI fields
                LoadTemplateSettings(selectedTemplate);

                // Save the template name to use when saving changes
                label_name_Tb.Text = selectedTemplate;
            }
        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(SettingsGrid);
            SettingsBtnGrid.Visibility = Visibility.Collapsed;
            ShowGrid(TemplateSPGrid);
            ShowGrid(Generate_Grid_);
            ShowGrid(TplBtnSpGrid);
            ShowGrid(PreviewSPGrid);

            // Set mode to deleting template
            currentTemplateMode = TemplateMode.Deleting;

            // Change format title text and make it bold
            TextBlock formatTitleTextBlock = FindName("___Name_FormatTB_") as TextBlock;
            if (formatTitleTextBlock != null)
            {
                formatTitleTextBlock.Text = "Szablon do usunięcia";
                formatTitleTextBlock.FontWeight = FontWeights.Bold;
            }

            // Hide template name stack panel
            StackPanel templateNameStackPanel = FindName("___TmplNameSP_") as StackPanel;
            if (templateNameStackPanel != null)
            {
                templateNameStackPanel.Visibility = Visibility.Collapsed;
            }

            // Populate formatCb with template names
            PopulateTemplateSelector();

            // Configure text boxes to be in read-only mode
            SetTemplateFieldsReadOnly(true);

            // Add SelectionChanged handler to load template settings when template is selected
            formatCb.SelectionChanged -= formatCb_SelectionChanged;
            formatCb.SelectionChanged += DeleteTemplateSelectionChanged;

            // Change addtemplBtn text to "Usuń" with bold text
            Button deleteTemplateButton = addtemplBtn;
            if (deleteTemplateButton != null)
            {
                deleteTemplateButton.Content = "Usuń";
                deleteTemplateButton.FontWeight = FontWeights.Bold;
            }
        }

        // Helper method to set all template fields to read-only or editable
        private void SetTemplateFieldsReadOnly(bool readOnly)
        {
            // Set all relevant text boxes to read-only mode
            sizeXTb.IsReadOnly = readOnly;
            sizeYTb.IsReadOnly = readOnly;
            mariginO_X_Tb.IsReadOnly = readOnly;
            mariginO_Y_Tb.IsReadOnly = readOnly;
            mariginI_X_Tb.IsReadOnly = readOnly;
            mariginI_Y_Tb.IsReadOnly = readOnly;
            label_size_X_Tb.IsReadOnly = readOnly;
            label_size_Y_Tb.IsReadOnly = readOnly;
            no_labels_Tb.IsReadOnly = readOnly;
            no_column_Tb.IsReadOnly = readOnly;
            label_name_Tb.IsReadOnly = readOnly;

            // Also update visual appearance to indicate read-only status
            var background = readOnly ? System.Windows.Media.Brushes.LightGray : System.Windows.Media.Brushes.White;

            sizeXTb.Background = background;
            sizeYTb.Background = background;
            mariginO_X_Tb.Background = background;
            mariginO_Y_Tb.Background = background;
            mariginI_X_Tb.Background = background;
            mariginI_Y_Tb.Background = background;
            label_size_X_Tb.Background = background;
            label_size_Y_Tb.Background = background;
            no_labels_Tb.Background = background;
            no_column_Tb.Background = background;
            label_name_Tb.Background = background;
        }

        // Modified to handle template selection for deletion
        private void DeleteTemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (formatCb.SelectedItem != null)
            {
                string selectedTemplate = formatCb.SelectedItem.ToString();

                // Store the original template name for deletion
                originalTemplateName = selectedTemplate;

                // Load template settings for display (will be read-only)
                LoadTemplateSettings(selectedTemplate);
            }
        }

        // Method to reset the SettingsGrid when it's collapsed or we switch views
        private void ResetSettingsGrid()
        {
            // Reset the mode flag
            currentTemplateMode = TemplateMode.None;
            originalTemplateName = string.Empty;

            // Reset fields to be editable
            SetTemplateFieldsReadOnly(false);

            // Reset the format title text and font weight
            TextBlock formatTitleTextBlock = FindName("___Name_FormatTB_") as TextBlock;
            if (formatTitleTextBlock != null)
            {
                formatTitleTextBlock.Text = "Format papieru";
                formatTitleTextBlock.FontWeight = FontWeights.Bold;
            }

            // Reset the template name text block
            TextBlock tmplNameTextBlock = FindName("TmplName") as TextBlock;
            if (tmplNameTextBlock != null)
            {
                tmplNameTextBlock.Text = "Nazwa szablonu";
                tmplNameTextBlock.FontWeight = FontWeights.Bold;
            }

            // Reset button text
            addtemplBtn.Content = "Zapisz";
            addtemplBtn.FontWeight = FontWeights.Bold;

            // Show the template name stack panel
            StackPanel templateNameStackPanel = FindName("___TmplNameSP_") as StackPanel;
            if (templateNameStackPanel != null)
            {
                templateNameStackPanel.Visibility = Visibility.Visible;
            }

            // Reset formatCb to show paper sizes
            formatCb.SelectionChanged -= EditTemplateSelectionChanged; // Remove template selection handler
            formatCb.SelectionChanged -= DeleteTemplateSelectionChanged; // Remove delete selection handler
            formatCb.SelectionChanged += formatCb_SelectionChanged; // Add paper size handler back

            // Reinitialize paper sizes
            InitializePaperSizes();
        }

        private void LoadToManualBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get codes from codesLB
            var codes = codesLB.ItemsSource as List<string>;

            if (codes != null && codes.Any())
            {
                // Clear existing text in manualTB_
                manualTB_.Text = string.Empty;

                // Add each code on a new line
                manualTB_.Text = string.Join(Environment.NewLine, codes);

                // Switch to Manual view (same as clicking the Manual button)
                ManualButton_Click(sender, e);
            }
            else
            {
                MessageBox.Show("Brak dostępnych kodów do załadowania.");
            }
        }


        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllGrids();
            ResetSettingsGrid();
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
            string extension = IOPath.GetExtension(filePath).ToLower();

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

                // Show CodesHistSP grid
                CodesHistSp.Visibility = Visibility.Visible;

                // Populate codesLB with barcodes from the selected history entry
                List<string> barcodes = GetBarcodesForHistoryEntry(selectedEntry);
                codesLB.ItemsSource = barcodes;
            }
        }

        private List<string> GetBarcodesForHistoryEntry(string historyEntry)
        {
            List<string> barcodes = new List<string>();

            try
            {
                // Extract history ID from the entry
                string[] parts = historyEntry.Split('|');
                if (parts.Length < 2)
                {
                    // Old format history entries won't have the ID
                    return barcodes;
                }

                string historyId = parts[1];

                // Load XML with history-barcode associations
                if (string.IsNullOrEmpty(Properties.Settings.Default.BarcodeHistoryAssociations))
                {
                    return barcodes;
                }

                XDocument historyDoc = XDocument.Parse(Properties.Settings.Default.BarcodeHistoryAssociations);

                // Find the history entry with this ID
                var entryElement = historyDoc.Root.Elements("HistoryEntry")
                    .FirstOrDefault(e => e.Attribute("Id")?.Value == historyId);

                if (entryElement == null)
                {
                    return barcodes;
                }

                // Get all barcodes for this history entry
                foreach (var barcodeElement in entryElement.Elements("Barcode"))
                {
                    barcodes.Add(barcodeElement.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas pobierania kodów kreskowych: {ex.Message}");
            }

            return barcodes;
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
                        // Convert string barcodes to BarcodeData objects
                        List<BarcodeData> barcodeDataList = barcodes.Select(code => new BarcodeData { Value = code }).ToList();

                        // Save to history with the new method
                        SaveToHistory(barcodeDataList, $"Imported from {IOPath.GetFileName(filePath)}");

                        MessageBox.Show($"Pomyślnie załadowano {barcodes.Count} kodów kreskowych z pliku: {IOPath.GetFileName(filePath)}");
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