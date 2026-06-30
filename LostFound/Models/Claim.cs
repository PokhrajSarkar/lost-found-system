using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostFound.Models
{
    public enum ClaimStatus { Pending, Approved, Rejected }

    public class Claim
    {
        public int Id { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public int ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public Item? Item { get; set; }

        public string ClaimantId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClaimantId))]
        public User? Claimant { get; set; }
    }
}
