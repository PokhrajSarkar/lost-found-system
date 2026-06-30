using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LostFound.Models;

namespace LostFound.Controllers
{
    public class ItemsController : Controller
    {
        private readonly LostFoundContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<User> _userManager;

        public ItemsController(LostFoundContext context, IWebHostEnvironment hostEnvironment, UserManager<User> userManager)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
        }

        // GET: Items
        public async Task<IActionResult> Index(string? searchString, string? category, string? type)
        {
            var items = _context.Items.Include(i => i.User).Where(i => i.IsApproved).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                items = items.Where(s => s.Title.Contains(searchString) || (s.Description != null && s.Description.Contains(searchString)));
            if (!string.IsNullOrEmpty(category))
                items = items.Where(i => i.Category == category);
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ItemType>(type, out var parsedType))
                items = items.Where(i => i.Type == parsedType);

            return View(await items.OrderByDescending(i => i.DatePosted).ToListAsync());
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.User)
                .Include(i => i.Images)
                .Include(i => i.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id.Value);

            if (item == null) return NotFound();

            ViewBag.SimilarItems = await _context.Items
                .Where(x => x.Category == item.Category && x.Id != item.Id && x.IsApproved)
                .Take(5).ToListAsync();

            return View(item);
        }

        // GET: Items/Create
        public IActionResult Create() => View();

        // POST: Items/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item, IFormFile? imageFile)
        {
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (!ModelState.IsValid) return View(item);

            item.UserId = _userManager.GetUserId(User)!;
            item.DatePosted = DateTime.UtcNow;
            item.IsApproved = true;

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "Uploads");
                Directory.CreateDirectory(uploadDir);
                var savePath = Path.Combine(uploadDir, fileName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                item.ImagePath = "/Uploads/" + fileName;
            }

            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: Items/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Item item, IFormFile? imageFile)
        {
            if (id != item.Id) return NotFound();

            ModelState.Remove("User");
            ModelState.Remove("ImagePath");

            if (!ModelState.IsValid) return View(item);

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    var uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "Uploads");
                    Directory.CreateDirectory(uploadDir);
                    var savePath = Path.Combine(uploadDir, fileName);
                    using var stream = new FileStream(savePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    item.ImagePath = "/Uploads/" + fileName;
                }
                else
                {
                    _context.Entry(item).Property(x => x.ImagePath).IsModified = false;
                }

                _context.Update(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(item.Id)) return NotFound();
                throw;
            }
        }

        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Items.Include(i => i.User).FirstOrDefaultAsync(m => m.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                // Clear partner link to avoid FK constraint
                if (item.RelatedItemId.HasValue)
                {
                    var partner = await _context.Items.FindAsync(item.RelatedItemId.Value);
                    if (partner != null)
                    {
                        partner.RelatedItemId = null;
                        partner.Status = ItemStatus.Active;
                    }
                }

                // Clear any items pointing back at this one
                var pointingBack = await _context.Items.Where(i => i.RelatedItemId == id).ToListAsync();
                foreach (var i in pointingBack) { i.RelatedItemId = null; i.Status = ItemStatus.Active; }

                // Remove related claims, comments, images, messages
                var claims = _context.Claims.Where(c => c.ItemId == id);
                _context.Claims.RemoveRange(claims);

                var comments = _context.Comments.Where(c => c.ItemId == id);
                _context.Comments.RemoveRange(comments);

                var images = _context.ItemImages.Where(img => img.ItemId == id);
                _context.ItemImages.RemoveRange(images);

                var messages = _context.Messages.Where(m => m.ItemId == id);
                foreach (var m in messages) m.ItemId = null;

                _context.Items.Remove(item);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Items/MyItems
        public async Task<IActionResult> MyItems()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.Items.Where(i => i.UserId == userId)
                .OrderByDescending(i => i.DatePosted).ToListAsync();
            return View(items);
        }

        // GET: Items/FindMatches/5
        public async Task<IActionResult> FindMatches(int? id)
        {
            if (id == null) return NotFound();
            var currentItem = await _context.Items.FindAsync(id);
            if (currentItem == null) return NotFound();

            var matches = await _context.Items
                .Where(i => i.Category == currentItem.Category
                         && i.Type != currentItem.Type
                         && i.Id != id
                         && i.RelatedItemId == null)
                .Include(i => i.User)
                .ToListAsync();

            ViewData["CurrentItem"] = currentItem;
            return View(matches);
        }

        // GET: Items/LinkItems?id=1&matchId=2
        public async Task<IActionResult> LinkItems(int id, int matchId)
        {
            var item1 = await _context.Items.FindAsync(id);
            var item2 = await _context.Items.FindAsync(matchId);

            if (item1 != null && item2 != null)
            {
                item1.RelatedItemId = matchId;
                item2.RelatedItemId = id;
                item1.Status = ItemStatus.Resolved;
                item2.Status = ItemStatus.Resolved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Items/Unmatch/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unmatch(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();

            if (item.RelatedItemId.HasValue)
            {
                var partner = await _context.Items.FindAsync(item.RelatedItemId.Value);
                if (partner != null)
                {
                    partner.RelatedItemId = null;
                    partner.Status = ItemStatus.Active;
                }
            }

            item.Status = ItemStatus.Active;
            item.RelatedItemId = null;
            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Items/SubmitClaim
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(int itemId, string description)
        {
            var claim = new Claim
            {
                ItemId = itemId,
                Description = description,
                ClaimantId = _userManager.GetUserId(User)!,
                SubmittedAt = DateTime.UtcNow
            };
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Claim submitted for review.";
            return RedirectToAction(nameof(Details), new { id = itemId });
        }

        // POST: Items/AddComment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int itemId, string body)
        {
            _context.Comments.Add(new Comment
            {
                ItemId = itemId,
                Body = body,
                UserId = _userManager.GetUserId(User)!,
                PostedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = itemId });
        }

        // POST: Items/DeleteComment/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                var itemId = comment.ItemId;
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = itemId });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Items/Category
        public async Task<IActionResult> Category(string? cat)
        {
            var items = _context.Items.Include(i => i.User).Where(i => i.IsApproved).AsQueryable();
            if (!string.IsNullOrEmpty(cat))
                items = items.Where(i => i.Category == cat);
            ViewBag.Category = cat;
            return View(await items.ToListAsync());
        }

        // GET: Items/Map
        public async Task<IActionResult> Map()
        {
            var items = await _context.Items
                .Where(i => i.IsApproved && i.Latitude.HasValue && i.Longitude.HasValue)
                .ToListAsync();
            return View(items);
        }

        // POST: Items/MarkResolved/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkResolved(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                item.Status = ItemStatus.Resolved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyItems));
        }

        private bool ItemExists(int id) => _context.Items.Any(e => e.Id == id);
    }
}
