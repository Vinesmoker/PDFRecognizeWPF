using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PDFRecognizeWPF
{
    public partial class MainWindow : Window
    {
        private bool _isOpenFileDialogShown = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SelectPdfButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_isOpenFileDialogShown)
            {
                _isOpenFileDialogShown = true;
                string pdfFilePath = await GetFilePathAsync("Выберите PDF-файл через проводник:");
                pdfFilePathTextBox.Text = pdfFilePath;
                _isOpenFileDialogShown = false;
            }
        }

        private async void SelectDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_isOpenFileDialogShown)
            {
                _isOpenFileDialogShown = true;
                string outputDirectory = await GetFolderPathAsync("Выберите каталог для сохранения файлов:");
                outputDirectoryTextBox.Text = outputDirectory;
                _isOpenFileDialogShown = false;
            }
        }

        private void ExtractTextButtonClick(object sender, RoutedEventArgs e)
        {
            string pdfFilePath = this.pdfFilePathTextBox.Text;
            string outputDirectory = this.outputDirectoryTextBox.Text;

            if (File.Exists(pdfFilePath) && !string.IsNullOrEmpty(outputDirectory))
            {
                string pdfText = ExtractTextFromPdf(pdfFilePath);
                this.resultTextBlock.Text = pdfText;

                string worksText = ExtractTableData(pdfText, "Выполненные работы по заказ-наряду", "Итого работ", new[] { 0, 2, 5, 7 });
                string partsText = ExtractTableData(pdfText, "Расходная накладная к заказ-наряду", "Итого материалов", new[] { 0, 2, 3, 7 });

                SaveTextToFile(Path.Combine(outputDirectory, "работы.txt"), worksText);
                SaveTextToFile(Path.Combine(outputDirectory, "запчасти.txt"), partsText);

                System.Windows.MessageBox.Show("Извлечение завершено.", "Успех");
            }
            else
            {
                System.Windows.MessageBox.Show("Выберите корректный PDF-файл и каталог.", "Ошибка");
            }
        }

        private string ExtractTableData(string text, string startPattern, string endPattern, int[] columns)
        {
            var startIndex = text.IndexOf(startPattern);
            if (startIndex != -1)
            {
                // Находим конец таблицы (может быть уточнено)
                var endIndex = text.IndexOf(endPattern, startIndex + startPattern.Length);

                // Проверяем, что есть строки данных
                if (endIndex > 0 && endIndex > startIndex + startPattern.Length)
                {
                    // Извлекаем текст между началом и концом таблицы
                    var tableText = text.Substring(startIndex + startPattern.Length, endIndex - startIndex - startPattern.Length).Trim();

                    // Разделяем строки таблицы
                    var lines = tableText.Split('\n');

                    // Извлекаем строки с данными таблицы (без пустых строк)
                    var dataLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

                    // Соединяем извлеченные строки в текст
                    return string.Join("\n", dataLines);
                }
            }
            return "";
        }

        private void SaveTextToFile(string filePath, string text)
        {
            File.WriteAllText(filePath, text);
        }

        private string ExtractTextFromPdf(string pdfFilePath)
        {
            using (var pdfReader = new PdfReader(pdfFilePath))
            {
                using (var pdfDocument = new PdfDocument(pdfReader))
                {
                    var text = "";
                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                    {
                        var page = pdfDocument.GetPage(i);
                        var strategy = new LocationTextExtractionStrategy();
                        var currentText = PdfTextExtractor.GetTextFromPage(page, strategy);
                        text += currentText;
                    }
                    return text;
                }
            }
        }

        private async Task<string> GetFilePathAsync(string prompt)
        {
            Console.WriteLine(prompt);

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";

            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                return await Task.FromResult(openFileDialog.FileName);
            }
            else
            {
                Console.WriteLine("Отменено. Завершение программы.");
                Environment.Exit(0);
                return await Task.FromResult<string>(null);
            }
        }

        private async Task<string> GetFolderPathAsync(string prompt)
        {
            Console.WriteLine(prompt);

            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return await Task.FromResult(folderBrowserDialog.SelectedPath);
            }
            else
            {
                Console.WriteLine("Отменено. Завершение программы.");
                Environment.Exit(0);
                return await Task.FromResult<string>(null);
            }
        }
    }
}
