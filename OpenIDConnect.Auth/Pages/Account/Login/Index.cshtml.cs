using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenIDConnect.Pages.Account.Login
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string Error { get; set; }

        public class InputModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.FindByNameAsync(Input.Username);
            if (user == null)
            {
                Error = "Invalid username or password";
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Input.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Redirect("~/connect/authorize" + Request.QueryString.Value);
            }

            Error = "Invalid username or password";
            return Page();
        }
    }

}
