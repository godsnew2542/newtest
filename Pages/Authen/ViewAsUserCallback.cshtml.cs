using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.IServices;

namespace LoanApp.Pages.Authen
{
    public class ViewAsUserCallbackModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string StaffId { get; set; }

        private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; }

        public ViewAsUserCallbackModel(Services.IServices.LoanDb.IPsuLoan _psuLoan)
        {
            psuLoan = _psuLoan;
        }

        public async Task<ActionResult> OnGet()
        {
            if (!string.IsNullOrEmpty(StaffId))
            {
                Model.Models.User user = await psuLoan.DevSignIn(StaffId);

                var staffid = user.StaffId;


                List<Claim> claims = new()
                {
                    new(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new(ClaimTypes.NameIdentifier, user.StaffNameThai ?? string.Empty),
                    new(ClaimTypes.GivenName, user.StaffNameEng ?? string.Empty),
                    new("CampusID", user.CampusId ?? string.Empty),
                    new("StaffID", staffid ?? string.Empty),
                    new("Token", string.Empty),
                };

                #region Set ClaimTypes.Role
                claims.Add(new Claim(ClaimTypes.Role, $"{(int)RoleType.User}"));

                //if (Utility.UserDev.Contains(user.UserName))
                //{
                //    List<LoanGroup> roleGroup = await psuLoan.GetAllLoanGroup();
                //    foreach (var item in roleGroup)
                //    {
                //        if (!claims.Exists(c => c.Value == item.GroupId.ToString()))
                //        {
                //            claims.Add(new Claim(ClaimTypes.Role, item.GroupId.ToString()));
                //        }
                //    }
                //}
                //else if (!string.IsNullOrEmpty(staffid))
                //{
                    var RoleList = await psuLoan.GetLoanStaffPrivilegeByStaffId(staffid);
                    Dictionary<decimal, LoanGroup> loanGroupDict = await psuLoan.GetLoanGroupDict();

                    foreach (var item in RoleList)
                    {
                        var roleGroup = loanGroupDict[item.GroupId!.Value].GroupId;
                        if (!claims.Exists(c => c.Value == roleGroup.ToString()))
                        {
                            claims.Add(new Claim(ClaimTypes.Role, roleGroup.ToString()));
                        }
                    }
                //}
                #endregion

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), 
                    new AuthenticationProperties()
                {
                    IsPersistent = true,
                });
            }
            return LocalRedirect(Url.Content("~/"));
        }
    }
}
