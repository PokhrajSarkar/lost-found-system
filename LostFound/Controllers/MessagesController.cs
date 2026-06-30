using LostFound.Models;
using LostFound.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly LostFoundContext _context;
        private readonly UserManager<User> _userManager;

        public MessagesController(LostFoundContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Messages
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var messages = await _context.Messages
                .Include(m => m.Sender).Include(m => m.Receiver).Include(m => m.Item)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.SentAt).ToListAsync();

            var conversations = messages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new ConversationSummary
                {
                    OtherUserId = g.Key,
                    OtherUser = g.First().SenderId == userId ? g.First().Receiver! : g.First().Sender!,
                    LastMessage = g.First(),
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                }).ToList();

            return View(conversations);
        }

        // GET: /Messages/Thread/userId
        public async Task<IActionResult> Thread(string id)
        {
            var myId = _userManager.GetUserId(User)!;
            var other = await _userManager.FindByIdAsync(id);
            if (other == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender).Include(m => m.Item)
                .Where(m => (m.SenderId == myId && m.ReceiverId == id) ||
                            (m.SenderId == id && m.ReceiverId == myId))
                .OrderBy(m => m.SentAt).ToListAsync();

            var unread = messages.Where(m => m.ReceiverId == myId && !m.IsRead).ToList();
            unread.ForEach(m => m.IsRead = true);
            if (unread.Any()) await _context.SaveChangesAsync();

            ViewBag.OtherUser = other;
            ViewBag.MyId = myId;
            return View(messages);
        }

        // GET: /Messages/Compose?receiverId=&itemId=
        public async Task<IActionResult> Compose(string receiverId, int? itemId)
        {
            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null) return NotFound();
            Item? item = itemId.HasValue ? await _context.Items.FindAsync(itemId.Value) : null;
            return View(new SendMessageViewModel
            {
                ReceiverId = receiverId,
                ReceiverName = receiver.FullName,
                ItemId = itemId,
                ItemTitle = item?.Title
            });
        }

        // POST: /Messages/Send
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(SendMessageViewModel vm)
        {
            if (!ModelState.IsValid) return View("Compose", vm);
            _context.Messages.Add(new Message
            {
                SenderId = _userManager.GetUserId(User)!,
                ReceiverId = vm.ReceiverId,
                Body = vm.Body,
                ItemId = vm.ItemId,
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Message sent.";
            return RedirectToAction(nameof(Thread), new { id = vm.ReceiverId });
        }

        // GET: /Messages/UnreadCount (AJAX)
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User)!;
            var count = await _context.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead);
            return Json(count);
        }
    }

    public class ConversationSummary
    {
        public string OtherUserId { get; set; } = string.Empty;
        public User OtherUser { get; set; } = null!;
        public Message LastMessage { get; set; } = null!;
        public int UnreadCount { get; set; }
    }
}
