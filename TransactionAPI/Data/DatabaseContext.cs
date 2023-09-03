using Microsoft.EntityFrameworkCore;
using TransactionAPI.Models;

namespace TransactionAPI.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Transaction> Transactions => Set<Transaction>();

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
            
        }
    }
}
