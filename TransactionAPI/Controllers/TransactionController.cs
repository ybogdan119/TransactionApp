using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TransactionAPI.Data;
using TransactionAPI.Models;
using TransactionAPI.Services;
using CsvHelper;
using System.Globalization;


namespace TransactionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private DatabaseContext _db;
        private ITransactionReaderService _transactionReader;

        public TransactionController(DatabaseContext context, ITransactionReaderService transactionReader)
        {
            _db = context;
            _transactionReader = transactionReader;
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet(Name = "GetAll")]
        public IAsyncEnumerable<Transaction> GetTransactions()
        {
            return _db.Transactions.AsAsyncEnumerable();
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id:int}", Name = "GetById")]
        public async Task<IActionResult> GetTransaction(int id)
        {
            var transaction = await _db.Transactions.FindAsync(id);

            if (transaction == null)
            {
                return NotFound();
            }
            return Ok(transaction);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("GetAllCsv", Name = "GetAllFilteredCsv")]
        public async Task<IActionResult> GetTransactionsCsv(string? typeFilter = null, string? statusFilter = null, string? clientFilter = null)
        {
            IQueryable<Transaction>? filteredQuery = _db.Transactions;

            typeFilter = typeFilter?.Replace(" ", "");
            typeFilter = typeFilter?.ToLower();
            string[]? typeFilterValues = typeFilter?.Split(',');

            if (typeFilterValues != null && typeFilterValues.Length > 0)
            {
                filteredQuery = filteredQuery.Where(t => typeFilterValues.Contains(t.Type.ToLower()));
            }
            if (statusFilter != null && statusFilter != string.Empty)
            {
                filteredQuery = filteredQuery.Where(t => t.Status.ToLower() == statusFilter.ToLower());
            }
            if (clientFilter != null && clientFilter != string.Empty)
            {
                filteredQuery = filteredQuery.Where(t => t.ClientName.ToLower() == clientFilter.ToLower());
            }

            var filteredList = await filteredQuery.ToListAsync();
            if(filteredList.Count == 0)
            {
                return NotFound();
            }

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.Headers.Add("Content-Disposition", "attachment; filename=exported_data.csv");
            Response.StatusCode = 200;
            await using (var textWriter = new StreamWriter(Response.Body, System.Text.Encoding.UTF8))
            await using (CsvWriter csvWriter = new CsvWriter(textWriter, CultureInfo.InvariantCulture))
            {
                await csvWriter.WriteRecordsAsync(filteredList);
            }

            return new EmptyResult();
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost]
        public async Task<IActionResult> PostTransaction(Transaction transaction)
        {
            await _db.Transactions.AddAsync(transaction);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id:int}", Name = "UpdateStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTransactionStatus(int id, [FromBody] string newStatus)
        {
            if (id <= 0 || newStatus == null)
            {
                return BadRequest();
            }

            var transactionToUpdate = await _db.Transactions.FindAsync(id);

            if (transactionToUpdate == null)
            {
                return NotFound();
            }

            transactionToUpdate.Status = newStatus;
            _db.Transactions.Update(transactionToUpdate);
            await _db.SaveChangesAsync();

            return Ok();
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpDelete(Name = "DeleteAll")]
        public async Task<IActionResult> DeleteTrasactions()
        {
            _db.Transactions.RemoveRange(_db.Transactions);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("{id:int}", Name = "DeleteById")]
        public async Task<IActionResult> DeleteTrasaction(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var transaction = await _db.Transactions.FindAsync(id);

            if (transaction == null)
            {
                return NotFound();
            }

            _db.Transactions.Remove(transaction);
            await _db.SaveChangesAsync();

            return Ok();
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            IEnumerable<Transaction>? transactions = await _transactionReader.ReadTransactionsAsync(file);
            if (transactions == null)
            {
                return BadRequest();
            }

            var zeroId = transactions.Where(t => t.TransactionId == 0);
            var nonZeroId = transactions.Where(t => t.TransactionId > 0);

            await _db.Database.OpenConnectionAsync();
            await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Transactions ON;");
            var ids = _db.Transactions.OrderBy(t => t.TransactionId).Select(t => t.TransactionId);
            if (ids.Any())
            {
                var update = nonZeroId.Where(t => ids.Contains(t.TransactionId));
                var add = nonZeroId.Where(t => !ids.Contains(t.TransactionId));
                _db.Transactions.UpdateRange(update);
                await _db.Transactions.AddRangeAsync(add);
            }
            else
            {
                await _db.Transactions.AddRangeAsync(transactions);
            }
            await _db.SaveChangesAsync();
            await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Transactions OFF");

            await _db.Transactions.AddRangeAsync(zeroId);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
