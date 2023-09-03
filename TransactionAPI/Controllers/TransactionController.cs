using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using System.Data;
using System.Globalization;
using TransactionAPI.Data;
using TransactionAPI.Models;
using TransactionAPI.Services;

namespace TransactionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private DatabaseContext _db;
        private TransactionReaderService _transactionReader;
        private TestService _ts;
        public TransactionController(DatabaseContext context, TransactionReaderService transactionReader, TestService ts)
        {
            _db = context;
            _transactionReader = transactionReader;
            _ts = ts;
        }

        [HttpGet]
        public IAsyncEnumerable<Transaction> GetTransactions()
        {
            return _db.Transactions.AsAsyncEnumerable();
        }
        [HttpPost]
        public async Task<IActionResult> PostTransaction(Transaction transaction)
        {
            await _db.Transactions.AddAsync(transaction);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [HttpPost("Test1")]
        public IActionResult Test1(IFormFile file)
        {
            _ts.transactions.Add(_transactionReader.ReadTransactions(file).ToList());
            return Ok();
        }
        [HttpGet("Test2")]
        public IActionResult Test2()
        {
            var v1 = _ts.transactions[0];
            var v2 = _ts.transactions[1];
            bool b = true;
            for (int i = 0; i < v1.Count(); i++)
            {
                b = v1[i].Status.Equals(v2[i].Status);
                if (b == false)
                    break;
                b = v1[i].TransactionId.Equals(v2[i].TransactionId);
                if (b == false)
                    break;
                b = v1[i].Amount.Equals(v2[i].Amount);
                if (b == false)
                    break;
                b = v1[i].ClientName.Equals(v2[i].ClientName);
                if (b == false)
                    break;
                b = v1[i].Type.Equals(v2[i].Type);
                if (b == false)
                    break;

            }
            return Ok(b);
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            IEnumerable<Transaction> transactions = _transactionReader.ReadTransactions(file);
            if (transactions == null)
            {
                return StatusCode(415);
            }

            // using (var transaction = _db.Database.BeginTransaction())
            //{
            var ids = _db.Transactions.Select(t => t.TransactionId).ToList();
            if (ids.Count > 0)
            {
                var update = transactions.Where(t => ids.Contains(t.TransactionId));
                var add = transactions.Where(t => !ids.Contains(t.TransactionId));
                _db.Transactions.UpdateRange(update);
                _db.Transactions.AddRange(add);
            }
            else
            {
                _db.Transactions.AddRange(transactions);
            }

            _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Transactions ON;");
            _db.SaveChanges();
            _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Transactions OFF");
            //transaction.Commit();
            //}
            return Ok();
        }

        private IEnumerable<Transaction> GetTrasnactionsFromCsv(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            return csv.GetRecords<Transaction>();
        }
        private IEnumerable<Transaction> GetTrasnactionsFromExcel(IFormFile file)
        {
            return null;
        }
    }
}
