using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionAPI.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        [Required]
        public string Status { get; set; } = string.Empty;
        [Required]
        public string Type { get; set; } = string.Empty;
        [Required]
        public string ClientName { get; set; } = string.Empty;
        [Required]
        public string Amount { get; set; } = string.Empty;
    }
}
