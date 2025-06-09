using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Pages.AdminCenter
{
    public partial class AdminUniHome
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? UserProvider { get; set; }

        [Inject] private INotificationService notificationService { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private Admin.AdminHome adminHome { get; set; } = new();

        private StaffTypeModel StaffType { get; set; } = new();
        private List<ReportAdminModel> ReportAdmin { get; set; } = new();

        /// <summary>
        /// 01
        /// </summary>
        private List<ReportAdminModel> ReportAdminHatyai { get; set; } = new();

        /// <summary>
        /// 02
        /// </summary>
        private List<ReportAdminModel> ReportAdminPattani { get; set; } = new();

        /// <summary>
        /// 03
        /// </summary>
        private List<ReportAdminModel> ReportAdminPhuket { get; set; } = new();

        /// <summary>
        /// 04
        /// </summary>
        private List<ReportAdminModel> ReportAdminSuratThani { get; set; } = new();

        /// <summary>
        /// 05
        /// </summary>
        private List<ReportAdminModel> ReportAdminTrang { get; set; } = new();

        private decimal[] ManageLoanRequestStatusId { get; } = new[] { 1m, 2m, 4m };
        private decimal[] WaitingLoanConsiderStatusId { get; } = new[] { 1m };
        private decimal[] WaitingContractStatusId { get; } = new[] { 4m };
        private decimal[] CheckDocumentsStatusId { get; } = new[] { 7m };
        private decimal[] SentDocumentsCountStatusId { get; } = new[] { 6m };
        private DateTime DataTimeNow { get; set; } = DateTime.Now;
        /// <summary>
        /// TH
        /// </summary>
        private decimal? FiscalYear { get; set; } = null;

        private bool loading = true;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                FiscalYear = userService.GetFiscalYear(DataTimeNow);

                try
                {
                    List<ReportAdminModel> repost = await psuLoan.GetAllDataReportAdminForFiscal(DataTimeNow);
                    ReportAdmin = await userService.FindDataInFisicalYear(repost, (FiscalYear - 543));

                    ReportAdminHatyai = ReportAdmin.Where(x => x.CampusId == "01").ToList();
                    ReportAdminPattani = ReportAdmin.Where(x => x.CampusId == "02").ToList();
                    ReportAdminPhuket = ReportAdmin.Where(x => x.CampusId == "03").ToList();
                    ReportAdminSuratThani = ReportAdmin.Where(x => x.CampusId == "04").ToList();
                    ReportAdminTrang = ReportAdmin.Where(x => x.CampusId == "05").ToList();

                    loading = false;
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    loading = false;
                    await Error.ProcessError(ex);
                }
            }
        }

        private async Task PdfAllAsync()
        {
            var StorageName = "ReportAdmin";
            await CheckDataInStorageAsync(StorageName);

            navigationManager.NavigateTo("/Admin/FilterReportAdmin");
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
