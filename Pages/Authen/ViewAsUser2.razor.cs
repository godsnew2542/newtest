using LoanApp.DatabaseModel.LoanEntities;

using LoanApp.Model.Helper;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Runtime.CompilerServices;

namespace LoanApp.Pages.Authen
{
    public partial class ViewAsUser2
    {
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; }
        [Inject] private INotificationService notificationService { get; set; }

        private List<VLoanStaffDetail> vLoanStaffDetails { get; set; } = new();

        private string? searchValue = null;
        private bool loading { get; set; } = false;

        private async Task OnEnter(KeyboardEventArgs e, string? changedString)
        {
            await Submit(changedString);
        }

        private async Task Submit(string? changedString)
        {
            loading = true;
            StateHasChanged();
            string? _Search = null;

            _Search = userService.CheckSearchText(changedString);

            if (string.IsNullOrEmpty(_Search))
            {
                loading = false;
                await notificationService.WarningDefult($"กรุณาใส่ข้อมูล อย่างน้อย {Utility.SearchMinlength} ตัวขึ้นไป");
                return;
            }

            vLoanStaffDetails = await psuLoan.GetListVLoanStaffDetailViewAsUser(_Search);

            loading = false;
            StateHasChanged();

            if (!vLoanStaffDetails.Any())
            {
                await notificationService.WarningDefult("ไม่พบข้อมูล");
            }
        }

        private void SetMember(string staffId)
        {
            navigationManager.NavigateTo($"Authen/ViewAsUserCallback/{staffId}", true);
        }
    }
}
