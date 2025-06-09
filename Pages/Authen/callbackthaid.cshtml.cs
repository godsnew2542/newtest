using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.IServices;
using LoanApp.Model.Models;
using LoanApp.Model.ThaIdModel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Radzen;

namespace LoanApp.Pages.Authen
{
    public class callbackthaidModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string Code { get; set; } = null!;

        private readonly IPsuoAuth2Services PSUOAuth2Services;
        private readonly Services.IServices.INotificationService notificationService;
        private readonly Services.IServices.LoanDb.IPsuLoan psuLoan;

        public callbackthaidModel(IPsuoAuth2Services OAuth2Services, Services.IServices.INotificationService notificationService, Services.IServices.LoanDb.IPsuLoan psuLoan)
        {
            PSUOAuth2Services = OAuth2Services;
            this.notificationService = notificationService;
            this.psuLoan = psuLoan;
        }

        public async Task<ActionResult> OnGet()
        {
            try
            {
                Tuple<TokenInfo?, UserInfo?> info = await PSUOAuth2Services.CallTokenThaId(Code);
                TokenInfo? tokenInfo = info.Item1;
                UserInfo? userInfo = info.Item2;
                if (tokenInfo == null || tokenInfo.access_token == null)
                {
                    return LocalRedirect(Url.Content("~/"));
                }

                VLoanStaffDetail? user = await psuLoan.GetUserDetailBy(userInfo?.pid);

                if (user == null)
                {
                    return LocalRedirect(Url.Content("~/"));
                }

                var staffid = user.StaffId;

                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, user.StaffNameThai ?? string.Empty),
                    new Claim(ClaimTypes.GivenName, user.StaffNameEng ?? string.Empty),
                    new Claim("CampusID", user.CampusId ?? string.Empty),
                    new Claim("StaffID", staffid),
                    new Claim("Token", tokenInfo.access_token ?? string.Empty),
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
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
                throw;
            }
        }
    }
}
