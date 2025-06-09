using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Pages.Admin
{
    public partial class AdminHome
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? UserProvider { get; set; }

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;

        #endregion

        private StaffTypeModel StaffType { get; set; } = new();
        private List<ReportAdminModel> ReportAdmin { get; set; } = new();

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
                    string? CapmId = UserProvider?.CurrentUser.CapmSelectNow;

                    if (!string.IsNullOrEmpty(CapmId))
                    {
                        List<ReportAdminModel> repost = await psuLoan.GetAllDataReportAdminForFiscal(DataTimeNow, CapmId);

                        ReportAdmin = await userService.FindDataInFisicalYear(repost, (FiscalYear - 543));
                    }

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

        private void ToPageAppointment()
        {
            navigationManager.NavigateTo($"/Admin/ManageAppointment");
        }

        private void ToPageCheckloan()
        {
            navigationManager.NavigateTo($"/Admin/CheckLoanpage");
        }

        private void ToPageManageLoanRequest(decimal StatusId = 0m)
        {
            if (StatusId != 0)
            {
                navigationManager.NavigateTo($"/Admin/ManageLoanRequest/{StatusId}");
            }
            else
            {
                navigationManager.NavigateTo($"/Admin/ManageLoanRequest");
            }
        }

        private void ToPageManageLoanAgreement(decimal StatusId = 0m)
        {
            if (StatusId != 0)
            {
                navigationManager.NavigateTo($"/Admin/ManageLoanAgreement/{StatusId}");
            }
            else
            {
                navigationManager.NavigateTo($"/Admin/ManageLoanAgreement");
            }
        }

        private void ToPageRecordFirstPayment()
        {
            navigationManager.NavigateTo("./Admin/RecordFirstPayment");
        }

        private void ToPageRecordPayment()
        {
            navigationManager.NavigateTo("./Admin/NewRecordPayment");
        }

        private async Task PdfAllAsync()
        {
            if (UserProvider?.CurrentUser.CapmSelectNow == "03")
            {
                navigationManager.NavigateTo("/Admin/ExportFileLoanAgreement");
            }
            else
            {
                var StorageName = "ReportAdmin";
                await CheckDataInStorageAsync(StorageName);

                navigationManager.NavigateTo("/Admin/FilterReportAdmin");
            }
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

        public int GetCountStaffTypeV2(string[] Stype, List<ReportAdminModel> reportAdmins)
        {
            return reportAdmins.Count(c => Stype.Contains(c.StaffType));
        }

        public string SumAmountByYear(List<ReportAdminModel> reportAdmins)
        {
            decimal? totalAmount = reportAdmins.Sum(c => c.LoanAmount);

            if (totalAmount != null)
            {
                return $"{string.Format("{0:n2}", totalAmount)} ฿";
            }
            else
            {
                return "เกิดข้อผิดพลาด";
            }
        }
    }
}
