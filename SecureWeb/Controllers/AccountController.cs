using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SecureWeb.Data;
using SecureWeb.Models;
using SecureWeb.ViewModel;

namespace SecureWeb.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AccountController : Controller
    {
        private readonly IUser _user;
        private readonly ILogger<AccountController> _logger;
        public AccountController(IUser user,ILogger<AccountController> logger)
        {
            _user = user;
            _logger = logger;
        }

        // GET: AccountController
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]

        public ActionResult Register(RegistrationViewModell registrationViewModell)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (!IsValidPassword(registrationViewModell.Password ?? string.Empty))
                    {
                        ModelState.AddModelError("Password", "- Min character 12.-Harus mengandung huruf besar, huruf kecil, dan angka");
                        return View(registrationViewModell); // Tetap di halaman registrasi jika validasi gagal
                    }
                    var user = new Models.User
                    {
                        Username = registrationViewModell.Username ?? string.Empty,
                        Password = registrationViewModell.Password ?? string.Empty,
                        Role = "Contributor"
                    };
                    _user.Registratiion(user);
                    return RedirectToAction("Login", "Account");
                }
                return View(registrationViewModell);
            }
            catch (System.Exception ex)
            {
                ViewBag.error = ex.Message;

            }
            return View(registrationViewModell);
        }

        private bool IsValidPassword(string? password)
        {
            if (password == null) return false;
            if (password.Length < 12) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            return true;
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel loginViewModel)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", loginViewModel.Username);

                var user = new User
                {
                    Username = loginViewModel.Username,
                    Password = loginViewModel.Password
                };

                var loginUser = _user.Login(user);
                if (loginUser == null)
                {
                    _logger.LogWarning("Failed login attempt for user: {Username}", loginViewModel.Username);
                    ViewBag.error = "Invalid username or password";
                    return View(loginViewModel);
                }
                _logger.LogInformation("Successful login for user: {Username}", loginViewModel.Username);

                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username)
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = loginViewModel.RememberMe
                    }
                );

                return RedirectToAction("Index", "Home");

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during login process for user: {Username}", loginViewModel.Username);
                ViewBag.Message = ex.Message;
            }
            return View(loginViewModel);
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(ChangePwViewModel model)
        {
            try
            {
                _logger.LogInformation("Password change attempt for user: {Username}", model.Username);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = _user.GetUserByUsername(model.Username);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found");
                    return View(model);
                }

                // Verify old password
                if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, user.Password))
                {
                    ModelState.AddModelError("", "Old password is incorrect");
                    return View(model);
                }

                // Update password
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _user.UpdatePassword(user);

                _logger.LogInformation("Password successfully changed for user: {Username}", model.Username);
                ViewBag.Message = "Password changed successfully. Please login again.";
                ViewBag.ShowLoginButton = true; // Tampilkan tombol login setelah password berhasil diubah.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user: {Username}", model.Username);
                ModelState.AddModelError("", ex.Message);
            }
            return View(model);
        }

    }
}
