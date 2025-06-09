using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Pages.User
{
    public partial class ContactUs
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan PsuLoan { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        private VLoanStaffDetail? StaffDetail { get; set; } = null;

        private string BgColor { get; set; } = "background-color:rgb(205 237 235);";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    StaffDetail = await PsuLoan.GetUserDetailAsync(StateProvider?.CurrentUser.StaffId);

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await notificationService.Error(notificationService.ExceptionLog(ex));
                }
            }
        }
    }
}
