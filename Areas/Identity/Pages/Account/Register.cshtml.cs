// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MasterServicePlatform.Web.Models;

namespace MasterServicePlatform.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IServiceProvider _serviceProvider;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _serviceProvider = serviceProvider;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // ----------------------------------------
        // NORMALIZATION METHOD
        // ----------------------------------------
        private string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            value = value.Trim();
            return char.ToUpper(value[0]) + value.Substring(1).ToLower();
        }

        // Input model
        public class InputModel
        {
            // Fields for all
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; } = "User";

            // Fields for user
            [Display(Name = "First name")]
            public string FirstName { get; set; }

            [Display(Name = "Last name")]
            public string LastName { get; set; }

            [Display(Name = "City")]
            public string City { get; set; }

            [Display(Name = "Phone")]
            public string Phone { get; set; }

            // Fields for master
            [Display(Name = "First name (master)")]
            public string MasterFirstName { get; set; }

            [Display(Name = "Last name (master)")]
            public string MasterLastName { get; set; }

            [Display(Name = "City (master)")]
            public string MasterCity { get; set; }

            [Display(Name = "Profession")]
            public string Profession { get; set; }

            [Display(Name = "Experience (years)")]
            public int? ExperienceYears { get; set; }

            [Display(Name = "Price per hour")]
            public decimal? PricePerHour { get; set; }

            [Display(Name = "Phone (master)")]
            public string MasterPhone { get; set; }

            [Display(Name = "Description")]
            public string Description { get; set; }
        }

        // Registration page opening
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        // Registration processing
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // Normalize user names
                user.FirstName = Input.Role == "Master"
                    ? Normalize(Input.MasterFirstName)
                    : Normalize(Input.FirstName);

                user.LastName = Input.Role == "Master"
                    ? Normalize(Input.MasterLastName)
                    : Normalize(Input.LastName);

                user.PhoneNumber = Input.Role == "Master"
                    ? Input.MasterPhone?.Trim() ?? ""
                    : Input.Phone?.Trim() ?? "";

                if (Enum.TryParse<UserRole>(Input.Role, out var parsedRole))
                    user.Role = parsedRole;
                else
                    user.Role = UserRole.User;

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Role
                    if (!string.IsNullOrEmpty(Input.Role))
                        await _userManager.AddToRoleAsync(user, Input.Role);

                    // SQL
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        if (user.Role == UserRole.Master)
                        {
                            // Creating master
                            var master = new Master
                            {
                                FirstName = Normalize(Input.MasterFirstName ?? user.Email.Split('@')[0]),
                                LastName = Normalize(Input.MasterLastName),
                                City = Normalize(Input.MasterCity),
                                Profession = Normalize(Input.Profession),
                                ExperienceYears = Input.ExperienceYears ?? 0,
                                PricePerHour = Input.PricePerHour ?? 0,
                                Phone = Input.MasterPhone?.Trim() ?? "",
                                Email = user.Email,
                                Description = Input.Description?.Trim() ?? "",
                                VerificationStatus = VerificationStatus.Pending
                            };

                            dbContext.Masters.Add(master);
                            await dbContext.SaveChangesAsync();

                            user.MasterId = master.Id;
                            await _userManager.UpdateAsync(user);
                        }
                        else if (user.Role == UserRole.User)
                        {
                            // Creating user profile
                            var profile = new UserProfile
                            {
                                FirstName = Normalize(Input.FirstName ?? user.Email.Split('@')[0]),
                                LastName = Normalize(Input.LastName),
                                Phone = Input.Phone?.Trim() ?? "",
                                City = Normalize(Input.City),
                                UserId = user.Id
                            };

                            dbContext.UserProfiles.Add(profile);
                            await dbContext.SaveChangesAsync();
                        }
                    }

                    // Email verification
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                // Errors of creation
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Error page
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' has a parameterless constructor, or override this page.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("The default UI requires a user store with email support.");

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
