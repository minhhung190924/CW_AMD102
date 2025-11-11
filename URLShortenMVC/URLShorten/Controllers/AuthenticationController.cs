using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using URLShorten.Data.IdentityEntities;
using URLShorten.Models.Identities;

namespace URLShorten.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly SignInManager<UrlLinksUser> _signInManager;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly UserManager<UrlLinksUser> _userManager;
        private readonly IUserStore<UrlLinksUser> _userStore;
        private readonly IUserEmailStore<UrlLinksUser> _emailStore;

        public AuthenticationController(
            SignInManager<UrlLinksUser> signInManager,
            UserManager<UrlLinksUser> userManager,
            IUserStore<UrlLinksUser> userStore,
            ILogger<AuthenticationController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
        }

        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var message = TempData["ErrorMessage"]?.ToString();
            if (!string.IsNullOrEmpty(message))
            {
                ModelState.AddModelError(string.Empty, message);
            }

            // Clear external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var loginVM = new LoginVM
            {
                ReturnUrl = returnUrl ??= Url.Content("~/"),
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };

            return View(loginVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            TempData.Keep();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(loginVM.Email, loginVM.Password, loginVM.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(loginVM.ReturnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = loginVM.ReturnUrl, RememberMe = loginVM.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(loginVM);
                }
                
            }

            return View(loginVM);
        }

        [HttpGet]
        public IActionResult AdminLogin(string returnUrl = "/UrlLinks/Index")
        {
            var model = new LoginVM { ReturnUrl = returnUrl };
            return View("AdminLogin", model);
        }

        [HttpPost]
        public async Task<IActionResult> AdminLogin(LoginVM model)
        {
            if (!ModelState.IsValid) return View("AdminLogin", model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                ModelState.AddModelError("", "Invalid admin credentials.");
                return View("AdminLogin", model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
            if (result.Succeeded)
                return Redirect(model.ReturnUrl ?? "/UrlLinks/Index");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View("AdminLogin", model);
        }

        public async Task<IActionResult> Register(string? returnUrl = null)
        {
            var registerVM = new RegisterVM
            {
                ReturnUrl = returnUrl ??= Url.Content("~/"),
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList(),
            };
            return View(registerVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (ModelState.IsValid)
            {
                // Custom password validation
                var password = registerVM.Password;
                var errors = new List<string>();

                if (password.Length < 8)
                    errors.Add("Password must be at least 8 characters long.");
                if (!password.Any(char.IsUpper))
                    errors.Add("Password must contain at least one uppercase letter.");
                if (!password.Any(char.IsDigit))
                    errors.Add("Password must contain at least one number.");
                if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                    errors.Add("Password must contain at least one special character.");

                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                        ModelState.AddModelError(nameof(registerVM.Password), error);
                    return View(registerVM);
                }

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, registerVM.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, registerVM.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, registerVM.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = registerVM.ReturnUrl },
                        protocol: Request.Scheme);

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = registerVM.Email, returnUrl = registerVM.ReturnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(registerVM.ReturnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return RedirectToAction("Login", "Authentication");
            }

            return View(registerVM);
        }


        //Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Authentication");
        }

        public IActionResult Index()
        {
            return View();
        }

        private UrlLinksUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<UrlLinksUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(UrlLinksUser)}'. " +
                    $"Ensure that '{nameof(UrlLinksUser)}' is not abstract and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<UrlLinksUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<UrlLinksUser>)_userStore;
        }
    }
}