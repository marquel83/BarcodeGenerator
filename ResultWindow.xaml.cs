using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Printing;
using System.Windows.Input;

namespace BarcodeGenerator
{
    public partial class ResultWindow : Window
    {
        private const double MM_TO_PIXELS = 3.779528;
        private List<Grid> pages;
        private int currentPageIndex;
        private double templateWidth;
        private double templateHeight;

        public ResultWindow()
        {
            InitializeComponent();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            pages = new List<Grid>();
            currentPageIndex = 0;

            templateWidth = double.Parse(Properties.Settings.Default.SheetWidth);
            templateHeight = double.Parse(Properties.Settings.Default.SheetHeight);

            // Initialize navigation buttons
            previouspageBtn.IsEnabled = false;
            nextpageBtn.IsEnabled = false;

            // Add click handlers for navigation buttons
            previouspageBtn.Click += (s, e) => NavigatePage(-1);
            nextpageBtn.Click += (s, e) => NavigatePage(1);
        }

        public void DisplayBarcodes(List<BarcodeData> barcodes, int columns, double labelWidth, double labelHeight, string barcodeType)
        {
            if (barcodes == null || barcodeType == null)
            {
                MessageBox.Show("Invalid barcode data or type.");
                return;
            }

            // Get template settings
            double marginOX = double.Parse(Properties.Settings.Default.MarginOX);
            double marginOY = double.Parse(Properties.Settings.Default.MarginOY);
            double marginIX = double.Parse(Properties.Settings.Default.MarginIX);
            double marginIY = double.Parse(Properties.Settings.Default.MarginIY);
            int labelsPerPage = int.Parse(Properties.Settings.Default.NumberOfLabels);

            // Set window size according to sheet dimensions with padding
            this.Width = (templateWidth * MM_TO_PIXELS) + 40;
            this.Height = (templateHeight * MM_TO_PIXELS) + 40;

            // Calculate number of pages needed
            int totalPages = (int)Math.Ceiling((double)barcodes.Count / labelsPerPage);
            pages.Clear();

            for (int pageNum = 0; pageNum < totalPages; pageNum++)
            {
                Grid pageGrid = CreatePageGrid(marginOX, marginOY);

                int startIdx = pageNum * labelsPerPage;
                int endIdx = Math.Min((pageNum + 1) * labelsPerPage, barcodes.Count);
                var pageBarcodes = barcodes.Skip(startIdx).Take(endIdx - startIdx).ToList();

                PopulatePageWithBarcodes(pageGrid, pageBarcodes, columns, labelWidth, labelHeight,
                    marginIX, marginIY, barcodeType);

                pages.Add(pageGrid);
            }

            // Show first page
            ShowCurrentPage();

            // Update navigation buttons
            UpdateNavigationButtons();
        }

        private Grid CreatePageGrid(double marginOX, double marginOY)
        {
            var grid = new Grid
            {
                Width = templateWidth * MM_TO_PIXELS,
                Height = templateHeight * MM_TO_PIXELS,
                Background = Brushes.White
            };

            grid.Margin = new Thickness(
                marginOX * MM_TO_PIXELS,
                marginOY * MM_TO_PIXELS,
                marginOX * MM_TO_PIXELS,
                marginOY * MM_TO_PIXELS
            );

            return grid;
        }

        private void PopulatePageWithBarcodes(Grid pageGrid, List<BarcodeData> barcodes,
            int columns, double labelWidth, double labelHeight, double marginIX, double marginIY,
            string barcodeType)
        {
            int rows = (int)Math.Ceiling((double)barcodes.Count / columns);

            pageGrid.Children.Clear();
            pageGrid.RowDefinitions.Clear();
            pageGrid.ColumnDefinitions.Clear();

            // Add columns with spacing
            for (int i = 0; i < columns; i++)
            {
                pageGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(labelWidth * MM_TO_PIXELS)
                });
                if (i < columns - 1)
                {
                    pageGrid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(marginIX * MM_TO_PIXELS)
                    });
                }
            }

            // Add rows with spacing
            for (int i = 0; i < rows; i++)
            {
                pageGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(labelHeight * MM_TO_PIXELS)
                });
                if (i < rows - 1)
                {
                    pageGrid.RowDefinitions.Add(new RowDefinition
                    {
                        Height = new GridLength(marginIY * MM_TO_PIXELS)
                    });
                }
            }

            // Add barcodes to grid
            int barcodeIndex = 0;
            bool isQRCode = barcodeType == "QR_CODE";

            for (int i = 0; i < rows && barcodeIndex < barcodes.Count; i++)
            {
                for (int j = 0; j < columns && barcodeIndex < barcodes.Count; j++)
                {
                    var labelContent = new Grid
                    {
                        Width = labelWidth * MM_TO_PIXELS,
                        Height = labelHeight * MM_TO_PIXELS
                    };

                    // Calculate proportional image size
                    double imageWidth = labelWidth * MM_TO_PIXELS * 0.9;
                    double imageHeight = isQRCode ?
                        Math.Min(imageWidth, labelHeight * MM_TO_PIXELS * 0.9) :
                        labelHeight * MM_TO_PIXELS * 0.9;

                    var barcodeImage = new Image
                    {
                        Source = barcodes[barcodeIndex].Image,
                        Width = imageWidth,
                        Height = imageHeight,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    labelContent.Children.Add(barcodeImage);

                    Grid.SetRow(labelContent, i * 2);
                    Grid.SetColumn(labelContent, j * 2);
                    pageGrid.Children.Add(labelContent);

                    barcodeIndex++;
                }
            }
        }

        private void NavigatePage(int direction)
        {
            currentPageIndex = Math.Max(0, Math.Min(pages.Count - 1, currentPageIndex + direction));
            ShowCurrentPage();
            UpdateNavigationButtons();
        }

        private void ShowCurrentPage()
        {
            BarcodesGrid.Children.Clear();
            BarcodesGrid.Children.Add(pages[currentPageIndex]);
        }

        private void UpdateNavigationButtons()
        {
            previouspageBtn.IsEnabled = currentPageIndex > 0;
            nextpageBtn.IsEnabled = currentPageIndex < pages.Count - 1;
        }

        private void GeneratePdf(string filename)
        {
            try
            {
                using (PdfDocument document = new PdfDocument())
                {
                    foreach (var pageGrid in pages)
                    {
                        PdfPage page = document.AddPage();

                        // Set exact page size from template
                        page.Width = XUnit.FromMillimeter(templateWidth);
                        page.Height = XUnit.FromMillimeter(templateHeight);

                        using (XGraphics gfx = XGraphics.FromPdfPage(page))
                        {
                            // Create a temporary grid with exact template dimensions
                            var tempGrid = new Grid
                            {
                                Width = templateWidth * MM_TO_PIXELS,
                                Height = templateHeight * MM_TO_PIXELS,
                                Background = Brushes.White
                            };

                            // Copy all the content and properties from the original page
                            foreach (var child in pageGrid.Children)
                            {
                                if (child is Grid labelGrid)
                                {
                                    var newLabelGrid = new Grid
                                    {
                                        Width = labelGrid.Width,
                                        Height = labelGrid.Height,
                                        Margin = labelGrid.Margin
                                    };

                                    // Copy column and row definitions
                                    foreach (var col in pageGrid.ColumnDefinitions)
                                        newLabelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = col.Width });
                                    foreach (var row in pageGrid.RowDefinitions)
                                        newLabelGrid.RowDefinitions.Add(new RowDefinition { Height = row.Height });

                                    // Copy the image with its properties
                                    if (labelGrid.Children.Count > 0 && labelGrid.Children[0] is Image originalImage)
                                    {
                                        var newImage = new Image
                                        {
                                            Source = originalImage.Source,
                                            Width = originalImage.Width,
                                            Height = originalImage.Height,
                                            Stretch = originalImage.Stretch,
                                            VerticalAlignment = originalImage.VerticalAlignment,
                                            HorizontalAlignment = originalImage.HorizontalAlignment
                                        };
                                        newLabelGrid.Children.Add(newImage);
                                    }

                                    Grid.SetRow(newLabelGrid, Grid.GetRow(labelGrid));
                                    Grid.SetColumn(newLabelGrid, Grid.GetColumn(labelGrid));
                                    tempGrid.Children.Add(newLabelGrid);
                                }
                            }

                            // Copy grid definitions from original page
                            foreach (var col in pageGrid.ColumnDefinitions)
                                tempGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = col.Width });
                            foreach (var row in pageGrid.RowDefinitions)
                                tempGrid.RowDefinitions.Add(new RowDefinition { Height = row.Height });

                            // Measure and arrange the temporary grid
                            tempGrid.Measure(new Size(templateWidth * MM_TO_PIXELS, templateHeight * MM_TO_PIXELS));
                            tempGrid.Arrange(new Rect(0, 0, templateWidth * MM_TO_PIXELS, templateHeight * MM_TO_PIXELS));

                            // Render the grid to bitmap
                            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                                (int)(templateWidth * MM_TO_PIXELS),
                                (int)(templateHeight * MM_TO_PIXELS),
                                96, 96, PixelFormats.Pbgra32);
                            renderBitmap.Render(tempGrid);

                            // Convert to PDF
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                            using (var stream = new MemoryStream())
                            {
                                encoder.Save(stream);
                                using (var imageStream = new MemoryStream(stream.ToArray()))
                                {
                                    XImage image = XImage.FromStream(() => imageStream);
                                    // Draw the image at the exact size of the PDF page
                                    gfx.DrawImage(image, 0, 0, page.Width, page.Height);
                                }
                            }
                        }
                    }

                    document.Save(filename);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating PDF: {ex.Message}");
                throw;
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                try
                {
                    // Configure print ticket for template size
                    var ticket = printDialog.PrintTicket;
                    ticket.PageMediaSize = new PageMediaSize(
                        templateWidth * 3.779528,  // Convert mm to hundredths of an inch
                        templateHeight * 3.779528
                    );
                    ticket.PageOrientation = templateWidth > templateHeight ?
                        PageOrientation.Landscape : PageOrientation.Portrait;

                    foreach (var pageGrid in pages)
                    {
                        // Create a temporary grid with exact template dimensions
                        var tempGrid = new Grid
                        {
                            Width = templateWidth * MM_TO_PIXELS,
                            Height = templateHeight * MM_TO_PIXELS,
                            Background = Brushes.White
                        };

                        // Copy all column and row definitions
                        foreach (var col in pageGrid.ColumnDefinitions)
                            tempGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = col.Width });
                        foreach (var row in pageGrid.RowDefinitions)
                            tempGrid.RowDefinitions.Add(new RowDefinition { Height = row.Height });

                        // Copy all content with exact positioning
                        foreach (var child in pageGrid.Children)
                        {
                            if (child is Grid labelGrid)
                            {
                                var newLabelGrid = new Grid
                                {
                                    Width = labelGrid.Width,
                                    Height = labelGrid.Height,
                                    Margin = labelGrid.Margin
                                };

                                // Copy the image with its properties
                                if (labelGrid.Children.Count > 0 && labelGrid.Children[0] is Image originalImage)
                                {
                                    var newImage = new Image
                                    {
                                        Source = originalImage.Source,
                                        Width = originalImage.Width,
                                        Height = originalImage.Height,
                                        Stretch = originalImage.Stretch,
                                        VerticalAlignment = originalImage.VerticalAlignment,
                                        HorizontalAlignment = originalImage.HorizontalAlignment
                                    };
                                    newLabelGrid.Children.Add(newImage);
                                }

                                Grid.SetRow(newLabelGrid, Grid.GetRow(labelGrid));
                                Grid.SetColumn(newLabelGrid, Grid.GetColumn(labelGrid));
                                tempGrid.Children.Add(newLabelGrid);
                            }
                        }

                        // Measure and arrange the grid at exact template size
                        tempGrid.Measure(new Size(templateWidth * MM_TO_PIXELS, templateHeight * MM_TO_PIXELS));
                        tempGrid.Arrange(new Rect(0, 0, templateWidth * MM_TO_PIXELS, templateHeight * MM_TO_PIXELS));

                        // Print the page
                        printDialog.PrintVisual(tempGrid, $"Barcode Labels - Page {pages.IndexOf(pageGrid) + 1}");
                    }

                    MessageBox.Show($"Successfully printed {pages.Count} page(s).");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during printing: {ex.Message}");
                }
            }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                DefaultExt = "pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    GeneratePdf(saveFileDialog.FileName);
                    MessageBox.Show("PDF file has been saved successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during PDF generation: {ex.Message}");
                }
            }
        }

        private UIElement CloneGridContent(UIElement original)
        {
            if (original is Grid grid)
            {
                var newGrid = new Grid
                {
                    Width = grid.Width,
                    Height = grid.Height,
                    Margin = grid.Margin
                };

                foreach (UIElement child in grid.Children)
                {
                    if (child is Image img)
                    {
                        var newImg = new Image
                        {
                            Source = img.Source,
                            Width = img.Width,
                            Height = img.Height,
                            Stretch = img.Stretch,
                            VerticalAlignment = img.VerticalAlignment,
                            HorizontalAlignment = img.HorizontalAlignment
                        };
                        Grid.SetRow(newImg, Grid.GetRow(img));
                        Grid.SetColumn(newImg, Grid.GetColumn(img));
                        newGrid.Children.Add(newImg);
                    }
                }

                Grid.SetRow(newGrid, Grid.GetRow(grid));
                Grid.SetColumn(newGrid, Grid.GetColumn(grid));
                return newGrid;
            }
            return original;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove(); // Przesuwa okno, gdy lewy przycisk myszy jest wciœniêty
            }
        }

        private void PreviewExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}