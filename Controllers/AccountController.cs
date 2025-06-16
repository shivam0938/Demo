using Demo.Data;
using Demo.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers
{
    public class AccountController : Controller
    {
        protected readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View(new User());
        }
        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(x => x.Email == model.Email);
                if (user != null)
                    ModelState.AddModelError("Email", "User email already exist");
                else
                {
                    await _context.Users.AddAsync(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Login");
                }
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(UserLogin model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == model.Email && x.Password == model.Password);
                if (user is null)
                    ModelState.AddModelError(string.Empty, "user login invalid");
                else
                {
                    var claims = new List<Claim>
                        {
                            new (ClaimTypes.Email, user.Email),
                            new (ClaimTypes.Name, user.Id.ToString()) // Using email as username
                        };

                    // Add role based on BecomeAsSeller
                    if (user.BecomeAsSeller)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "User"));
                    }

                    var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(new ClaimsPrincipal(claimIdentity));

                    return RedirectToAction("Index", "ProductDetails");
                }
            }
            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "ProductDetails");
        }

        public async Task<IActionResult> ChangeProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user  = await _context.Users.FirstOrDefaultAsync(x => x.Email == userEmail);
            var userprofile = new ChangeProfile();
            if (user != null)
            {
                userprofile.Id = user.Id;
                userprofile.Email= user.Email;
                userprofile.PreviousPassword = user.Password;
                userprofile.BecomeAsSeller = user.BecomeAsSeller;
            }

            // Sign out the user
            return View(userprofile);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeProfile(ChangeProfile model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == model.Id && x.Password == model.PreviousPassword);
                if (user != null)
                { 
                    if(!string.IsNullOrEmpty(model.NewPassword))
                    {
                        user.Password = model.NewPassword;
                    }
                    user.Email = model.Email;
                    user.BecomeAsSeller = model.BecomeAsSeller;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Logout");
                }

            }
            return View(model);
        }

       
    }
}
