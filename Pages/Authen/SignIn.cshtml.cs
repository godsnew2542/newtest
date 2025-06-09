using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LoanApp.Pages.Authen
{
    public class SignInModel : PageModel
    {
         public async Task<IActionResult> OnGet()
         {
             var claims = new List<Claim>
         {
             new Claim(ClaimTypes.Name, "Wachirawit"),
             new Claim(ClaimTypes.Email, "wachirawit.j@psu.ac.th"),

            /* คนที่อายุตรงเหลือไม่ถึง */
            /* new Claim("StaffID", "0005269")*/

            /* นางสายใจ ณ สุวรรณ */
            /*new Claim("StaffID", "0001972")*/

             new Claim("StaffID", "0036824")
         };

             var claimsIdentity = new ClaimsIdentity(
                 claims,
                 CookieAuthenticationDefaults.AuthenticationScheme);

             await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));
             return LocalRedirect(Url.Content("~/"));
         }
    }
}
