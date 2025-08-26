using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace google_reviews.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect to login instead of showing registration
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost()
        {
            // Redirect to login for any POST attempts
            return RedirectToPage("./Login");
        }
    }
}