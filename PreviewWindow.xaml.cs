using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BarcodeGenerator
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow()
        {
            InitializeComponent();
        }

        public void ShowPreview(double sheetWidth, double sheetHeight, int numberOfLabels,
            int numberOfColumns, double labelWidth, double labelHeight,
            double marginOX, double marginOY, double marginIX, double marginIY)
        {
            // Set the window size according to the sheet dimensions with some padding
            // Convert millimeters to pixels (1mm ≈ 3.779528 pixels)
            const double MM_TO_PIXELS = 3.779528;

            // Set window size with some padding
            this.Width = (sheetWidth * MM_TO_PIXELS) + 40;  // 20px padding on each side
            this.Height = (sheetHeight * MM_TO_PIXELS) + 40; // 20px padding on each side

            PreviewGrid.Children.Clear();
            PreviewGrid.ColumnDefinitions.Clear();
            PreviewGrid.RowDefinitions.Clear();

            // Calculate number of rows
            int numberOfRows = (int)Math.Ceiling((double)numberOfLabels / numberOfColumns);

            // Create container for the sheet
            Border sheetBorder = new Border
            {
                Width = sheetWidth * MM_TO_PIXELS,
                Height = sheetHeight * MM_TO_PIXELS,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };

            // Create grid for labels
            Grid labelsGrid = new Grid
            {
                Margin = new Thickness(marginOX * MM_TO_PIXELS)
            };

            // Set up the grid columns and rows
            for (int i = 0; i < numberOfColumns; i++)
            {
                labelsGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(labelWidth * MM_TO_PIXELS)
                });

                // Add column spacing except for the last column
                if (i < numberOfColumns - 1)
                {
                    labelsGrid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(marginIX * MM_TO_PIXELS)
                    });
                }
            }

            for (int i = 0; i < numberOfRows; i++)
            {
                labelsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(labelHeight * MM_TO_PIXELS)
                });

                // Add row spacing except for the last row
                if (i < numberOfRows - 1)
                {
                    labelsGrid.RowDefinitions.Add(new RowDefinition
                    {
                        Height = new GridLength(marginIY * MM_TO_PIXELS)
                    });
                }
            }

            // Add labels to the grid
            int labelCounter = 0;
            for (int row = 0; row < numberOfRows; row++)
            {
                for (int col = 0; col < numberOfColumns; col++)
                {
                    if (labelCounter >= numberOfLabels) break;

                    var rect = new Rectangle
                    {
                        Width = labelWidth * MM_TO_PIXELS,
                        Height = labelHeight * MM_TO_PIXELS,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Fill = Brushes.WhiteSmoke // Light background to make labels visible
                    };

                    // Add label number for reference
                    var textBlock = new TextBlock
                    {
                        Text = (labelCounter + 1).ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var labelContainer = new Grid();
                    labelContainer.Children.Add(rect);
                    labelContainer.Children.Add(textBlock);

                    Grid.SetRow(labelContainer, row * 2); // Multiply by 2 to account for spacing rows
                    Grid.SetColumn(labelContainer, col * 2); // Multiply by 2 to account for spacing columns

                    labelsGrid.Children.Add(labelContainer);
                    labelCounter++;
                }
            }

            // Add the labels grid to the sheet
            sheetBorder.Child = labelsGrid;

            // Center the sheet in the window
            PreviewGrid.Children.Add(sheetBorder);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove(); // Przesuwa okno, gdy lewy przycisk myszy jest wciśnięty
            }
        }

        private void PreviewExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}