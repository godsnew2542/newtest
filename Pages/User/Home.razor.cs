using LoanApp.Components.Document;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace LoanApp.Pages.User
{
    public partial class Home
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        #region Ref Doc
        private LoanAttrachment RefLoanAttrachment { get; set; } = null!;
        private LoanGuarantor RefGuarantor { get; set; } = null!;
        private LoanPartner RefDebtorPartner { get; set; } = null!;
        private LoanPartner RefGuarantorPartner { get; set; } = null!;

        #endregion

        private List<VLoanRequestContract> RequestList { get; set; } = new();
        private List<List<VLoanRequestContract>> ListOverview { get; set; } = new();
        private LoanType? LoanData { get; set; } = new();
        private ApplyLoanModel Info { get; set; } = new();
        private VLoanStaffDetail? DebtorStaffDetail { get; set; } = new();
        private VStaffAddress DebtorStaffAssress { get; set; } = new();
        private VStaffFamily DebtorStaffFamilies { get; set; } = new();
        private VStaffFamily GuarantorStaffFamilies { get; set; } = new();
        private VLoanStaffDetail? GuarantorStaffDetail { get; set; } = new();
        private VStaffAddress GuarantorStaffAssress { get; set; } = new();
        private DocumentOptionModel OptionLoanAgreement { get; set; } = new();

        private decimal[] StatusforCount { get; } = new[] { 6m, 7m, 8m, 80m, 81m, 82m, 9m, 200m };
        private decimal[] RequestStatusId { get; } = new[] { 2m, 4m, 6m, 200m }; // { 2m, 4m, 6m };
        private decimal[] OverviewStatusId { get; } = new[] { 0m, 1m, 3m, 99m, 100m };
        private decimal[] AllowedStatus { get; set; } = new[] { 0m, 3m, 99m, 98m, 100m };
        private int Notification { get; set; } = 0;
        private string? StaffID { get; set; } = string.Empty;
        private string LoanAttrachmentHTML { get; set; } = string.Empty;
        private bool IsMobile { get; set; } = false;

        protected override void OnInitialized()
        {
            StaffID = StateProvider?.CurrentUser.StaffId;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (!string.IsNullOrEmpty(StaffID))
                    {
                        RequestList = await _context.VLoanRequestContracts
                            .Where(c => RequestStatusId.Contains(c.CurrentStatusId!.Value))
                            .Where(c => c.DebtorStaffId == StaffID)
                            .ToListAsync();
                    }

                    IsMobile = await JS.InvokeAsync<bool>("isDevice");
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }

            }
        }

        private void TopageAgreement()
        {
            navigationManager.NavigateTo("/LoanAgreement");
        }

        private async Task ChoosedateAsync(VLoanRequestContract req)
        {
            var Detail = _context.LoanStaffDetails
                    .Where(c => c.StaffId == req.DebtorStaffId)
                    .FirstOrDefault();
            if (Detail != null)
            {
                navigationManager.NavigateTo($"/User/ChooseDate/{req.LoanRequestId}");
            }
            else
            {
                string alert = $"กรุณาระบุหมายเลขบัญชีธนาคารที่ต้องการรับเงินกู้ยืม ที่เมนู 'ข้อมูลส่วนตัว'";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
            }
        }

        private void UploadAgreementPremise(VLoanRequestContract req, bool EditFile = false)
        {
            navigationManager.NavigateTo($"/AgreementPremise/{req.LoanRequestId}/{3}{(EditFile ? $"/{EditFile}" : null)}");
        }

        private string GetHeaderTitle(LoanType? loan, decimal? loanAmount, string otherStart = "", string otherEnd = "")
        {
            var text = $"{otherStart} {UserService.GetLoanName(loan)} [จำนวน {@String.Format("{0:n2}", loanAmount)} บาท] {otherEnd}";
            return text;
        }

        private async Task DownloadPdfAsync(VLoanRequestContract agreement)
        {
            await SetAgreementAsync(agreement);
            var html = await HtmlAsync();
            /* await PrintPdfAsync(html);*/
        }

        private async Task PrintAgreementAsync(VLoanRequestContract agreement)
        {
            await SetAgreementAsync(agreement);
            LoanAttrachmentHTML = await HtmlAsync();
        }

        private async Task SetAgreementAsync(VLoanRequestContract agreement)
        {
            LoanAttrachmentHTML = string.Empty;
            OptionLoanAgreement.DateTitle = dateService.ConvertToDateTime(agreement.ContractDate);
            LoanData = new();
            DebtorStaffDetail = new();
            Info = new();
            DebtorStaffAssress = new();
            DebtorStaffFamilies = new();
            GuarantorStaffFamilies = new();

            await SetPdfForDebtorAsync(agreement);
            await SetPdfForGuarantorAsync(agreement);
        }

        private async Task<string> HtmlAsync()
        {
            var Html = string.Empty;

            await Task.Delay(2000);
            var HtmlText = await AttrachmentPreviewAsync();

            if (!string.IsNullOrEmpty(HtmlText))
            {
                var HeadHTML = await JS.InvokeAsync<string>("headHTML");
                var ScriptHTML = await JS.InvokeAsync<string>("scriptHTML");
                Html = $"{HeadHTML} <br/> {HtmlText} <br/> {ScriptHTML}";
            }
            return Html;
        }

        private async Task SetPdfForDebtorAsync(VLoanRequestContract Agreement)
        {
            LoanData = await psuLoan.GetLoanTypeAsync(Agreement.LoanTypeId);
            DebtorStaffDetail = await psuLoan.GetUserDetailAsync(Agreement.DebtorStaffId);

            var ContractLoanAmount = 0;
            var Address = await psuLoan.GetUserAddressesAsync(Agreement.DebtorStaffId);
            var Families = await psuLoan.GetUserFamilyAsync(Agreement.DebtorStaffId);

            if (Address != null)
            {
                DebtorStaffAssress = Address;
            }
            if (Agreement.ContractLoanAmount != null)
            {
                ContractLoanAmount = (int)Agreement.ContractLoanAmount;
            }
            if (Families != null)
            {
                DebtorStaffFamilies = Families;
            }

            Info.SalaryNetAmount = Agreement.SalaryNetAmount;
            Info.LoanAmount = ContractLoanAmount;
            Info.LoanNumInstallments = (Agreement.LoanRequestNumInstallments != null ?
                (int)Agreement.LoanRequestNumInstallments : 0);
            Info.LoanTypeID = Agreement.LoanTypeId;
            Info.LoanMonthlyInstallment = Agreement.LoanRequestLoanInstallment;

        }

        private async Task SetPdfForGuarantorAsync(VLoanRequestContract Agreement)
        {
            string? GuarantorStaffId = Agreement.LoanRequestGuaranStaffId;
            if (Agreement.ContractGuarantorStaffId != null)
            {
                GuarantorStaffId = Agreement.ContractGuarantorStaffId;
            }

            GuarantorStaffDetail = await psuLoan.GetUserDetailAsync(GuarantorStaffId);

            var Address = await psuLoan.GetUserAddressesAsync(GuarantorStaffId);
            var Families = await psuLoan.GetUserFamilyAsync(GuarantorStaffId);

            if (Address != null)
            {
                GuarantorStaffAssress = Address;
            }
            if (Families != null)
            {
                GuarantorStaffFamilies = Families;
            }
        }

        private async Task<string> AttrachmentPreviewAsync()
        {
            var HtmlText = string.Empty;
            var DebtorPartner = string.Empty;
            var GuarantorPartner = string.Empty;

            if (RefLoanAttrachment != null)
            {
                HtmlText = await RefLoanAttrachment.GetBoByHtmlAsync();
            }

            if (DebtorStaffDetail?.MarriedId == "2")
            {
                if (RefDebtorPartner != null)
                {
                    DebtorPartner = await RefDebtorPartner.GetBoByHtmlAsync();
                    HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div>" +
                        $"{DebtorPartner}";
                }
            }

            if (RefGuarantor != null)
            {
                var GuarantorIT_Text = await RefGuarantor.GetBoByHtmlAsync();
                HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div>" +
                        $"{GuarantorIT_Text}";
            }

            if (GuarantorStaffDetail?.MarriedId == "2")
            {
                if (RefGuarantorPartner != null)
                {
                    GuarantorPartner = await RefGuarantorPartner.GetBoByHtmlAsync();
                    HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div>" +
                        $"{GuarantorPartner}";
                }
            }
            return HtmlText;
        }

        private string SetTag(LoanType? loan)
        {
            LoanWithSetDateModel model = new();

            string mess = "กรุณานัดหมายทำสัญญาภายใน 30 วัน";
            if (loan != null && model.LoanType.Contains(loan.LoanTypeId))
            {
                var Max = model.Day.Max(t => t);
                var Min = model.Day.Min(t => t);
                mess = $"กรุณานัดหมายทำสัญญาภายในวันที่ {Min} - {Max} เท่านั้น";
            }
            return mess;
        }
    }
}
