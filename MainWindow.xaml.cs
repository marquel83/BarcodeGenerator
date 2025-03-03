using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Microsoft.Win32;

namespace BarcodeGenerator
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
    
    // Initialize settings for paper formats and label templates
    InitializeSettingsControls();
    
    // Set default text for template name
    label_name_Tb.Text = "New Template";

            // Trigger the format selection to populate size fields
            if (formatCb.SelectedItem != null)
            {
                FormatCb_SelectionChanged(formatCb, null);
            }
        }
        
        // Ustawienie domyślnej wartości po uruchomieniu programu
        private void MyComboBox_Loaded(object sender, RoutedEventArgs e)
         {
             string lastSelected = Properties.Settings.Default.LastSelectedItem;

             // Jeśli ustawienie istnieje, znajdź i wybierz element
             if (!string.IsNullOrEmpty(lastSelected))
             {
                 foreach (ComboBoxItem item in MyComboBox.Items)
                 {
                     if (item.Content.ToString() == lastSelected)
                     {
                         MyComboBox.SelectedItem = item;
                         break;
                     }
                 }
             }
             else
             {
                 // Jeśli brak zapisanego wyboru, ustaw pierwszy element jako domyślny
                 MyComboBox.SelectedIndex = 0;
             }
         }

         // Zapisywanie wyboru użytkownika
         private void MyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
         {
             if (MyComboBox.SelectedItem is ComboBoxItem selectedItem)
             {
                 Properties.Settings.Default.LastSelectedItem = selectedItem.Content.ToString();
                 Properties.Settings.Default.Save();
             }
         }

        private void ShowGrid(Grid activeGrid)
        {
            HomeGrid.Visibility = Visibility.Collapsed;
            ManualGrid.Visibility = Visibility.Collapsed;
            SettingsGrid.Visibility = Visibility.Collapsed;
            HistoryGrid.Visibility = Visibility.Collapsed;

            activeGrid.Visibility = Visibility.Visible;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(HomeGrid);
            ShowGrid(Generate_Grid_);
        }

        private void ManualButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(ManualGrid);
            ShowGrid(Generate_Grid_);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(SettingsGrid);
            Generate_Grid_.Visibility = Visibility.Collapsed;

            // Make sure the paper format is initialized correctly
            if (formatCb.SelectedItem != null)
            {
                FormatCb_SelectionChanged(formatCb, null);
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(HistoryGrid);
            Generate_Grid_.Visibility = Visibility.Collapsed;
        }

        // Zamykanie aplikacji
        private void MainExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Obsługa przycisku do ładowania plików

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Wszystkie obsługiwane pliki|*.xlsx;*.xls;*.txt;*.doc;*.docx;*.odt;*.csv|Pliki Excel (*.xlsx, *.xls)|*.xlsx;*.xls|Pliki tekstowe (*.txt, *.csv)|*.txt;*.csv|Dokumenty Word (*.doc, *.docx, *.odt)|*.doc;*.docx;*.odt|Wszystkie pliki (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"Załadowano plik: {filePath}");
                // Tutaj można dodać dalszą obsługę pliku
            }
        }

        // Walidacja wprowadzenia tylko liczb w NumberTB
        private void NumberTB_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, "^[0-9]*$"); // Akceptuje tylko cyfry 0-9
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addtemplBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (!double.TryParse(sizeXTb.Text, out double sheetWidth) ||
                    !double.TryParse(sizeYTb.Text, out double sheetHeight) ||
                    !double.TryParse(mariginO_X_Tb.Text, out double marginOX) ||
                    !double.TryParse(mariginO_Y_Tb.Text, out double marginOY) ||
                    !double.TryParse(mariginI_X_Tb.Text, out double marginIX) ||
                    !double.TryParse(mariginI_Y_Tb.Text, out double marginIY) ||
                    !double.TryParse(label_size_X_Tb.Text, out double labelWidth) ||
                    !double.TryParse(label_size_Y_Tb.Text, out double labelHeight) ||
                    !int.TryParse(no_labels_Tb.Text, out int labelsCount) ||
                    !int.TryParse(no_columns_Tb.Text, out int columnsCount) ||
                    labelsCount <= 0 ||
                    columnsCount <= 0)
                {
                    MessageBox.Show("Please enter valid numeric values for all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Generate a default name if none is provided
                string templateName = label_name_Tb.Text.Trim();
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    templateName = GenerateDefaultTemplateName(labelsCount, columnsCount);
                }

                // Create a template object
                var template = new LabelTemplate
                {
                    Name = templateName,
                    Format = formatCb.SelectedItem.ToString(),
                    SheetWidth = sheetWidth,
                    SheetHeight = sheetHeight,
                    MarginOuterX = marginOX,
                    MarginOuterY = marginOY,
                    MarginInnerX = marginIX,
                    MarginInnerY = marginIY,
                    LabelWidth = labelWidth,
                    LabelHeight = labelHeight,
                    LabelsCount = labelsCount,
                    ColumnsCount = columnsCount
                };

                // Save template to settings
                SaveTemplate(template);

                MessageBox.Show($"Template '{template.Name}' has been saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear or reset form
                label_name_Tb.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateDefaultTemplateName(int labelsCount, int columnsCount)
        {
            string baseName = $"Labels_{labelsCount}_{columnsCount}";

            // Check if template with this name already exists
            List<LabelTemplate> existingTemplates = LoadTemplates();

            // If no template with this name exists, use it as is
            if (!existingTemplates.Any(t => t.Name.StartsWith(baseName)))
            {
                return baseName;
            }

            // Find templates with similar names to determine the next number
            var similarTemplates = existingTemplates
                .Where(t => t.Name.StartsWith(baseName))
                .Select(t => t.Name)
                .ToList();

            // If we have names like "Labels_8_2", "Labels_8_2_1", find the highest suffix
            int highestSuffix = 0;

            foreach (var name in similarTemplates)
            {
                // If the name is exactly the base name, then we need at least suffix 1
                if (name == baseName)
                {
                    highestSuffix = Math.Max(highestSuffix, 1);
                    continue;
                }

                // Check if there's a suffix like "_1", "_2", etc.
                var parts = name.Split('_');
                if (parts.Length >= 4 && int.TryParse(parts[3], out int suffix))
                {
                    highestSuffix = Math.Max(highestSuffix, suffix + 1);
                }
                else
                {
                    // If there's a matching name without an ordinal, we need at least suffix 1
                    highestSuffix = Math.Max(highestSuffix, 1);
                }
            }

            // Return base name with suffix if needed
            return highestSuffix > 0 ? $"{baseName}_{highestSuffix}" : baseName;
        }
        private void SaveTemplate(LabelTemplate template)
{
    // Get existing templates or create new list
    List<LabelTemplate> templates = LoadTemplates();
    
    // Check if template with same name exists
    int existingIndex = templates.FindIndex(t => t.Name == template.Name);
    if (existingIndex >= 0)
    {
        // Replace existing template
        templates[existingIndex] = template;
    }
    else
    {
        // Add new template
        templates.Add(template);
    }
    
    // Save templates to settings
    string serializedTemplates = Newtonsoft.Json.JsonConvert.SerializeObject(templates);
    Properties.Settings.Default.LabelTemplates = serializedTemplates;
    Properties.Settings.Default.Save();
}

private List<LabelTemplate> LoadTemplates()
{
    try
    {
        string serializedTemplates = Properties.Settings.Default.LabelTemplates;
        if (string.IsNullOrEmpty(serializedTemplates))
        {
            return new List<LabelTemplate>();
        }
        
        return Newtonsoft.Json.JsonConvert.DeserializeObject<List<LabelTemplate>>(serializedTemplates);
    }
    catch
    {
        return new List<LabelTemplate>();
    }
}

                // Add this to your MainWindow class
        private Dictionary<string, (double width, double height)> paperSizes = new Dictionary<string, (double, double)>()
        {
            { "A3", (297, 420) },
            { "A4", (210, 297) },
            { "A5", (148, 210) },
            { "A6", (105, 148) },
            { "Letter", (216, 279) },
            { "Legal", (216, 356) },
            { "Custom", (0, 0) }
        };

        private void InitializeSettingsControls()
        {
            // Fill the paper format ComboBox
            foreach (var size in paperSizes)
            {
                formatCb.Items.Add(size.Key);
            }
            formatCb.SelectedIndex = 1; // Default to A4

            // Add event handlers
            formatCb.SelectionChanged += FormatCb_SelectionChanged;
            no_labels_Tb.TextChanged += No_labels_Tb_TextChanged;
            no_columns_Tb.TextChanged += No_columns_Tb_TextChanged;

            // Add handlers for margin textboxes
            mariginO_X_Tb.TextChanged += Margin_TextChanged;
            mariginO_Y_Tb.TextChanged += Margin_TextChanged;
            mariginI_X_Tb.TextChanged += Margin_TextChanged;
            mariginI_Y_Tb.TextChanged += Margin_TextChanged;

            // Add handlers for sheet size changes
            sizeXTb.TextChanged += SheetSize_TextChanged;
            sizeYTb.TextChanged += SheetSize_TextChanged;

            // Add numeric validation to all textboxes in SettingsGrid
            AddNumericValidationToTextBoxes();
        }

        private void No_columns_Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateLabelSize();
        }

        private void Margin_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateLabelSize();
        }

        private void SheetSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateLabelSize();
        }

        private void FormatCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedFormat = formatCb.SelectedItem as string;
            if (selectedFormat != null && paperSizes.ContainsKey(selectedFormat))
            {
                var (width, height) = paperSizes[selectedFormat];
                
                // Don't update the fields if "Custom" is selected
                if (selectedFormat != "Custom")
                {
                    sizeXTb.Text = width.ToString();
                    sizeYTb.Text = height.ToString();
                }
                
                CalculateLabelSize();
            }
        }

        private void AddNumericValidationToTextBoxes()
        {
            // Get all TextBox controls in SettingsGrid
            var textboxes = FindVisualChildren<TextBox>(SettingsGrid);
            foreach (var textbox in textboxes)
            {
                textbox.PreviewTextInput += NumericTextBox_PreviewTextInput;
            }
        }

        // Helper method to find all controls of a specific type
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits and decimal point
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void No_labels_Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateLabelSize();
        }

        private void CalculateLabelSize()
        {
            try
            {
                if (!double.TryParse(sizeXTb.Text, out double sheetWidth) ||
                    !double.TryParse(sizeYTb.Text, out double sheetHeight) ||
                    !double.TryParse(mariginO_X_Tb.Text, out double marginOX) ||
                    !double.TryParse(mariginO_Y_Tb.Text, out double marginOY) ||
                    !double.TryParse(mariginI_X_Tb.Text, out double marginIX) ||
                    !double.TryParse(mariginI_Y_Tb.Text, out double marginIY) ||
                    !int.TryParse(no_labels_Tb.Text, out int labelsCount) ||
                    labelsCount <= 0)
                {
                    return; // Not enough valid information to calculate
                }

                // Calculate available space after external margins
                double availableWidth = sheetWidth - (2 * marginOX);
                double availableHeight = sheetHeight - (2 * marginOY);

                // Get the user-specified number of columns if provided
                int columns;
                bool hasSpecifiedColumns = int.TryParse(no_columns_Tb.Text, out columns) && columns > 0;

                // Calculate optimal label dimensions based on count and column specification
                int cols, rows;

                if (hasSpecifiedColumns)
                {
                    // Use user-specified number of columns
                    cols = columns;
                    rows = (int)Math.Ceiling((double)labelsCount / cols);
                }
                else if (labelsCount < 8)
                {
                    // For fewer than 8 labels without specified columns, use a more square-ish arrangement
                    cols = (int)Math.Ceiling(Math.Sqrt(labelsCount));
                    rows = (int)Math.Ceiling((double)labelsCount / cols);
                }
                else
                {
                    // For 8 or more labels, prioritize horizontal arrangement
                    // Try to make the labels wider than tall (X > Y)
                    double aspectRatio = availableWidth / availableHeight;
                    int potentialRows = (int)Math.Sqrt(labelsCount / aspectRatio);
                    rows = Math.Max(1, potentialRows);
                    cols = (int)Math.Ceiling((double)labelsCount / rows);
                }

                // Calculate label dimensions considering internal margins
                double labelWidth = (availableWidth - ((cols - 1) * marginIX)) / cols;
                double labelHeight = (availableHeight - ((rows - 1) * marginIY)) / rows;

                // Ensure X side is longer than Y side if we're not using user-specified columns
                if (!hasSpecifiedColumns && labelWidth < labelHeight)
                {
                    // Swap rows and columns if it would result in wider labels
                    if ((availableWidth / rows) > (availableHeight / cols))
                    {
                        int temp = rows;
                        rows = cols;
                        cols = temp;

                        // Recalculate dimensions
                        labelWidth = (availableWidth - ((cols - 1) * marginIX)) / cols;
                        labelHeight = (availableHeight - ((rows - 1) * marginIY)) / rows;
                    }
                }

                // Update columns textbox to reflect the actual calculation
                if (!hasSpecifiedColumns)
                {
                    no_columns_Tb.Text = cols.ToString();
                }

                // Update label size textboxes
                label_size_X_Tb.Text = Math.Round(labelWidth, 2).ToString();
                label_size_Y_Tb.Text = Math.Round(labelHeight, 2).ToString();
            }
            catch
            {
                // Handle any calculation errors silently
            }
        }
        public class LabelTemplate
        {
            public string Name { get; set; }
            public string Format { get; set; }
            public double SheetWidth { get; set; }
            public double SheetHeight { get; set; }
            public double MarginOuterX { get; set; }
            public double MarginOuterY { get; set; }
            public double MarginInnerX { get; set; }
            public double MarginInnerY { get; set; }
            public double LabelWidth { get; set; }
            public double LabelHeight { get; set; }
            public int LabelsCount { get; set; }
            public int ColumnsCount { get; set; }
        }

    }
    

    

    }
