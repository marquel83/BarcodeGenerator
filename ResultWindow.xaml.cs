using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfImage = System.Windows.Controls.Image;

namespace BarcodeGenerator
{
    /// <summary>
    /// Interaction logic for ResultWindow.xaml
    /// </summary>
    public partial class ResultWindow : Window
    {
        // Private fields
        private List<BarcodeData> barcodes;
        private List<List<BarcodeData>> barcodePages;
        private int currentPageIndex = 0;
        private int numberOfColumns;
        private int numberOfRows;
        private double labelWidth;
        private double labelHeight;
        private double marginIX;
        private double marginIY;
        private double pageWidth;
        private double pageHeight;
        private string currentBarcodeType;
        private bool showBarcodeText;
        private bool showBorders = true;

        // Constructor
        public ResultWindow(List<BarcodeData> barcodeData, string barcodeType, int columns, double lWidth, double lHeight, double mIX, double mIY, double pWidth, double pHeight, bool displayBarcodeText, bool showBordersInitial = true)
        {
            InitializeComponent();

            // Initialize all the parameters
            barcodes = barcodeData;
            currentBarcodeType = barcodeType;
            numberOfColumns = columns;
            labelWidth = lWidth;
            labelHeight = lHeight;
            marginIX = mIX;
            marginIY = mIY;
            pageWidth = pWidth;
            pageHeight = pHeight;
            showBarcodeText = displayBarcodeText;
            showBorders = showBordersInitial;
            showBordersCheckbox.IsChecked = showBorders;

            // In the ResultWindow constructor
            this.Width = pageWidth * 3.8; // Convert mm to screen pixels (approximate)
            this.Height = pageHeight * 3.8;

            // Calculate number of rows based on barcode count and columns
            numberOfRows = (int)Math.Floor(((double)pageHeight - marginIY) / (labelHeight + marginIY));

            // Split barcodes into pages
            barcodePages = SplitBarcodesIntoPages(barcodes);

            // Setup navigation buttons
            SetupNavigationButtons();

            // Initialize QuestPDF license if needed
            QuestPDF.Settings.License = LicenseType.Community;

            // Display the barcodes in the window
            DisplayCurrentPage();
        }

        private void ShowBordersCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            showBorders = showBordersCheckbox.IsChecked ?? true;

            // Refresh the display to show/hide borders
            DisplayBarcodes();
        }

        // Add click event handlers for navigation buttons
        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPageIndex > 0)
            {
                currentPageIndex--;
                DisplayBarcodes(); // This will now use the current page index
                UpdateNavigationButtonState();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPageIndex < barcodePages.Count - 1)
            {
                currentPageIndex++;
                DisplayBarcodes(); // This will now use the current page index
                UpdateNavigationButtonState();
            }
        }

        // Method to set up the navigation buttons
        private void SetupNavigationButtons()
        {
            // Connect buttons to their click handlers
            previouspageBtn.Click += PreviousPage_Click;
            nextpageBtn.Click += NextPage_Click;

            // Initially update button visibility
            UpdateNavigationButtonState();
        }

        // Update button enabled state based on current page
        private void UpdateNavigationButtonState()
        {
            // If only one page or no pages, disable both buttons
            if (barcodePages == null || barcodePages.Count <= 1)
            {
                previouspageBtn.IsEnabled = false;
                nextpageBtn.IsEnabled = false;
                return;
            }

            // Update button states based on current page
            previouspageBtn.IsEnabled = currentPageIndex > 0;
            nextpageBtn.IsEnabled = currentPageIndex < barcodePages.Count - 1;
        }

        // Display the current page of barcodes
        private void DisplayCurrentPage()
        {
            if (barcodePages.Count > 0 && currentPageIndex >= 0 && currentPageIndex < barcodePages.Count)
            {
                List<BarcodeData> currentPageBarcodes = barcodePages[currentPageIndex];
                DisplayBarcodes(currentPageBarcodes);
                UpdateNavigationButtonState();
            }
        }

        // Window mouse down event handler
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // Display barcodes in the UI
        private void DisplayBarcodes(List<BarcodeData> pageOfBarcodes)
        {
            DisplayBarcodesOnePage(pageOfBarcodes);
        }

        // Call to support existing functionality
        private void DisplayBarcodes()
        {
            if (barcodePages != null && barcodePages.Count > 0)
            {
                // Display the current page of barcodes
                DisplayBarcodes(barcodePages[currentPageIndex]);
            }
            else if (barcodes != null)
            {
                // Fallback to original behavior if something went wrong with page splitting
                DisplayBarcodesOnePage(barcodes);
            }
        }

        private void DisplayBarcodesOnePage(List<BarcodeData> pageOfBarcodes)
        {
            // Clear any existing content
            BarcodesGrid.Children.Clear();
            BarcodesGrid.RowDefinitions.Clear();
            BarcodesGrid.ColumnDefinitions.Clear();

            // Calculate dimensions and rows
            int displayRows = (int)Math.Ceiling((double)pageOfBarcodes.Count / numberOfColumns);

            // Set up grid dimensions to match the paper size
            BarcodesGrid.Width = pageWidth * 3.8; // Convert from mm to pixels
            BarcodesGrid.Height = pageHeight * 3.8;

            // Create outer margins
            double marginOX = (pageWidth - (numberOfColumns * labelWidth + (numberOfColumns - 1) * marginIX)) / 2;
            double marginOY = (pageHeight - (displayRows * labelHeight + (displayRows - 1) * marginIY)) / 2;

            // Create rows for labels and spacing
            for (int i = 0; i < displayRows; i++)
            {
                // Add label row
                BarcodesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(labelHeight * 3.8) });

                // Add spacing row if not the last row
                if (i < displayRows - 1)
                {
                    BarcodesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(marginIY * 3.8) });
                }
            }

            // Create columns for labels and spacing
            for (int i = 0; i < numberOfColumns; i++)
            {
                // Add label column
                BarcodesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth * 3.8) });

                // Add spacing column if not the last column
                if (i < numberOfColumns - 1)
                {
                    BarcodesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(marginIX * 3.8) });
                }
            }

            // Add barcodes to the grid
            int barcodeIndex = 0;
            for (int row = 0; row < displayRows && barcodeIndex < pageOfBarcodes.Count; row++)
            {
                for (int col = 0; col < numberOfColumns && barcodeIndex < pageOfBarcodes.Count; col++)
                {
                    var barcode = pageOfBarcodes[barcodeIndex];

                    // Create a border for the label
                    var border = new Border
                    {
                        BorderBrush = showBorders ? Brushes.Black : Brushes.Transparent,
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(2)
                    };

                    // Create container for the barcode
                    var panel = new Grid();

                    // Create the image element
                    var image = new WpfImage
                    {
                        Source = barcode.Image,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    };

                    // Size the image appropriately based on barcode type
                    if (currentBarcodeType == "QR_CODE")
                    {
                        // For QR codes, use the smaller dimension to maintain square aspect ratio
                        double dimension = Math.Min(labelWidth * 0.9, labelHeight * 0.9) * 3.8;
                        image.Width = dimension;
                        image.Height = dimension;
                    }
                    else
                    {
                        // For linear barcodes, use more width than height
                        image.Width = labelWidth * 0.9 * 3.8;
                        image.Height = labelHeight * 0.7 * 3.8;
                    }

                    panel.Children.Add(image);
                    border.Child = panel;

                    // Add to grid at the proper position
                    Grid.SetRow(border, row * 2);
                    Grid.SetColumn(border, col * 2);
                    BarcodesGrid.Children.Add(border);

                    barcodeIndex++;
                }
            }
        }

        // Print button click event handler
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            // Add print functionality if needed
            MessageBox.Show("Print functionality not implemented yet.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Save PDF button click event handler - matches the name in your XAML
        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    Title = "Save Barcode PDF",
                    FileName = "Barcodes.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    // Generate and save the PDF
                    if (GeneratePdfAndSaveToFile(barcodes, saveFileDialog.FileName))
                    {
                        MessageBox.Show($"Plik PDF zapisano pomyœlnie w:\n{saveFileDialog.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Ask if user wants to open the PDF
                        if (MessageBox.Show("Czy chcesz teraz otworzyæ plik PDF?",
                            "Wygenerowano plik PDF", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            // Open the PDF with the default PDF viewer
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveFileDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nie uda³o siê zapisaæ pliku PDF.", "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    Mouse.OverrideCursor = null;
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"B³¹d podczas generowania pliku PDF: {ex.Message}", "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Exit button click event handler - matches the name in your XAML
        private void PreviewExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #region PDF Generation Methods

        /// <summary>
        /// Creates barcode labels layout for a single page.
        /// </summary>
        /// <param name="container">The container to render barcodes into.</param>
        /// <param name="barcodes">The list of barcodes to display on this page.</param>
        private void CreateBarcodeLabelsForPdf(IContainer container, List<BarcodeData> barcodes)
        {
            int numberOfRowsInPage = (int)Math.Ceiling((double)barcodes.Count / numberOfColumns);

            // Safety check for barcodeType
            string barcodeType = currentBarcodeType ?? "CODE_128";

            container.Table(table =>
            {
                // Define columns with proper dimensions
                table.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < numberOfColumns; i++)
                    {
                        // Add label column
                        columns.ConstantColumn((float)labelWidth, Unit.Millimetre);

                        // Add spacing column if not the last column
                        if (i < numberOfColumns - 1)
                        {
                            columns.ConstantColumn((float)marginIX, Unit.Millimetre);
                        }
                    }
                });

                // Create rows with barcodes
                int barcodeIndex = 0;

                for (int row = 0; row < numberOfRowsInPage && barcodeIndex < barcodes.Count; row++)
                {
                    // Create cells for this row
                    for (int col = 0; col < numberOfColumns && barcodeIndex < barcodes.Count; col++)
                    {
                        BarcodeData barcode = barcodes[barcodeIndex];
                        uint colIndex = (uint)(col * 2); // Account for spacing columns
                        uint rowIndex = (uint)(row * 2);

                        // Add barcode cell at specific position
                        table.Cell()
                            .Column(colIndex + 1)  // Column numbers start at 1 in QuestPDF
                            .Row(rowIndex + 1)     // Row numbers start at 1 in QuestPDF
                            .Element(cellContainer =>
                            {
                                // Calculate image dimensions based on barcode type
                                float imageWidth, imageHeight;

                                if (barcodeType == "QR_CODE")
                                {
                                    float dimension = (float)Math.Min(labelWidth, labelHeight) * 0.9f;
                                    imageWidth = dimension;
                                    imageHeight = dimension;
                                }
                                else
                                {
                                    imageWidth = (float)labelWidth * 0.95f;
                                    imageHeight = (float)labelHeight * 0.7f;
                                }

                                // Convert barcode image to byte array
                                byte[] imageBytes = ConvertBitmapSourceToByteArray(barcode.Image);

                                // Apply all formatting in a single chain to avoid container reuse
                                cellContainer
                                .Border(0.5f)
                                .BorderColor(showBorders ? QuestPDF.Helpers.Colors.Black : QuestPDF.Helpers.Colors.Transparent)
                                .Height((float)labelHeight, Unit.Millimetre)
                                .Padding(2, Unit.Millimetre)
                                .Element(container =>
                                {
                                    // Image needs to be properly constrained and centered
                                    container
                                        .AlignCenter()
                                        .AlignMiddle()
                                        // Ensure the container takes full available space
                                        .Height((float)labelHeight - 4, Unit.Millimetre)// Subtract padding (2mm on top and bottom)
                                                                                        // Then set the image with appropriate dimensions
                                        .Element(imageContainer =>
                                        {
                                            imageContainer
                                                .Width(imageWidth, Unit.Millimetre)
                                                .Height(imageHeight, Unit.Millimetre)
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Image(imageBytes, ImageScaling.FitArea);
                                        });
                                });

                                barcodeIndex++;
                            });

                        // Add spacing row if not the last row and we have more barcodes
                        if (row < numberOfRowsInPage - 1 && barcodeIndex < barcodes.Count)
                        {
                            // Create an empty row for spacing
                            uint spacingRowIndex = (uint)((row * 2) + 1);

                            // Add spacing row
                            table.Cell()
                                .ColumnSpan((uint)((numberOfColumns * 2) - 1))
                                .Row(spacingRowIndex + 1)  // Row numbers start at 1 in QuestPDF
                                .Height((float)marginIY, Unit.Millimetre);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Converts a BitmapSource to a byte array for use with QuestPDF.
        /// </summary>
        /// <param name="image">The barcode image to convert.</param>
        /// <returns>Image as byte array.</returns>
        private byte[] ConvertBitmapSourceToByteArray(BitmapSource image)
        {
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder(); // Or use JpegBitmapEncoder for JPEG
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Organizes barcodes into pages based on how many can fit on each page.
        /// </summary>
        /// <param name="allBarcodes">Complete list of all barcodes to include.</param>
        /// <returns>List of barcode collections, with each collection representing a page.</returns>
        private List<List<BarcodeData>> SplitBarcodesIntoPages(List<BarcodeData> allBarcodes)
        {
            // Calculate how many barcodes can fit on one page
            int barcodesPerPage = numberOfColumns * numberOfRows;

            // Divide barcodes into pages
            var result = new List<List<BarcodeData>>();

            for (int i = 0; i < allBarcodes.Count; i += barcodesPerPage)
            {
                var pageOfBarcodes = allBarcodes
                    .Skip(i)
                    .Take(barcodesPerPage)
                    .ToList();

                result.Add(pageOfBarcodes);
            }

            return result;
        }

        /// <summary>
        /// Generates a multi-page PDF document with barcode labels.
        /// </summary>
        /// <param name="barcodePages">List of barcode collections, where each collection represents a page.</param>
        /// <returns>PDF document as byte array.</returns>
        /// <summary>
        /// Generates a multi-page PDF document with barcode labels.
        /// </summary>
        /// <param name="barcodePages">List of barcode collections, where each collection represents a page.</param>
        /// <returns>PDF document as byte array.</returns>
        private byte[] CreateMultiPagePdf(List<List<BarcodeData>> barcodePages)
        {
            // Create the document
            var document = Document.Create(container =>
            {
                // Add each page of barcodes
                for (int pageIndex = 0; pageIndex < barcodePages.Count; pageIndex++)
                {
                    var pageOfBarcodes = barcodePages[pageIndex];

                    // Skip empty pages
                    if (pageOfBarcodes == null || !pageOfBarcodes.Any())
                        continue;

                    container.Page(page =>
                    {
                        // Define page size based on your label dimensions and margins
                        page.Size((float)pageWidth, (float)pageHeight, Unit.Millimetre);
                        page.Margin(0);

                        // Content for this page
                        page.Content().Element(contentContainer =>
                        {
                            // Create a new container for each page
                            CreateBarcodeLabelsForPdf(contentContainer, pageOfBarcodes);
                        });

                        // Remove the footer to eliminate the margin at the bottom
                        // No page numbers will be shown
                    });
                }
            });

            // Generate the PDF as a byte array
            return document.GeneratePdf();
        }

        /// <summary>
        /// Generates a single page PDF with barcode labels.
        /// </summary>
        /// <param name="barcodes">List of barcodes to include on the page.</param>
        /// <returns>PDF document as byte array.</returns>
        /// <summary>
        /// Generates a single page PDF with barcode labels.
        /// </summary>
        /// <param name="barcodes">List of barcodes to include on the page.</param>
        /// <returns>PDF document as byte array.</returns>
        private byte[] CreateSinglePagePdf(List<BarcodeData> barcodes)
        {
            // Create the document
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Define page size based on your label dimensions and margins
                    page.Size((float)pageWidth, (float)pageHeight, Unit.Millimetre);
                    page.Margin(0);

                    // Content
                    page.Content().Element(contentContainer =>
                    {
                        CreateBarcodeLabelsForPdf(contentContainer, barcodes);
                    });

                    // No footer - removed to maximize space usage
                });
            });

            // Generate the PDF as a byte array
            return document.GeneratePdf();
        }

        /// <summary>
        /// Generate and save a PDF file with barcode labels.
        /// </summary>
        /// <param name="allBarcodes">All barcodes to include in the document.</param>
        /// <param name="outputFilePath">Where to save the PDF file.</param>
        /// <returns>True if successful, false otherwise.</returns>
        private bool GeneratePdfAndSaveToFile(List<BarcodeData> allBarcodes, string outputFilePath)
        {
            try
            {
                // Organize barcodes into pages
                var barcodePages = SplitBarcodesIntoPages(allBarcodes);

                // If we have more than one page, use multi-page method
                byte[] pdfBytes;
                if (barcodePages.Count > 1)
                {
                    pdfBytes = CreateMultiPagePdf(barcodePages);
                }
                else
                {
                    // Just one page, use simpler method
                    pdfBytes = CreateSinglePagePdf(allBarcodes);
                }

                // Save to file
                File.WriteAllBytes(outputFilePath, pdfBytes);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas generowania pliku PDF: {ex.Message}", "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
    #endregion
}
