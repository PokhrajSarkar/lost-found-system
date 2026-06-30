using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostFound.Models
{
    public enum ItemType { Lost, Found }
    public enum ItemStatus { Active, Resolved, Expired }

    public class Item
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public ItemType Type { get; set; }

        public ItemStatus Status { get; set; } = ItemStatus.Active;

        public bool IsApproved { get; set; } = true;

        [StringLength(255)]
        public string? Location { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Display(Name = "Date Occurred")]
        public DateTime DateOccurred { get; set; } = DateTime.UtcNow;

        [Display(Name = "Date Posted")]
        public DateTime DatePosted { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        [StringLength(200)]
        public string? ContactInfo { get; set; }

        public string? ImagePath { get; set; }

        public int? RelatedItemId { get; set; }

        [Required(ErrorMessage = "Please select a user.")]
        [Display(Name = "Reported By")]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        public virtual ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
