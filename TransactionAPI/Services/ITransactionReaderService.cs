using TransactionAPI.Models;

namespace TransactionAPI.Services
{
    public interface ITransactionReaderService
    {
        public Task<IEnumerable<Transaction>?> ReadTransactionsAsync(IFormFile file);
    }
}
