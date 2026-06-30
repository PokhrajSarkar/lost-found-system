using LostFound.Models;
using LostFound.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LostFoundContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(LostFoundContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalItems = await _context.Items.CountAsync(),
                ActiveItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Active),
                ResolvedItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Resolved),
                PendingApproval = await _context.Items.CountAsync(i => !i.IsApproved),
                TotalMessages = await _context.Messages.CountAsync(),
                RecentItems = await _context.Items.Include(i => i.User)
                    .OrderByDescending(i => i.DatePosted).Take(10).ToListAsync(),
                RecentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt).Take(10).ToListAsync()
            };
            return View(vm);
        }

        // GET: /Admin/Items
        public async Task<IActionResult> Items(string? search, bool pendingOnly = false)
        {
            var query = _context.Items.Include(i => i.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.Title.Contains(search) || i.Location.Contains(search));
            if (pendingOnly) query = query.Where(i => !i.IsApproved);
            ViewBag.Search = search;
            ViewBag.PendingOnly = pendingOnly;
            return View(await query.OrderByDescending(i => i.DatePosted).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();
            item.IsApproved = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{item.Title}' approved.";
            return RedirectToAction(nameof(Items));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Item rejected and removed.";
            return RedirectToAction(nameof(Items));
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users(string? search)
        {
            var users = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                users = users.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
            ViewBag.Search = search;
            return View(await users.OrderByDescending(u => u.CreatedAt).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["Success"] = $"'{user.FullName}' banned.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"'{user.FullName}' unbanned.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["Success"] = $"'{user.FullName}' is now an Admin.";
            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/Claims
        public async Task<IActionResult> Claims()
        {
            var claims = await _context.Claims
                .Include(c => c.Item).Include(c => c.Claimant)
                .OrderByDescending(c => c.SubmittedAt).ToListAsync();
            return View(claims);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int id)
        {
            var claim = await _context.Claims.Include(c => c.Item).FirstOrDefaultAsync(c => c.Id == id);
            if (claim == null) return NotFound();
            claim.Status = ClaimStatus.Approved;
            if (claim.Item != null) claim.Item.Status = ItemStatus.Resolved;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Claim approved and item resolved.";
            return RedirectToAction(nameof(Claims));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();
            claim.Status = ClaimStatus.Rejected;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Claim rejected.";
            return RedirectToAction(nameof(Claims));
        }
    }
}
