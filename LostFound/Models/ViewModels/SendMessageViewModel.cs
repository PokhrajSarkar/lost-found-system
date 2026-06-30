using System.ComponentModel.DataAnnotations;

namespace LostFound.Models.ViewModels
{
    public class SendMessageViewModel
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public int? ItemId { get; set; }
        public string? ItemTitle { get; set; }

        [Required, StringLength(2000)]
        public string Body { get; set; } = string.Empty;
    }
}
