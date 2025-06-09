using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Pages.Manager
{
    public partial class ManagerHome
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }


        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;


        private Admin.AdminHome adminHome { get; set; } = new();

        private StaffTypeModel StaffType { get; set; } = new();
        private List<ReportAdminModel> ReportAdmin { get; set; } = new();

        private decimal[] WaitingLoanConsiderStatusId { get; } = new[] { 1m };
        private decimal[] WaitingContractStatusId { get; } = new[] { 4m };
        private decimal[] CheckDocumentsStatusId { get; } = new[] { 7m };
        private decimal[] SentDocumentsCountStatusId { get; } = new[] { 6m };
        private decimal[] ManageLoanRequestStatusId { get; } = new[] { 8m };
        private decimal[] SumStatusId { get; } = new[] { 2m, 4m, 9m, 6m, 7m, 8m, 80m, 81m, 82m, 99m, 200m };
        private decimal[] StatusId { get; } = new[] { 9m, 6m, 7m, 8m, 80m, 81m, 82m, 99m, 200m };
        private DateTime DataTimeNow { get; set; } = DateTime.Now;

        private decimal? FiscalYear = null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                FiscalYear = userService.GetFiscalYear(DataTimeNow);

                try
                {
                    string? CapmId = StateProvider?.CurrentUser.CapmSelectNow;

                    if (CapmId != null)
                    {
                        List<ReportAdminModel> repost = await psuLoan.GetAllDataReportAdminForFiscal(DataTimeNow, CapmId);
                        ReportAdmin = await userService.FindDataInFisicalYear(repost, (FiscalYear - 543));
                    }

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
            }
        }

        private async Task PdfAllAsync()
        {
            var StorageName = "ReportAdmin";
            await CheckDataInStorageAsync(StorageName);

            navigationManager.NavigateTo("/Manager/FilterReportAdmin");
        }
        private async Task CheckDataInStorageAsync(string StorageName)
        {
            var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
            if (!string.IsNullOrEmpty(checkData))
            {
                await sessionStorage.RemoveItemAsync(StorageName);
            }
            else { }
        }
    }
}

