using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Windows.Input;

namespace BarcodeGenerator
{
    public partial class ResultWindow : Window
    {
        private const double MM_TO_PIXELS = 3.779528;
        private List<Grid> pages;
        private int currentPageIndex;

        // Template dimensions
        private double templateWidth;
        private double templateHeight;
        private double marginOX;
        private double marginOY;
        private double marginIX;
        private double marginIY;
        private double labelWidth;
        private double labelHeight;
        private int numberOfLabels;
        private int numberOfColumns;

        public ResultWindow()
        {
            InitializeComponent();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            pages = new List<Grid>();
            currentPageIndex = 0;

            // Initialize navigation buttons
            previouspageBtn.IsEnabled = false;
            nextpageBtn.IsEnabled = false;

            // Add click handlers for navigation buttons
            previouspageBtn.Click += (s, e) => NavigatePage(-1);
            nextpageBtn.Click += (s, e) => NavigatePage(1);
        }

        public void DisplayBarcodes(List<BarcodeData> barcodes, int columns, double labelWidthParam, double labelHeightParam, string barcodeType)
        {
            if (barcodes == null || barcodeType == null)
            {
                MessageBox.Show("Invalid barcode data or type.");
                return;
            }

            // Load template settings directly
            templateWidth = double.Parse(Properties.Settings.Default.SheetWidth);
            templateHeight = double.Parse(Properties.Settings.Default.SheetHeight);
            marginOX = double.Parse(Properties.Settings.Default.MarginOX);
            marginOY = double.Parse(Properties.Settings.Default.MarginOY);
            marginIX = double.Parse(Properties.Settings.Default.MarginIX);
            marginIY = double.Parse(Properties.Settings.Default.MarginIY);
            labelWidth = labelWidthParam;
            labelHeight = labelHeightParam;
            numberOfLabels = int.Parse(Properties.Settings.Default.NumberOfLabels);

            // Use the number of columns exactly as defined in the template
            numberOfColumns = columns; // This is the key fix - ensure we use the columns from template

            // Set window size according to sheet dimensions with padding
            this.Width = (templateWidth * MM_TO_PIXELS) + 40;
            this.Height = (templateHeight * MM_TO_PIXELS) + 40;

            int totalPages = (int)Math.Ceiling((double)barcodes.Count / numberOfLabels);
            pages.Clear();

            for (int pageNum = 0; pageNum < totalPages; pageNum++)
            {
                Grid pageGrid = new Grid
                {
                    Width = templateWidth * MM_TO_PIXELS,
                    Height = templateHeight * MM_TO_PIXELS,
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                int startIdx = pageNum * numberOfLabels;
                int endIdx = Math.Min((pageNum + 1) * numberOfLabels, barcodes.Count);
                var pageBarcodes = barcodes.Skip(startIdx).Take(endIdx - startIdx).ToList();

                CreateLabelsGrid(pageGrid, pageBarcodes, barcodeType);

                pages.Add(pageGrid);
            }

            ShowCurrentPage();
            UpdateNavigationButtons();
        }

        private void CreateLabelsGrid(Grid pageGrid, List<BarcodeData> barcodes, string barcodeType)
        {
            Grid contentGrid = new Grid
            {
                Margin = new Thickness(marginOX * MM_TO_PIXELS, marginOY * MM_TO_PIXELS,
                                       marginOX * MM_TO_PIXELS, marginOY * MM_TO_PIXELS)
            };

            // Important: Calculate rows based on the exact number of columns from template
            // and the number of barcodes for this page
            int numberOfRows = (int)Math.Ceiling((double)barcodes.Count / numberOfColumns);

            contentGrid.ColumnDefinitions.Clear();
            contentGrid.RowDefinitions.Clear();

            // Create columns with exact sizing - use the template's column count
            for (int i = 0; i < numberOfColumns; i++)
            {
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(labelWidth * MM_TO_PIXELS)
                });

                if (i < numberOfColumns - 1)
                {
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(marginIX * MM_TO_PIXELS)
                    });
                }
            }

            // Create rows based on the calculated number needed
            for (int i = 0; i < numberOfRows; i++)
            {
                contentGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(labelHeight * MM_TO_PIXELS)
                });

                if (i < numberOfRows - 1)
                {
                    contentGrid.RowDefinitions.Add(new RowDefinition
                    {
                        Height = new GridLength(marginIY * MM_TO_PIXELS)
                    });
                }
            }

            // Place barcodes in grid using template column count for layout
            int barcodeIndex = 0;
            for (int row = 0; row < numberOfRows && barcodeIndex < barcodes.Count; row++)
            {
                for (int col = 0; col < numberOfColumns && barcodeIndex < barcodes.Count; col++)
                {
                    Border labelContainer = new Border
                    {
                        Width = labelWidth * MM_TO_PIXELS,
                        Height = labelHeight * MM_TO_PIXELS,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0.5)
                    };

                    StackPanel panel = new StackPanel
                    {
                        Orientation = Orientation.Vertical
                    };

                    double imageHeight = (barcodeType == "QR_CODE") ?
                        Math.Min(labelWidth, labelHeight) * MM_TO_PIXELS * 0.8 :
                        labelHeight * MM_TO_PIXELS * 0.7;

                    Image barcodeImage = new Image
                    {
                        Source = barcodes[barcodeIndex].Image,
                        Height = imageHeight,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 0)
                    };

                    panel.Children.Add(barcodeImage);

                    if (labelHeight >= 15)
                    {
                        TextBlock codeText = new TextBlock
                        {
                            Text = barcodes[barcodeIndex].Value,
                            TextAlignment = TextAlignment.Center,
                            FontSize = 8,
                            Margin = new Thickness(0, 1, 0, 2)
                        };
                        panel.Children.Add(codeText);
                    }

                    labelContainer.Child = panel;

                    Grid.SetRow(labelContainer, row * 2); // Account for spacing rows
                    Grid.SetColumn(labelContainer, col * 2); // Account for spacing columns
                    contentGrid.Children.Add(labelContainer);

                    barcodeIndex++;
                }
            }

            pageGrid.Children.Add(contentGrid);
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
                            // Render the grid to bitmap
                            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                                (int)(templateWidth * MM_TO_PIXELS),
                                (int)(templateHeight * MM_TO_PIXELS),
                                96, 96, PixelFormats.Pbgra32);

                            renderBitmap.Render(pageGrid);

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
                    ticket.PageMediaSize = new System.Printing.PageMediaSize(
                        templateWidth * 3.779528,  // Convert mm to hundredths of an inch
                        templateHeight * 3.779528
                    );

                    // Print each page
                    foreach (var pageGrid in pages)
                    {
                        printDialog.PrintVisual(pageGrid, $"Barcode Labels - Page {pages.IndexOf(pageGrid) + 1}");
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void PreviewExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}