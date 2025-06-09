using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Settings;
using LoanApp.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace LoanApp.Pages.Authen
{
    public partial class ViewAsUser
    {
        [Inject] private INotificationService notificationService { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private IOptions<DelAuth> dtlAuthData { get; set; } = null!;


        private string? userName { get; set; } = null;
        private string? passWord { get; set; } = null;
        private List<string> containsUser { get; set; } = new()
        {
            "watcharapon.n@psu.ac.th", /// Dev
            "naparat.h@psu.ac.th", /// Dev
            "duangthida.c@psu.ac.th", /// Dev
            "pharadee.w@phuket.psu.ac.th", /// Hatyai
            "anna.te@phuket.psu.ac.th", /// Phuket
            "sittiporn.p@phuket.psu.ac.th" /// Phuket
        };
        private string passDev { get; set; } = "1234";

        protected override void OnInitialized()
        {
            if (dtlAuthData != null)
            {
                if (!string.IsNullOrEmpty(dtlAuthData.Value.Key))
                {
                    passDev = dtlAuthData.Value.Key;
                }

                if (dtlAuthData.Value.Email != null && dtlAuthData.Value.Email .Length >0)
                {
                    containsUser = dtlAuthData.Value.Email.ToList();
                }
            }
        }

        private async Task ViewAsLogin()
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passWord))
            {
                await notificationService.WarningDefult("กรอกข้อมูล");
            }

            if (containsUser.Contains(userName!.ToLower()))
            {
                if (passWord == passDev)
                {
                    VLoanStaffDetail? vLoanStaff = await psuLoan.GetVLoanStaffDetailByEmail(userName.ToLower());
                    navigationManager.NavigateTo($"Authen/ViewAsUserCallback/{vLoanStaff?.StaffId}", true);
                }
                else
                {
                    await notificationService.WarningDefult("No data");
                }
            }
            else
            {
                await notificationService.WarningDefult("ไม่พบข้อมูล/ไม่มีสิทธิเข้าใช้งาน");
            }
        }
    }
}
