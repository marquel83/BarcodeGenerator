using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BarcodeGenerator
{
    public partial class BarcodePreviewWindow : Window
    {
        private readonly List<string> barcodes; // Lista kodów kreskowych
        private readonly LabelTemplate template; // Szablon etykiety

        // Zamykanie aplikacji
        private void PreviewExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public BarcodePreviewWindow(List<string> barcodes, LabelTemplate template)
        {
            InitializeComponent();
            this.barcodes = barcodes ?? throw new ArgumentNullException(nameof(barcodes));
            this.template = template ?? throw new ArgumentNullException(nameof(template));
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            // Tworzenie kolumn w tabeli na podstawie wybranego szablonu
            for (int i = 0; i < template.Columns; i++)
            {
                BarcodesGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = $"Kolumna {i + 1}",
                    Binding = new System.Windows.Data.Binding($"[{i}]")
                });
            }

            // Przygotowanie danych do wyświetlenia w tabeli
            var rows = new List<string[]>();
            for (int i = 0; i < template.Rows; i++)
            {
                string[] row = new string[template.Columns];
                for (int j = 0; j < template.Columns; j++)
                {
                    int index = i * template.Columns + j;
                    if (index < barcodes.Count)
                    {
                        row[j] = barcodes[index];
                    }
                }
                rows.Add(row);
            }

            BarcodesGrid.ItemsSource = rows;
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
                GeneratePdf(saveFileDialog.FileName);
                MessageBox.Show("Plik PDF został zapisany.");
            }
        }

        private void GeneratePdf(string filename)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < template.Columns; i++)
                                columns.RelativeColumn();
                        });

                        int index = 0;
                        for (int row = 0; row < template.Rows; row++)
                        {
                            for (int col = 0; col < template.Columns; col++)
                            {
                                if (index >= barcodes.Count) break;

                                table.Cell().Element(CellStyle).AlignCenter().AlignMiddle().Text(barcodes[index]);
                                index++;
                            }
                        }
                    });
                });
            }).GeneratePdf(filename);
        }

        private IContainer CellStyle(IContainer container)
        {
            return container.Border(1).Padding(5);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(BarcodesGrid, "Etykiety z kodami kreskowymi");
                MessageBox.Show("Drukowanie zakończone.");
            }
        }
    }

    public class LabelTemplate
    {
        public string Name { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
    }
}
