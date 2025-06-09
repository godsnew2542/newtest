using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using LoanApp.IServices;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;

namespace LoanApp.Pages.Authen
{
    public class CallbackModel : PageModel
    {
        [BindProperty(SupportsGet = true)] 
        public string Code { get; set; } = null!;

        public IPsuoAuth2Services PSUOAuth2Services { get; set; }
        private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; }
        private ModelContext Context { get; set; }

        public CallbackModel(IPsuoAuth2Services OAuth2Services, ModelContext context, Services.IServices.LoanDb.IPsuLoan _psuLoan)
        {
            PSUOAuth2Services = OAuth2Services;
            Context = context;
            psuLoan = _psuLoan;
        }

        public async Task<ActionResult> OnGet()
        {
            try
            {
                TokenInfo tokenInfo = await PSUOAuth2Services.CallTokenAsync(Code);
                if (tokenInfo == null || tokenInfo.access_token == null)
                {
                    return LocalRedirect(Url.Content("~/"));
                }

                Model.Models.User user = await PSUOAuth2Services.GetProfileByAccessTokenAsync(tokenInfo.access_token);

                var staffid = user.StaffId;

                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, user.StaffNameThai ?? string.Empty),
                    new Claim(ClaimTypes.GivenName, user.StaffNameEng ?? string.Empty),
                    new Claim("CampusID", user.CampusId ?? string.Empty),
                    new Claim("StaffID", staffid ?? string.Empty),
                    new Claim("Token", string.Empty),
                };

                #region Set ClaimTypes.Role
                claims.Add(new Claim(ClaimTypes.Role, $"{(int)RoleType.User}"));

                List<LoanStaffPrivilege> loanStaffPrivilege = await psuLoan.GetLoanStaffPrivilegeByStaffId(staffid);
                Dictionary<decimal, LoanGroup> loanGroupDict = await psuLoan.GetLoanGroupDict();

                foreach (var item in loanStaffPrivilege)
                {
                    decimal roleGroup = loanGroupDict[item.GroupId!.Value].GroupId;
                    if (!claims.Exists(c => c.Value == roleGroup.ToString()))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleGroup.ToString()));
                    }
                }
                #endregion

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                   CookieAuthenticationDefaults.AuthenticationScheme,
                   new ClaimsPrincipal(claimsIdentity),
                   new AuthenticationProperties()
                   {
                       IsPersistent = true,
                       ExpiresUtc = DateTime.Now.AddHours(1)

                   });

                return LocalRedirect(Url.Content("~/"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

    }
}
