using LoanApp.Model.Models;
using LoanApp.Components.Document;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using LoanApp.Shared;
using Microsoft.EntityFrameworkCore;
using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Html;
using System.Text;
using LoanApp.Model.Helper;

namespace LoanApp.Pages.Admin
{
    public partial class PdfAgreement
    {
        [CascadingParameter] public Error Error { get; set; }

        //[Inject] IWebHostEnvironment _env { get; set; }
        //[Inject] IOptions<AppSettings> AppSettings { get; set; }

        [Parameter] public decimal RequestID { get; set; } = 0;

        private VLoanRequestContract? Agreement { get; set; } = new();
        private LoanType? LoanData { get; set; } = new();
        private ApplyLoanModel Info { get; set; } = new();
        private ApplyLoanModel ModelApplyLoan { get; set; } = new();
        private DocumentOptionModel OptionLoanAgreement { get; set; } = new();
        private VLoanStaffDetail? DebtorStaffDetail { get; set; } = new();
        private VStaffAddress DebtorStaffAssress { get; set; } = new();
        private VStaffFamily DebtorStaffFamilies { get; set; } = new();
        private VLoanStaffDetail? GuarantorStaffDetail { get; set; } = new();
        private VStaffAddress GuarantorStaffAssress { get; set; } = new();
        private VStaffFamily GuarantorStaffFamilies { get; set; } = new();
        private LoanGuarantor RefGuarantor { get; set; } = new();
        private LoanPartner RefDebtorPartner { get; set; } = new();
        private LoanPartner RefGuarantorPartner { get; set; } = new();
        private LoanAttrachment RefLoanAttrachment { get; set; } = new();
        private RequestAttrachment RefRequestAttrachment { get; set; } = new();
        private List<ListDocModel> ResultDocList { get; set; } = new();

        private string DataUser { get; set; } = string.Empty;
        private decimal? Status { get; set; } = 0;
        private string DataUserStatus { get; set; } = string.Empty;
        public string LoanAttrachmentHTML { get; set; } = string.Empty;
        public string RequestAttrachmentHTML { get; set; } = string.Empty;
        private bool IsMobile { get; set; } = false;
        private string StorageName { get; } = "BackToManageLoanRequest";
        private bool LoadingResultImg { get; set; } = false;


        protected async override Task OnInitializedAsync()
        {
            try
            {
                if (RequestID != 0)
                {
                    decimal InstallmentPaid = 0m;

                    LoanRequest? Req = await _context.LoanRequests
                    .Where(c => c.LoanRequestId == RequestID)
                    .FirstOrDefaultAsync();

                    if (Req != null)
                    {
                        DataUser = GetData(Req);
                        DataUserStatus = Data(Req);
                        InstallmentPaid = (Req.LoanInstallment != null ? Req.LoanInstallment.Value : 0);
                    }

                    Agreement = userService.GetVLoanRequestContract(RequestID);
                    SetPdfForDebtor(InstallmentPaid);
                    SetPdfForGuarantor();
                }

                var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
                if (!string.IsNullOrEmpty(checkData))
                {
                    ModelApplyLoan = await sessionStorage.GetItemAsync<ApplyLoanModel>(StorageName);
                }
                else { }
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
                IsMobile = await JS.InvokeAsync<bool>("isDevice");
                LoanAttrachmentHTML = await HtmlAsync();
                RequestAttrachmentHTML = await HtmlAsync(true);

                StateHasChanged();
            }
        }

        private async Task ToPdfBufferAsync(string html)
        {
            try
            {
                if (!string.IsNullOrEmpty(html))
                {
                    byte[] test = GeneratePDFService.GeneratePDF(html);
                    await JS.InvokeVoidAsync("jsOpenIntoNewTab", test);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private string Data(LoanRequest data)
        {
            Status = data.LoanStatusId;
            ContractStatus? step = _context.ContractStatuses
                  .Where(c => c.ContractStatusId == Status)
                  .FirstOrDefault();

            var message1 = $" [ สถานะ : {step?.ContractStatusName} ]";

            return message1;
        }

        private string GetData(LoanRequest data)
        {
            var NameUser = userService.GetFullName(data.DebtorStaffId);
            var Date = data.LoanRequestDate;

            var message = $" สัญญากู้ ของ {NameUser} ( วันที่ยื่นกู้ {dateService.ChangeDate(Date, "dd MMMM yyyy HH:mm", Utility.DateLanguage_TH)} น.) ";

            return message;
        }

        private async Task DownloadPdfAsync()
        {
            try
            {
                var fileName = $"เอกสารสัญญา.pdf";
                var html = await HtmlAsync();
                if (!string.IsNullOrEmpty(html))
                {
                    byte[] pdfBuffer = GeneratePDFService.GeneratePDF(html);
                    await GeneratePDFService.SaveFilePDFAsync(pdfBuffer, fileName);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task<string> HtmlAsync(bool IsRequestAttrachment = false)
        {
            var Html = string.Empty;
            var HtmlText = string.Empty;

            try
            {
                if (!IsRequestAttrachment)
                {
                    HtmlText = await AttrachmentPreviewAsync();
                }
                else
                {
                    HtmlText = await RequestAttrachmentPreviewAsync();
                }

                if (!string.IsNullOrEmpty(HtmlText))
                {
                    var HeadHTML = await JS.InvokeAsync<string>("headHTML");
                    var ScriptHTML = await JS.InvokeAsync<string>("scriptHTML");
                    Html = $"{HeadHTML} <br/> {HtmlText} <br/> {ScriptHTML}";
                }
                return Html;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SetPdfForDebtor(decimal InstallmentPaid)
        {
            try
            {
                LoanData = userService.GetLoanType(Agreement?.LoanTypeId);
                DebtorStaffDetail = userService.GetUserDetail(Agreement?.DebtorStaffId);

                var Address = userService.GetUserAddresses(Agreement?.DebtorStaffId);
                var Families = _context.VStaffFamilies.Where(c => c.StaffId.Equals(Agreement.DebtorStaffId)).FirstOrDefault();

                if (Address != null)
                {
                    DebtorStaffAssress = Address;
                }
                var ContractLoanAmount = 0;
                if (Agreement?.ContractLoanAmount != null)
                {
                    ContractLoanAmount = (int)Agreement.ContractLoanAmount;
                }
                if (Families != null)
                {
                    DebtorStaffFamilies = Families;
                }
                Info.SalaryNetAmount = Agreement?.SalaryNetAmount;
                Info.LoanAmount = ContractLoanAmount;
                Info.LoanNumInstallments = Agreement?.LoanRequestNumInstallments != null ?
                    (int)Agreement.LoanRequestNumInstallments :
                    0;
                Info.LoanMonthlyInstallment = InstallmentPaid;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SetPdfForGuarantor()
        {
            try
            {
                var GuarantorStaffId = Agreement?.LoanRequestGuaranStaffId;
                if (Agreement?.ContractGuarantorStaffId != null)
                {
                    GuarantorStaffId = Agreement.ContractGuarantorStaffId;
                }

                GuarantorStaffDetail = userService.GetUserDetail(GuarantorStaffId);

                var Address = userService.GetUserAddresses(GuarantorStaffId);
                var Families = _context.VStaffFamilies.Where(c => c.StaffId.Equals(GuarantorStaffId)).FirstOrDefault();
                if (Address != null)
                {
                    GuarantorStaffAssress = Address;
                }
                if (Families != null)
                {
                    GuarantorStaffFamilies = Families;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<string> AttrachmentPreviewAsync()
        {
            var HtmlText = string.Empty;
            var DebtorPartner = string.Empty;
            var GuarantorPartner = string.Empty;

            try
            {
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
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<string> RequestAttrachmentPreviewAsync()
        {
            try
            {
                var HtmlText = string.Empty;
                if (RefRequestAttrachment != null)
                {
                    HtmlText = await RefRequestAttrachment.GetBoByHtmlAsync();
                }
                return HtmlText;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Back()
        {
            navigationManager.NavigateTo($"Admin/ManageLoanRequest/{true}/{ModelApplyLoan.LoanTypeID}/{ModelApplyLoan.ContractStatusId}");
        }

        private void SeeDetail()
        {
            navigationManager.NavigateTo($"/Admin/UploadAdmin/{RequestID}");
        }

        private async Task PrintPdfAsync(string html)
        {
            var fileName = $"เอกสารสัญญา.pdf";
            try
            {
                if (!string.IsNullOrEmpty(html))
                {
                    byte[] pdfBuffer = GeneratePDFService.GeneratePDF(html);
                    await GeneratePDFService.SaveFilePDFAsync(pdfBuffer, fileName);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task GetDocStap1Async()
        {
            LoadingResultImg = true;
            try
            {
                VLoanRequestContract? ReqCon = await _context.VLoanRequestContracts
               .Where(c => c.LoanRequestId == RequestID)
               .FirstOrDefaultAsync();

                var stap1 = await SaveFileAndImgService.GetDocByStapAsync(1, ReqCon);
                if (stap1 != null)
                {
                    ResultDocList.Add(stap1);
                }
                LoadingResultImg = false;
            }
            catch (Exception ex)
            {
                LoadingResultImg = false;
                await Error.ProcessError(ex);
            }
            StateHasChanged();
        }
    }
}
