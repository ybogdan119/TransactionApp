using CsvHelper;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Globalization;
using TransactionAPI.Models;

namespace TransactionAPI.Services
{
    public class TransactionReaderService
    {
        public IEnumerable<Transaction>? ReadTransactions(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                return null;
            }

            string ext = Path.GetExtension(file.FileName);
            IEnumerable<Transaction> transactions;

            if (ext == ".xls" || ext == ".xlsx")
            {
                transactions = GetTrasnactionsFromExcel(file);
            }
            else if (ext == ".csv")
            {
                transactions = GetTrasnactionsFromCsv(file);
            }
            else
            {
                return null;
            }
            return transactions;
        }

        private IEnumerable<Transaction> GetTrasnactionsFromCsv(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            return csv.GetRecords<Transaction>().ToList();
        }

        private IEnumerable<Transaction> GetTrasnactionsFromExcel(IFormFile file)
        {
            var transactions = new List<Transaction>();

            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                var fileExtension = Path.GetExtension(file.FileName);

                using (var fs = new MemoryStream(memoryStream.ToArray()))
                {
                    IWorkbook workbook;
                    if (fileExtension == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(fs); // Для XLSX
                    }
                    else if (fileExtension == ".xls")
                    {
                        workbook = new HSSFWorkbook(fs); // Для XLS
                    }
                    else
                    {
                        // Обработка неподдерживаемых форматов
                        throw new Exception("Неподдерживаемый формат файла.");
                    }

                    ISheet sheet = workbook.GetSheetAt(0); // Выберите нужный лист

                    for (int row = 1; row <= sheet.LastRowNum; row++)
                    {
                        IRow currentRow = sheet.GetRow(row);

                        if (currentRow != null)
                        {
                            var transaction = new Transaction
                            {
                                TransactionId = Convert.ToInt32(currentRow.GetCell(0).ToString()),
                                Status = currentRow.GetCell(1).ToString(),
                                Type = currentRow.GetCell(2).ToString(),
                                ClientName = currentRow.GetCell(3).ToString(),
                                Amount = currentRow.GetCell(4).ToString()
                            };

                            transactions.Add(transaction);
                        }
                    }
                }
            }
            return transactions;
        }
    }
}

