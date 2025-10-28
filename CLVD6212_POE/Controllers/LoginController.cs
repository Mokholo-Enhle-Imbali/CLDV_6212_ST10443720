using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using CLVD6212_POE.Data;
using CLVD6212_POE.Models.ViewModels;
using CLVD6212_POE.Service; // ✅ For IFunctionsApi
using CLVD6212_POE.Models;

namespace CLVD6212_POE.Controllers
    {
        public class LoginController : Controller
        {
            private readonly abcretailersDbContext _db;
            private readonly IFunctionsApi _functionsApi; // ✅ Access Azure Functions API

            public LoginController(abcretailersDbContext db, IFunctionsApi functionsApi)
            {
                _db = db;
                _functionsApi = functionsApi;
            }

            // =====================================
            // GET: /Login
            // =====================================
            [HttpGet]
            public IActionResult Index(string? returnUrl = null)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View(new LoginViewModel());
            }

            // =====================================
            // POST: /Login
            // =====================================
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
            {
                if (!ModelState.IsValid)
                    return View(model);

                // ✅ Find user in local SQL database
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Role == model.Role);

                if (user == null || user.PasswordHash != model.Password)
                {
                    ViewBag.Error = "Invalid username, password, or role.";
                    return View(model);
                }

                // ✅ Fetch matching Customer record from Azure (by Username, not Email)
                var customer = await _functionsApi.GetCustomerByUsernameAsync(user.Username);

                if (customer == null)
                {
                    ViewBag.Error = "Matching customer profile not found in Azure Table Storage.";
                    return View(model);
                }

                // ✅ Build user claims for cookie authentication
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserRole", user.Role),
                new Claim("CustomerId", customer.CustomerId)  // ✅ Crucial for linking Orders
            };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                // ✅ Cookie properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(45)
                };

                await HttpContext.SignInAsync("MyCookieAuth", principal, authProperties);

                // ✅ Optional: store in session (for extra safety)
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("CustomerId", customer.CustomerId);

                // ✅ Redirect appropriately
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("AdminDashboard", "Home");

                return RedirectToAction("CustomerDashboard", "Home");
            }

            // =====================================
            // GET: /Login/Logout
            // =====================================
            [HttpGet]
            public async Task<IActionResult> Logout()
            {
                await HttpContext.SignOutAsync("MyCookieAuth");
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Login");
            }

            // =====================================
            // GET: /Login/AccessDenied
            // =====================================
            [HttpGet]
            public IActionResult AccessDenied()
            {
                return View("AccessDenied");
            }
        }
    }

