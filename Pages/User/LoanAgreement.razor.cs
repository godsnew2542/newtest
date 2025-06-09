using LoanApp.Components.Document;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using AntDesign.TableModels;
using LoanApp.Model.Helper;
using LoanApp.Pages.Admin;

namespace LoanApp.Pages.User
{
    public partial class LoanAgreement
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #endregion

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<VLoanRequestContract> ListLoanNow { get; set; } = new();
        private List<VLoanRequestContract> ListLoanSuccess { get; set; } = new();
        private LoanType? LoanData { get; set; } = new();
        private VLoanStaffDetail? DebtorStaffDetail { get; set; } = new();
        private ApplyLoanModel Info { get; set; } = new();
        private VStaffAddress DebtorStaffAssress { get; set; } = new();
        private VLoanStaffDetail? GuarantorStaffDetail { get; set; } = new();
        private VStaffAddress GuarantorStaffAssress { get; set; } = new();
        private VStaffFamily DebtorStaffFamilies { get; set; } = new();
        private VStaffFamily GuarantorStaffFamilies { get; set; } = new();
        private DocumentOptionModel OptionLoanAgreement { get; set; } = new();
        private LoanAttrachment? RefLoanAttrachment { get; set; }
        private LoanGuarantor? RefGuarantor { get; set; }
        private LoanPartner? RefDebtorPartner { get; set; }
        private LoanPartner? RefGuarantorPartner { get; set; }
        private List<VLoanRequestContract> LoanAgreementDebtor { get; set; } = new();
        private List<VLoanStaffDetail> ListStaffIdOld { get; set; } = new();


        private Boolean LoanCheckbook { get; set; } = false;
        private decimal[] StatusIdLoanNow { get; } = new[] { 0m, 100m, 3m, 98m, 99m };
        private decimal[] StatusIdLoanSuccess { get; } = new[] { 3m, 98m, 99m };
        private string FormathDate { get; set; } = "dd-MM-yyyy";
        private string FormathTime { get; set; } = "HH:mm";
        private string? LoanAttrachmentHTML { get; set; } = string.Empty;
        private bool IsMobile { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            string StaffID = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
            try
            {
                if (!string.IsNullOrEmpty(StaffID))
                {
                    ListLoanNow = await _context.VLoanRequestContracts
                        .Where(c => c.DebtorStaffId!.Equals(StaffID))
                        .Where(c => !StatusIdLoanNow.Contains(c.CurrentStatusId!.Value))
                        .Where(c => (c.ContractDate == null) ||
                        ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                        .OrderBy(c => c.StatusUserRequestOrder != null)
                        .OrderBy(c => c.StatusUserRequestOrder)
                        .ToListAsync();

                    ListLoanSuccess = await _context.VLoanRequestContracts
                        .Where(c => c.DebtorStaffId == StaffID)
                        .Where(c => StatusIdLoanSuccess.Contains(c.CurrentStatusId!.Value))
                        .Where(c => (c.ContractDate == null) ||
                        ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                        .OrderBy(c => c.StatusUserRequestOrder != null)
                        .OrderBy(c => c.StatusUserRequestOrder)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (StateProvider != null && StateProvider.CurrentUser.CapmSelectNow == null)
                {
                    VLoanStaffDetail? vLoanStaffDetail = await psuLoan.GetUserDetailAsync(StateProvider?.CurrentUser.StaffId);

                    if (vLoanStaffDetail != null)
                    {
                        ListStaffIdOld = await psuLoan.GetListStaffIdByStaffPersId(staffPersId: vLoanStaffDetail.StaffPersId, staffId: StateProvider?.CurrentUser.StaffId, isRemoveStaffIdNow: true);

                        if (ListStaffIdOld.Any())
                        {
                            LoanAgreementDebtor = await psuLoan.GetAllLoanForDebtorOnly(staffIds: ListStaffIdOld.Select(x => x.StaffId).ToList());
                        }
                    }
                }

                IsMobile = await JS.InvokeAsync<bool>("isDevice");
                StateHasChanged();
            }
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
                        $"<div class='page-break'></div> <br/>" +
                        $"{DebtorPartner}";
                }
            }

            if (RefGuarantor != null)
            {
                var GuarantorIT_Text = await RefGuarantor.GetBoByHtmlAsync();
                HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div> <br/>" +
                        $"{GuarantorIT_Text}";
            }

            if (GuarantorStaffDetail?.MarriedId == "2")
            {
                if (RefGuarantorPartner != null)
                {
                    GuarantorPartner = await RefGuarantorPartner.GetBoByHtmlAsync();
                    HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div> <br/>" +
                        $"{GuarantorPartner}";
                }
            }
            return HtmlText;
        }

        private void UploadPayment(VLoanRequestContract agreement)
        {
            navigationManager.NavigateTo($"/AgreementDetailPage/{agreement.LoanRequestId}");
        }

        private void SeeDetail(RowData<VLoanRequestContract> row)
        {
            UploadPayment(row.Data);
        }

        private void UploadAgreementPremise(VLoanRequestContract req, bool EditFile = false)
        {
            navigationManager.NavigateTo($"/AgreementPremise/{req.LoanRequestId}/{3}{(EditFile ? $"/{EditFile}" : null)}");
        }

        private async Task ChooseDateAsync(VLoanRequestContract req)
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

        private async Task PrintAgreementAsync(VLoanRequestContract agreement)
        {
            SetAgreement(agreement);
            LoanAttrachmentHTML = await HtmlAsync();
        }

        private async Task DownloadPdfAsync(VLoanRequestContract agreement)
        {
            SetAgreement(agreement);
            var html = await HtmlAsync();
            await PrintPdfAsync(html);
        }

        private async Task PrintPdfAsync(string html)
        {
            var fileName = $"เอกสารสัญญา.pdf";
            if (!string.IsNullOrEmpty(html))
            {
                byte[] pdfBuffer = GeneratePDFService.GeneratePDF(html);
                await GeneratePDFService.SaveFilePDFAsync(pdfBuffer, fileName);
            }
        }

        private void SetAgreement(VLoanRequestContract agreement)
        {
            OptionLoanAgreement.DateTitle = dateService.ConvertToDateTime(agreement.ContractDate);
            LoanData = new();
            DebtorStaffDetail = new();
            Info = new();
            DebtorStaffAssress = new();
            DebtorStaffFamilies = new();
            GuarantorStaffFamilies = new();

            LoanAttrachmentHTML = string.Empty;

            SetPdfForDebtor(agreement);
            SetPdfForGuarantor(agreement);
        }

        private void SetPdfForDebtor(VLoanRequestContract Agreement)
        {
            LoanData = userService.GetLoanType(Agreement.LoanTypeId);
            DebtorStaffDetail = userService.GetUserDetail(Agreement.DebtorStaffId);

            var Address = userService.GetUserAddresses(Agreement.DebtorStaffId);
            var Families = _context.VStaffFamilies
                .Where(c => c.StaffId.Equals(Agreement.DebtorStaffId))
                .FirstOrDefault();
            if (Address != null)
            {
                DebtorStaffAssress = Address;
            }
            var ContractLoanAmount = 0;
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
            Info.LoanMonthlyInstallment = Agreement.LoanRequestLoanInstallment;
        }

        private void SetPdfForGuarantor(VLoanRequestContract Agreement)
        {
            var GuarantorStaffId = Agreement.LoanRequestGuaranStaffId;
            if (Agreement.ContractGuarantorStaffId != null)
            {
                GuarantorStaffId = Agreement.ContractGuarantorStaffId;
            }
            GuarantorStaffDetail = userService.GetUserDetail(GuarantorStaffId);

            var Address = userService.GetUserAddresses(GuarantorStaffId);
            var Families = _context.VStaffFamilies
                .Where(c => c.StaffId.Equals(GuarantorStaffId))
                .FirstOrDefault();

            if (Address != null)
            {
                GuarantorStaffAssress = Address;
            }
            if (Families != null)
            {
                GuarantorStaffFamilies = Families;
            }
        }

        private async Task TopageLoanAgreementOld()
        {
            LoanAgreementOldModel oldModel = new()
            {
                StaffId = StateProvider?.CurrentUser.StaffId,
                StaffList = ListStaffIdOld.Select(x => x.StaffId).ToList(),
                DebtorRequestId = LoanAgreementDebtor.Select(x => x.LoanRequestId).ToList(),
                //GuaranRequestId = guaran.Select(x => x.LoanRequestId).ToList(),
                IsShowDebtorSuccess = true,
                //IsShowGuaranSuccess = true,
            };
            await sessionStorage.SetItemAsync("OldAgreement", oldModel);

            navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Staff}/LoanAgreementOld/{(int)BackRootPageEnum.User_LoanAgreement}");
        }
    }
}
