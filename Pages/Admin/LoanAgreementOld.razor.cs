using AntDesign.TableModels;
using LoanApp.Components.Document;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Org.BouncyCastle.Asn1.Ocsp;


namespace LoanApp.Pages.Admin
{
    public partial class LoanAgreementOld
    {
        #region Parameter
        /// <summary>
        /// LoanApp.Model.Models.RoleTypeEnum
        /// </summary>
        [Parameter] public int Role { get; set; } = 0;
        /// <summary>
        /// LoanApp.Model.Models.BackRootPageEnum
        /// </summary>
        [Parameter] public int BackToPage { get; set; } = 0;
        #endregion

        #region Inject
        [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoanDb { get; set; } = null!;

        #endregion

        #region class
        private class DisplayAgreementOldModel
        {
            public string StaffId { get; set; } = null!;
            public ProgressAgreement? AgreementDebtor { get; set; } = null;
            public ProgressAgreement? AgreementGuaran { get; set; } = null;
        }

        private class ProgressAgreement
        {
            /// <summary>
            /// กำลัง กู้/ค้ำ
            /// </summary>
            public List<VLoanRequestContract> Progress { get; set; } = new();

            /// สิ้นสุด กู้/ค้ำ
            public List<VLoanRequestContract> Success { get; set; } = new();
        }
        #endregion

        private LoanAgreementOldModel? AgreementOld { get; set; } = null;
        private List<DisplayAgreementOldModel> DisplayAgreements { get; set; } = new();
        private DocumentOptionModel OptionLoanAgreement { get; set; } = new();
        private LoanType? LoanData { get; set; } = new();
        private VLoanStaffDetail? DebtorStaffDetail { get; set; } = new();
        private ApplyLoanModel Info { get; set; } = new();
        private VStaffAddress DebtorStaffAssress { get; set; } = new();
        private VStaffFamily DebtorStaffFamilies { get; set; } = new();
        private VStaffFamily GuarantorStaffFamilies { get; set; } = new();
        private VLoanStaffDetail? GuarantorStaffDetail { get; set; } = new();
        private VStaffAddress GuarantorStaffAssress { get; set; } = new();
        private VLoanStaffDetail? StaffDetail { get; set; } = new();
        private VStaffFamily StaffFamily { get; set; } = new();
        private MonthModel model_month { get; set; } = new();

        private const string SessionKey = "OldAgreement";
        private string FormathDate { get; set; } = "dd-MM-yyyy";
        private string FormathTime { get; set; } = "HH:mm";
        private bool IsMobile { get; set; } = false;
        private string? LoanAttrachmentHTML { get; set; } = string.Empty;
        private List<decimal> AgreementStatusActive { get; } = new() { 6, 7, 8, 9, 80, 81, 82, 200 };

        private bool loading { get; set; } = true;

        private LoanAttrachment? RefLoanAttrachment { get; set; }
        private LoanPartner? RefDebtorPartner { get; set; }
        private LoanGuarantor? RefGuarantor { get; set; }
        private LoanPartner? RefGuarantorPartner { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    IsMobile = await JS.InvokeAsync<bool>("isDevice");

                    AgreementOld = await sessionStorage.GetItemAsync<LoanAgreementOldModel?>(SessionKey);

                    StaffDetail = await psuLoanDb.GetUserDetailAsync(AgreementOld?.StaffId);

                    if (AgreementOld != null && AgreementOld.StaffList.Any())
                    {
                        List<VLoanRequestContract> loanAgreementDebtor = new();
                        List<VLoanRequestContract> loanAgreementGuaran = new();

                        if (AgreementOld.DebtorRequestId.Any() && AgreementOld.IsShowDebtorOnly)
                        {
                            loanAgreementDebtor = await psuLoanDb.GetListVLoanRequestContractByRequestIds(AgreementOld.DebtorRequestId);
                        }

                        if (AgreementOld.GuaranRequestId.Any() && AgreementOld.IsShowGuaranOnly)
                        {
                            loanAgreementGuaran = await psuLoanDb.GetListVLoanRequestContractByRequestIds(AgreementOld.GuaranRequestId);
                        }

                        foreach (var item in AgreementOld.StaffList)
                        {
                            var debtor = GetDebtorLoanAgreementByStaffId(item, loanAgreementDebtor, AgreementOld.IsShowDebtorSuccess);

                            var guaran = GetGuarantorLoanAgreementByStaffId(item, loanAgreementGuaran, AgreementOld.IsShowGuaranSuccess);

                            DisplayAgreementOldModel display = new()
                            {
                                StaffId = item,
                                AgreementDebtor = new()
                                {
                                    Progress = debtor.Item1,
                                    Success = debtor.Item2
                                },
                                AgreementGuaran = new()
                                {
                                    Progress = guaran.Item1,
                                    Success = guaran.Item2
                                },
                            };

                            DisplayAgreements.Add(display);
                        }
                    }

                    loading = false;
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    loading = false;
                    StateHasChanged();

                    await notificationService.Error(notificationService.ExceptionLog(ex));
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="staffId"></param>
        /// <param name="agreement"></param>
        /// <param name="isShowDebtorSuccess"></param>
        /// <returns>สัญญากำลังกู้, สัญญาสิ้นสุดการกู้</returns>
        private Tuple<List<VLoanRequestContract>, List<VLoanRequestContract>> GetDebtorLoanAgreementByStaffId(string staffId, List<VLoanRequestContract> agreement, bool isShowSuccess)
        {
            Tuple<List<VLoanRequestContract>, List<VLoanRequestContract>> concatenatedList = new(new(), new());
            if (agreement.Any())
            {
                List<decimal?> successStatusAgreement = new() { 3, 98, 99 };

                List<VLoanRequestContract> yy = agreement
                    .Where(c => c.DebtorStaffId == staffId)
                    .ToList();

                var progress = yy
                    .Where(c => !successStatusAgreement.Contains(c.CurrentStatusId))
                    .ToList();

                List<VLoanRequestContract> success = new();
                if (isShowSuccess)
                {
                    success = yy
                        .Where(c => successStatusAgreement.Contains(c.CurrentStatusId))
                        .ToList();
                }

                concatenatedList = Tuple.Create(progress, success);
                return concatenatedList;
            }
            return concatenatedList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="staffId"></param>
        /// <param name="agreement"></param>
        /// <returns>สัญญากำลังค้ำ, สัญญาสิ้นสุดการค้ำ</returns>
        private Tuple<List<VLoanRequestContract>, List<VLoanRequestContract>> GetGuarantorLoanAgreementByStaffId(string staffId, List<VLoanRequestContract> agreement, bool isShowSuccess)
        {
            Tuple<List<VLoanRequestContract>, List<VLoanRequestContract>> concatenatedList = new(new(), new());

            if (agreement.Any())
            {
                List<decimal?> successStatusAgreement = new() { 3, 98, 99 };

                List<VLoanRequestContract> yy = agreement
                    .Where(c => c.ContractGuarantorStaffId == staffId || c.LoanRequestGuaranStaffId == staffId)
                    .ToList();

                var progress = yy.Where(c => !successStatusAgreement.Contains(c.CurrentStatusId)).ToList();

                List<VLoanRequestContract> success = new();
                if (isShowSuccess)
                {
                    success = yy
                        .Where(c => successStatusAgreement.Contains(c.CurrentStatusId))
                        .ToList();
                }

                concatenatedList = Tuple.Create(progress, success);
                return concatenatedList;
            }
            return concatenatedList;
        }

        private async Task ChooseDateAsync(VLoanRequestContract req)
        {
            var Detail = _context.LoanStaffDetails
                    .Where(c => c.StaffId == req.DebtorStaffId)
                    .FirstOrDefault();

            if (Detail != null)
            {
                navigationManager.NavigateTo($"/{(RoleTypeEnum)Role}/ChooseDate/{req.DebtorStaffId}/{req.LoanRequestId}");
            }
            else
            {
                string alert = $"กรุณาระบุหมายเลขบัญชีธนาคารที่ต้องการรับเงินกู้ยืม ที่เมนู 'ข้อมูลส่วนตัว'";
                await notificationService.Error(alert);
            }
        }

        private async Task DownloadPdfAsync(VLoanRequestContract agreement)
        {
            SetAgreement(agreement);
            var html = await HtmlAsync();
            await PrintPdfAsync(html);
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

        private async Task PrintPdfAsync(string html)
        {
            var fileName = $"เอกสารสัญญา.pdf";
            if (!string.IsNullOrEmpty(html))
            {
                byte[] pdfBuffer = GeneratePDFService.GeneratePDF(html);
                await GeneratePDFService.SaveFilePDFAsync(pdfBuffer, fileName);
            }
        }

        private async Task PrintAgreementAsync(VLoanRequestContract agreement)
        {
            SetAgreement(agreement);
            LoanAttrachmentHTML = await HtmlAsync();
        }

        private void UploadAgreementPremise(VLoanRequestContract req)
        {
            // true == status 200
            bool? IsNewupload = null;

            if (req.CurrentStatusId != null)
            {
                IsNewupload = req.CurrentStatusId == 6 ? false : req!.CurrentStatusId == 200 ? true : null;
            }

            if (IsNewupload == true)
            {
                navigationManager.NavigateTo($"/{(RoleTypeEnum)Role}/AgreementPremise/{req.LoanRequestId}/{3}/{(int)PageControl.LoanAgreementOld}");
            }
            else if (IsNewupload == false)
            {
                navigationManager.NavigateTo($"/{(RoleTypeEnum)Role}/AgreementPremise/{req.LoanRequestId}/{3}/{(int)PageControl.LoanAgreementOld}/{IsNewupload}");
            }
        }

        private void DetailPage(VLoanRequestContract agreement)
        {
            navigationManager.NavigateTo($"/{Role}/AgreementDetailPage/{(int)PageControl.AdminCheckAgreement}/{agreement.DebtorStaffId}/{agreement.LoanRequestId}/{(int)BackRootPageEnum.LoanAgreementOld}/{0}");
        }

        private void DetailGuarantPage(VLoanRequestContract agreement)
        {
            navigationManager.NavigateTo($"/{Role}/GuarantDetailPage/{(int)PageControl.AdminCheckGurantorAgreement}/{agreement.DebtorStaffId}/{agreement.LoanRequestId}/{(int)BackRootPageEnum.LoanAgreementOld}/{0}");
        }
        private string ChangeDate(string? StringDate, string[] selectMonth)
        {
            var ShowDate = string.Empty;
            DateModel Date = Utility.ChangeDateMonth(StringDate, selectMonth);

            if (!string.IsNullOrEmpty(Date.Day))
            {
                ShowDate = $"{Date.Day} {Date.Month} {Date.Year}";
            }
            return ShowDate;
        }

        private decimal SumAmount(List<VLoanRequestContract> reqCon)
        {
            decimal Amount = 0;

            if (reqCon.Any())
            {
                foreach (var LoanAmount in reqCon)
                {
                    if (LoanAmount.ContractLoanAmount != null)
                    {
                        Amount += LoanAmount.ContractLoanAmount.Value;
                    }
                }
            }

            return Amount;
        }

        private async Task<decimal> BalanceTotalAsync(List<VLoanRequestContract> reqCon)
        {
            decimal Balance = 0;

            if (reqCon.Any())
            {
                for (int i = 0; i < reqCon.Count; i++)
                {
                    var data = reqCon[i];

                    PaymentTransaction? Transactions = await _context.PaymentTransactions
                        .Where(c => c.ContractId == data.ContractId)
                        .Select(c => new PaymentTransaction
                        {
                            ContractId = c.ContractId,
                            InstallmentNo = c.InstallmentNo,
                            BalanceAmount = c.BalanceAmount,
                        })
                        .Distinct()
                        .OrderByDescending(c => c.InstallmentNo)
                        .FirstOrDefaultAsync();

                    if (Transactions?.BalanceAmount != null)
                    {
                        Balance += Transactions.BalanceAmount.Value;
                    }
                    else
                    {
                        if (data.ContractLoanAmount != null)
                        {
                            Balance += data.ContractLoanAmount.Value;
                        }
                    }
                }
            }

            return Balance;
        }

        private void BackPage()
        {
            switch (Role)
            {
                case (int)RoleTypeEnum.Staff:

                    if (BackToPage == (int)BackRootPageEnum.User_LoanAgreement)
                    {
                        navigationManager.NavigateTo($"/LoanAgreement");
                        return;
                    }
                    else if (BackToPage == (int)BackRootPageEnum.User_Guarantor)
                    {
                        navigationManager.NavigateTo($"/Guarantor");
                        return;
                    }

                    navigationManager.NavigateTo($"/HomeUser");
                    return;

                default:
                    navigationManager.NavigateTo($"{(RoleTypeEnum)Role}/CheckLoanpage/{AgreementOld?.StaffId}");
                    return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="agreementAction">1 ผู้กู้ || 2 ผู้ค้ำ</param>
        private void SeeDetail(RowData<VLoanRequestContract> row, int agreementAction)
        {
            switch (agreementAction)
            {
                case 1:
                    DetailPage(row.Data);
                    break;

                case 2:
                    DetailGuarantPage(row.Data);
                    break;
            }
        }
    }

    public class LoanAgreementOldModel
    {
        /// <summary>
        /// id หลักที่ใช่งานได้ ณ ปัจจุบัน
        /// </summary>
        public string? StaffId { get; set; }
        public List<string> StaffList { get; set; } = new();
        /// <summary>
        /// List Id สัญญา ผู้กู้
        /// </summary>
        public List<decimal> DebtorRequestId { get; set; } = new();
        /// <summary>
        /// List Id สัญญา ผู้ค้ำ
        /// </summary>
        public List<decimal> GuaranRequestId { get; set; } = new();

        /// <summary>
        /// แสดงสัญญากู้ defult  true ใช่
        /// </summary>
        public bool IsShowDebtorOnly { get; set; } = true;
        /// <summary>
        /// แสดงสัญญาค้ำ defult  true ใช่
        /// </summary>
        public bool IsShowGuaranOnly { get; set; } = true;

        /// <summary>
        /// แสดงสัญญากู้ที่สิ้นสุดแล้ว defult false ไม่ใช่
        /// </summary>
        public bool IsShowDebtorSuccess { get; set; } = false;
        /// <summary>
        /// แสดงสัญญาค้ำที่สิ้นสุดแล้ว defult false ไม่ใช่
        /// 
        /// </summary>
        public bool IsShowGuaranSuccess { get; set; } = false;
    }
}
