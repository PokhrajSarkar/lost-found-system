using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostFound.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required, StringLength(500)]
        public string Body { get; set; } = string.Empty;

        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        public int ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public Item? Item { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
