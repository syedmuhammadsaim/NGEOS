using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.AspNetCore.Authentication.Google;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using NGEOS.Data;

using NGEOS.Helpers;

using NGEOS.Models;

using NGEOS.Services;

using NGEOS.ViewModels;



namespace NGEOS.Controllers

{

    public class AccountController : Controller

    {

        private readonly ApplicationDbContext _context;

        private readonly PasswordResetService _resetService;

        private readonly ILogger<AccountController> _logger;

        private readonly IConfiguration _configuration;



        public AccountController(

            ApplicationDbContext context,

            PasswordResetService resetService,

            ILogger<AccountController> logger,

            IConfiguration configuration)

        {

            _context = context;

            _resetService = resetService;

            _logger = logger;

            _configuration = configuration;

        }



        [HttpGet]

        public IActionResult Register(string? returnUrl = null)

        {

            if (User.Identity?.IsAuthenticated == true)

            {

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))

                {

                    return Redirect(returnUrl);

                }

                return RedirectToAction("Index", "Home");

            }

            return View(new RegisterViewModel { ReturnUrl = returnUrl });

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Register(RegisterViewModel model)

        {

            if (!ModelState.IsValid)

            {

                return View(model);

            }



            // Check if email is already in use

            var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower().Trim());

            if (existingUser)

            {

                ModelState.AddModelError("Email", "An account with this email address already exists. Please login instead.");

                return View(model);

            }



            // Create new regular User with personal and professional details

            var user = new User

            {

                FullName = model.FullName.Trim(),

                Email = model.Email.ToLower().Trim(),

                PasswordHash = PasswordHelper.HashPassword(model.Password),

                Phone = model.Phone?.Trim(),

                Address = model.Address?.Trim(),

                Profession = model.Profession.Trim(),

                Organization = model.Organization.Trim(),

                Role = "User",

                CreatedAt = DateTime.UtcNow,

                IsActive = true

            };



            _context.Users.Add(user);

            await _context.SaveChangesAsync();



            // Sign in the newly registered user

            await SignInUserAsync(user, false);



            TempData["SuccessMessage"] = $"Welcome to NGEOS, {user.FullName}! Your account has been created successfully.";



            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))

            {

                return Redirect(model.ReturnUrl);

            }



            return RedirectToAction("Dashboard", "User");

        }



        [HttpGet]

        public IActionResult Login(string? returnUrl = null)

        {

            if (User.Identity?.IsAuthenticated == true)

            {

                if (User.IsInRole("Admin"))

                {

                    return RedirectToAction("Index", "Admin");

                }

                return RedirectToAction("Dashboard", "User");

            }



            // Clean up any lingering post-login welcome messages from unauthenticated login view

            if (TempData["SuccessMessage"] is string msg &&

                (msg.Contains("Welcome back", StringComparison.OrdinalIgnoreCase) ||

                 msg.Contains("linked successfully", StringComparison.OrdinalIgnoreCase)))

            {

                TempData.Remove("SuccessMessage");

            }



            return View(new LoginViewModel { ReturnUrl = returnUrl });

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Login(LoginViewModel model)

        {

            if (!ModelState.IsValid)

            {

                return View(model);

            }



            var input = (model.Email ?? string.Empty).Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == input.ToLower());



            // If not found by email, attempt phone number lookup

            if (user == null)

            {

                var inputDigits = new string(input.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrEmpty(inputDigits) && inputDigits.Length >= 7)

                {

                    var allPhoneUsers = await _context.Users.Where(u => !string.IsNullOrEmpty(u.Phone)).ToListAsync();

                    user = allPhoneUsers.FirstOrDefault(u => {

                        var uDigits = new string(u.Phone!.Where(char.IsDigit).ToArray());

                        return uDigits.EndsWith(inputDigits) || inputDigits.EndsWith(uDigits);

                    });

                }

            }



            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))

            {

                ModelState.AddModelError(string.Empty, "Invalid email address, mobile number, or password. Please try again.");

                return View(model);

            }



            if (!user.IsActive)

            {

                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact NGEOS support.");

                return View(model);

            }



            await SignInUserAsync(user, model.RememberMe);



            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";



            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))

            {

                return Redirect(model.ReturnUrl);

            }



            if (user.Role == "Admin")

            {

                return RedirectToAction("Index", "Admin");

            }



            return RedirectToAction("Dashboard", "User");

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Logout()

        {

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignOutAsync("External");

            HttpContext.Session.Clear();

            TempData.Clear();

            TempData["InfoMessage"] = "You have been logged out successfully.";

            return RedirectToAction("Index", "Home");

        }



        [HttpGet]

        public async Task<IActionResult> Logout(string? redirectUrl = null)

        {

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignOutAsync("External");

            HttpContext.Session.Clear();

            TempData.Clear();

            TempData["InfoMessage"] = "You have been logged out successfully.";

            return RedirectToAction("Index", "Home");

        }



        [HttpGet]

        public IActionResult AccessDenied()

        {

            return View();

        }



        #region Google Authentication



        [HttpGet]

        public IActionResult GoogleLogin(string? returnUrl = null)

        {

            var clientId = _configuration["Authentication:Google:ClientId"];

            var clientSecret = _configuration["Authentication:Google:ClientSecret"];



            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) ||

                clientId == "placeholder-client-id" || clientSecret == "placeholder-client-secret")

            {

                _logger.LogWarning("Google login attempted but Google ClientId or ClientSecret is not configured.");

                TempData["ErrorMessage"] = "Google Sign-In is not configured yet. Please configure Authentication:Google:ClientId and ClientSecret in User Secrets or appsettings.json.";

                return RedirectToAction(nameof(Login), new { returnUrl });

            }



            var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });

            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);

        }



        [HttpGet]

        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)

        {

            if (!string.IsNullOrEmpty(remoteError))

            {

                _logger.LogWarning("Error received from Google OAuth provider: {RemoteError}", remoteError);

                TempData["ErrorMessage"] = $"Google Sign-In was cancelled or failed: {remoteError}";

                return RedirectToAction(nameof(Login), new { returnUrl });

            }



            var authenticateResult = await HttpContext.AuthenticateAsync("External");

            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)

            {

                _logger.LogWarning("Failed to authenticate with External scheme.");

                TempData["ErrorMessage"] = "Google authentication failed. Please try again.";

                return RedirectToAction(nameof(Login), new { returnUrl });

            }



            var principal = authenticateResult.Principal;

            var googleId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 

                           ?? principal.FindFirst("sub")?.Value;

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            var name = principal.FindFirst(ClaimTypes.Name)?.Value 

                       ?? principal.FindFirst(ClaimTypes.GivenName)?.Value;

            var picture = principal.FindFirst("picture")?.Value 

                          ?? principal.FindFirst("urn:google:picture")?.Value;



            // Clear the temporary external cookie

            await HttpContext.SignOutAsync("External");



            if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(email))

            {

                TempData["ErrorMessage"] = "Could not retrieve your Google email or profile ID.";

                return RedirectToAction(nameof(Login), new { returnUrl });

            }



            var normalizedEmail = email.Trim().ToLower();



            // 1. Check if an account is already linked with this GoogleId

            var userByGoogleId = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (userByGoogleId != null)

            {

                if (!userByGoogleId.IsActive)

                {

                    TempData["ErrorMessage"] = "Your account has been deactivated. Please contact NGEOS support.";

                    return RedirectToAction(nameof(Login));

                }



                await SignInUserAsync(userByGoogleId, isPersistent: true);

                TempData["SuccessMessage"] = $"Welcome back, {userByGoogleId.FullName}!";



                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))

                {

                    return Redirect(returnUrl);

                }



                if (userByGoogleId.Role == "Admin")

                {

                    return RedirectToAction("Index", "Admin");

                }

                return RedirectToAction("Dashboard", "User");

            }



            // 2. Check if a user already exists with this Email

            var userByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (userByEmail != null)

            {

                // Requirement 7: Never automatically link an unverified existing account solely because its email matches.

                // Require secure account linking via password verification.

                HttpContext.Session.SetString("LinkGoogle_Email", normalizedEmail);

                HttpContext.Session.SetString("LinkGoogle_Id", googleId);

                HttpContext.Session.SetString("LinkGoogle_Name", name ?? userByEmail.FullName);

                HttpContext.Session.SetString("LinkGoogle_Picture", picture ?? userByEmail.ProfileImageUrl ?? "");

                if (!string.IsNullOrEmpty(returnUrl))

                {

                    HttpContext.Session.SetString("LinkGoogle_ReturnUrl", returnUrl);

                }



                return RedirectToAction(nameof(LinkAccount));

            }



            // 3. New user registration via Google

            var newUser = new User

            {

                FullName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : "Google User",

                Email = normalizedEmail,

                GoogleId = googleId,

                PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString("N")),

                Role = "User",

                ProfileImageUrl = picture,

                IsActive = true,

                CreatedAt = DateTime.UtcNow

            };



            _context.Users.Add(newUser);

            await _context.SaveChangesAsync();



            await SignInUserAsync(newUser, isPersistent: true);

            TempData["SuccessMessage"] = $"Welcome to NGEOS, {newUser.FullName}! Your account has been registered with Google.";



            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))

            {

                return Redirect(returnUrl);

            }

            return RedirectToAction("Dashboard", "User");

        }



        // GET: /Account/LinkAccount

        [HttpGet]

        public IActionResult LinkAccount()

        {

            var email = HttpContext.Session.GetString("LinkGoogle_Email");

            var googleId = HttpContext.Session.GetString("LinkGoogle_Id");



            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(googleId))

            {

                return RedirectToAction(nameof(Login));

            }



            var vm = new LinkAccountViewModel

            {

                Email = email,

                FullName = HttpContext.Session.GetString("LinkGoogle_Name"),

                ProfileImageUrl = HttpContext.Session.GetString("LinkGoogle_Picture"),

                ReturnUrl = HttpContext.Session.GetString("LinkGoogle_ReturnUrl")

            };



            return View("~/Views/Account/LinkAccount.cshtml", vm);

        }



        // POST: /Account/LinkAccount

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> LinkAccount(LinkAccountViewModel model)

        {

            var sessionEmail = HttpContext.Session.GetString("LinkGoogle_Email");

            var googleId = HttpContext.Session.GetString("LinkGoogle_Id");



            if (string.IsNullOrWhiteSpace(sessionEmail) || string.IsNullOrWhiteSpace(googleId))

            {

                TempData["ErrorMessage"] = "Account linking session has expired. Please sign in again.";

                return RedirectToAction(nameof(Login));

            }



            if (!ModelState.IsValid)

            {

                return View("~/Views/Account/LinkAccount.cshtml", model);

            }



            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == sessionEmail.ToLower());

            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))

            {

                ModelState.AddModelError("Password", "Incorrect password. Please enter your existing NGEOS password to verify account ownership.");

                return View("~/Views/Account/LinkAccount.cshtml", model);

            }



            if (!user.IsActive)

            {

                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");

                return View("~/Views/Account/LinkAccount.cshtml", model);

            }



            // Link GoogleId securely

            user.GoogleId = googleId;

            var picture = HttpContext.Session.GetString("LinkGoogle_Picture");

            if (string.IsNullOrEmpty(user.ProfileImageUrl) && !string.IsNullOrEmpty(picture))

            {

                user.ProfileImageUrl = picture;

            }



            await _context.SaveChangesAsync();



            // Clear session data

            HttpContext.Session.Remove("LinkGoogle_Email");

            HttpContext.Session.Remove("LinkGoogle_Id");

            HttpContext.Session.Remove("LinkGoogle_Name");

            HttpContext.Session.Remove("LinkGoogle_Picture");

            var returnUrl = HttpContext.Session.GetString("LinkGoogle_ReturnUrl");

            HttpContext.Session.Remove("LinkGoogle_ReturnUrl");



            await SignInUserAsync(user, isPersistent: true);

            TempData["SuccessMessage"] = $"Your Google account has been linked successfully! Welcome back, {user.FullName}.";



            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))

            {

                return Redirect(returnUrl);

            }



            if (user.Role == "Admin")

            {

                return RedirectToAction("Index", "Admin");

            }

            return RedirectToAction("Dashboard", "User");

        }



        #endregion



        #region Forgot Password & Reset Flow



        // GET: /Account/ForgotPassword

        [HttpGet]

        public IActionResult ForgotPassword()

        {

            if (User.Identity?.IsAuthenticated == true)

            {

                return RedirectToAction("Index", "Home");

            }

            return View("~/Views/Account/ForgotPassword.cshtml", new ForgotPasswordViewModel());

        }



        // POST: /Account/ForgotPassword

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)

        {

            if (!ModelState.IsValid)

            {

                return View("~/Views/Account/ForgotPassword.cshtml", model);

            }



            var (success, errorMessage, remainingSeconds, userExists) = await _resetService.RequestResetCodeAsync(model.Email);



            if (!success)

            {

                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to send verification code. Please try again.");

                return View("~/Views/Account/ForgotPassword.cshtml", model);

            }



            TempData["InfoMessage"] = "If this email is registered in our system, a 6-digit verification code has been sent.";

            return RedirectToAction(nameof(VerifyCode), new { email = model.Email.Trim().ToLower() });

        }



        // GET: /Account/VerifyCode

        [HttpGet]

        public IActionResult VerifyCode(string? email)

        {

            if (string.IsNullOrWhiteSpace(email))

            {

                return RedirectToAction(nameof(ForgotPassword));

            }



            var vm = new VerifyCodeViewModel

            {

                Email = email.Trim().ToLower()

            };

            return View("~/Views/Account/VerifyCode.cshtml", vm);

        }



        // POST: /Account/VerifyCode

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)

        {

            if (!ModelState.IsValid)

            {

                return View("~/Views/Account/VerifyCode.cshtml", model);

            }



            var (success, errorMessage, resetSessionToken) = await _resetService.VerifyCodeAsync(model.Email, model.Code);



            if (!success)

            {

                ModelState.AddModelError(string.Empty, errorMessage ?? "Invalid or expired verification code.");

                return View("~/Views/Account/VerifyCode.cshtml", model);

            }



            TempData["SuccessMessage"] = "Verification code confirmed successfully. Please create your new password.";

            return RedirectToAction(nameof(ResetPassword), new { email = model.Email.Trim().ToLower(), token = resetSessionToken });

        }



        // POST: /Account/ResendCode

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ResendCode(string email)

        {

            if (string.IsNullOrWhiteSpace(email))

            {

                TempData["ErrorMessage"] = "Email address is required to resend verification code.";

                return RedirectToAction(nameof(ForgotPassword));

            }



            var (success, errorMessage, remainingSeconds, userExists) = await _resetService.RequestResetCodeAsync(email);



            if (!success)

            {

                TempData["ErrorMessage"] = errorMessage ?? "Unable to resend code at this time.";

            }

            else

            {

                TempData["SuccessMessage"] = "A new 6-digit verification code has been sent to your email address.";

            }



            return RedirectToAction(nameof(VerifyCode), new { email = email.Trim().ToLower() });

        }



        // POST: /Account/ValidateAccountForSms

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ValidateAccountForSms([FromForm] string? email, [FromForm] string? phone)

        {

            User? user = null;



            // 1. Try to find user by Phone number

            if (!string.IsNullOrWhiteSpace(phone))

            {

                var inputDigits = new string(phone.Where(char.IsDigit).ToArray());

                if (inputDigits.Length >= 7)

                {

                    var allPhoneUsers = await _context.Users.Where(u => !string.IsNullOrEmpty(u.Phone)).ToListAsync();

                    user = allPhoneUsers.FirstOrDefault(u => {

                        var uDigits = new string(u.Phone!.Where(char.IsDigit).ToArray());

                        return uDigits.EndsWith(inputDigits) || inputDigits.EndsWith(uDigits);

                    });

                }

            }



            // 2. Try to find user by Email if not found by phone

            if (user == null && !string.IsNullOrWhiteSpace(email))

            {

                var normalizedEmail = email.Trim().ToLower();

                user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            }



            if (user == null)

            {

                return Json(new { success = false, message = "No account found matching this mobile number or email address." });

            }



            if (!user.IsActive)

            {

                return Json(new { success = false, message = "This account is inactive. Please contact NGEOS support." });

            }



            return Json(new { 

                success = true, 

                email = user.Email, 

                name = user.FullName,

                phone = user.Phone ?? "" 

            });

        }



        // POST: /Account/CompletePhoneResetVerification

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CompletePhoneResetVerification([FromForm] string? email, [FromForm] string? phoneNumber)
        {
            User? user = null;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLower();
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            }
            if (user == null && !string.IsNullOrWhiteSpace(phoneNumber))
            {
                var inputDigits = new string(phoneNumber.Where(char.IsDigit).ToArray());
                if (inputDigits.Length >= 7)
                {
                    var allPhoneUsers = await _context.Users.Where(u => !string.IsNullOrEmpty(u.Phone)).ToListAsync();
                    user = allPhoneUsers.FirstOrDefault(u => {
                        var uDigits = new string(u.Phone!.Where(char.IsDigit).ToArray());
                        return uDigits.EndsWith(inputDigits) || inputDigits.EndsWith(uDigits);
                    });
                }
            }
            if (user == null || !user.IsActive)
            {
                return Json(new { success = false, message = "Account not found or inactive." });
            }


            if (user == null || !user.IsActive)

            {

                return Json(new { success = false, message = "Account not found or inactive." });

            }



            // If phone provided, save/update phone in profile if not already set

            if (!string.IsNullOrWhiteSpace(phoneNumber))

            {

                user.Phone = phoneNumber.Trim();

            }



            // Invalidate any existing active tokens for this user

            var oldTokens = await _context.PasswordResetTokens

                .Where(t => t.UserId == user.Id && !t.IsUsed)

                .ToListAsync();



            foreach (var t in oldTokens)

            {

                t.IsUsed = true;

            }



            // Create a pre-verified reset token

            var sessionToken = Guid.NewGuid().ToString("N");

            var token = new PasswordResetToken

            {

                UserId = user.Id,

                Code = _resetService.GenerateSecure6DigitCode(),

                Expiration = DateTime.UtcNow.AddMinutes(PasswordResetService.ExpirationMinutes),

                CreatedAt = DateTime.UtcNow,

                IsVerified = true,

                ResetSessionToken = sessionToken,

                IsUsed = false

            };



            _context.PasswordResetTokens.Add(token);

            await _context.SaveChangesAsync();



            var redirectUrl = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token = sessionToken });

            return Json(new { success = true, redirectUrl });

        }



        // GET: /Account/ResetPassword

        [HttpGet]

        public async Task<IActionResult> ResetPassword(string? email, string? token)

        {

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))

            {

                TempData["ErrorMessage"] = "Invalid or expired password reset session. Please request a new verification code.";

                return RedirectToAction(nameof(ForgotPassword));

            }



            var (isValid, errorMessage) = await _resetService.ValidateResetSessionTokenAsync(email, token);

            if (!isValid)

            {

                TempData["ErrorMessage"] = errorMessage ?? "Your reset session is invalid or has expired.";

                return RedirectToAction(nameof(ForgotPassword));

            }



            var vm = new ResetPasswordViewModel

            {

                Email = email.Trim().ToLower(),

                ResetSessionToken = token

            };



            return View("~/Views/Account/ResetPassword.cshtml", vm);

        }



        // POST: /Account/ResetPassword

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)

        {

            if (!ModelState.IsValid)

            {

                return View("~/Views/Account/ResetPassword.cshtml", model);

            }



            var (success, errorMessage) = await _resetService.ResetPasswordAsync(

                model.Email,

                model.ResetSessionToken,

                model.NewPassword);



            if (!success)

            {

                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to reset password. Please restart the process.");

                return View("~/Views/Account/ResetPassword.cshtml", model);

            }



            TempData["SuccessMessage"] = "Your password has been reset successfully! Please sign in with your new password.";

            return RedirectToAction(nameof(Login));

        }



        #endregion



        private async Task SignInUserAsync(User user, bool isPersistent)

        {

            var claims = new List<Claim>

            {

                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

                new Claim(ClaimTypes.Name, user.FullName),

                new Claim(ClaimTypes.Email, user.Email),

                new Claim(ClaimTypes.Role, user.Role),

                new Claim("Profession", user.Profession ?? ""),

                new Claim("Organization", user.Organization ?? ""),

                new Claim("ProfileImageUrl", user.ProfileImageUrl ?? "")

            };



            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties

            {

                IsPersistent = isPersistent,

                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(4)

            };



            await HttpContext.SignInAsync(

                CookieAuthenticationDefaults.AuthenticationScheme,

                new ClaimsPrincipal(claimsIdentity),

                authProperties);

        }

    }

}

