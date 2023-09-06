using CsvHelper;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Globalization;
using TransactionAPI.Models;

namespace TransactionAPI.Services
{
    public class TransactionReaderService : ITransactionReaderService
    {
        public async Task<IEnumerable<Transaction>?> ReadTransactionsAsync(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                return null;
            }

            string ext = Path.GetExtension(file.FileName);
            IEnumerable<Transaction>? transactions = null;

            if (ext == ".xls" || ext == ".xlsx")
            {
                transactions = await GetTrasnactionsFromExcelAsync(file);
            }
            else if (ext == ".csv")
            {
                transactions = await GetTrasnactionsFromCsvAsync(file);
            }

            return transactions;
        }

        private Task<IEnumerable<Transaction>?> GetTrasnactionsFromCsvAsync(IFormFile file)
        {
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                return Task.FromResult<IEnumerable<Transaction>?>(csv.GetRecords<Transaction>().ToList());
            }
            catch (CsvHelperException)
            {
                return Task.FromResult<IEnumerable<Transaction>?>(null);
            }
        }

        private async Task<IEnumerable<Transaction>?> GetTrasnactionsFromExcelAsync(IFormFile file)
        {
            var transactions = new List<Transaction>();

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var fileExtension = Path.GetExtension(file.FileName);

                using (var fs = new MemoryStream(memoryStream.ToArray()))
                {
                    IWorkbook workbook;
                    if (fileExtension == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(fs);
                    }
                    else if (fileExtension == ".xls")
                    {
                        workbook = new HSSFWorkbook(fs);
                    }
                    else
                    {
                        throw new Exception("Unsupported media type.");
                    }

                    ISheet sheet = workbook.GetSheetAt(0);

                    try
                    {
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
                    catch (NullReferenceException)
                    {
                        return null;
                    }
                }
            }
            return transactions;
        }
    }
}

