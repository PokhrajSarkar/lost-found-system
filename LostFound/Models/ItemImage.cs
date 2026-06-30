using System.ComponentModel.DataAnnotations.Schema;

namespace LostFound.Models
{
    public class ItemImage
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public Item? Item { get; set; }
    }
}
