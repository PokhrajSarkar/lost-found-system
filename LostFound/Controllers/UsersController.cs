using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LostFound.Models;
using LostFound.Models.ViewModels;

namespace LostFound.Controllers
{
    public class UsersController : Controller
    {
        private readonly LostFoundContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _hostEnvironment;

        public UsersController(LostFoundContext context, UserManager<User> userManager,
            SignInManager<User> signInManager, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Users/Register
        public IActionResult Register() => View();

        // POST: Users/Register
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new User
            {
                FullName = vm.FullName,
                UserName = vm.Email,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(vm);
        }

        // GET: Users/Login
        public IActionResult Login() => View();

        // POST: Users/Login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            if (result.IsLockedOut)
                ModelState.AddModelError(string.Empty, "Account is locked.");
            else
                ModelState.AddModelError(string.Empty, "Invalid email or password.");

            return View(vm);
        }

        // POST: Users/Logout
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Users/Profile/id
        public async Task<IActionResult> Profile(string? id)
        {
            var user = id != null
                ? await _userManager.FindByIdAsync(id)
                : await _userManager.GetUserAsync(User);

            if (user == null) return NotFound();

            await _context.Entry(user).Collection(u => u.Items).LoadAsync();
            return View(user);
        }

        // GET: Users/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(new EditProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber
            });
        }

        // POST: Users/EditProfile
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.FullName = vm.FullName;
            user.PhoneNumber = vm.PhoneNumber;

            if (vm.AvatarFile != null && vm.AvatarFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(vm.AvatarFile.FileName);
                var uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "Uploads");
                Directory.CreateDirectory(uploadDir);
                var savePath = Path.Combine(uploadDir, fileName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await vm.AvatarFile.CopyToAsync(stream);
                user.AvatarPath = "/Uploads/" + fileName;
            }

            if (!string.IsNullOrEmpty(vm.NewPassword))
            {
                if (string.IsNullOrEmpty(vm.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(vm.CurrentPassword), "Current password is required.");
                    return View(vm);
                }
                var pwResult = await _userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
                if (!pwResult.Succeeded)
                {
                    foreach (var e in pwResult.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return View(vm);
                }
            }

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
