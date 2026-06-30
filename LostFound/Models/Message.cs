using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostFound.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required, StringLength(2000)]
        public string Body { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public string SenderId { get; set; } = string.Empty;
        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        public string ReceiverId { get; set; } = string.Empty;
        [ForeignKey(nameof(ReceiverId))]
        public User? Receiver { get; set; }

        public int? ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public Item? Item { get; set; }
    }
}
