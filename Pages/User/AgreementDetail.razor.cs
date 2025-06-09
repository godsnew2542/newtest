using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace LoanApp.Pages.User
{
    public partial class AgreementDetail
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        #region Parameter
        /// <summary>
        /// RequestID ที่นำมาค้นข้อมูลต่างๆ เพื่อแสดง ใน page นี้
        /// </summary>
        [Parameter] public decimal RequestID { get; set; } = 0;
        /// <summary>
        /// ขั้นตอนการส่งเอกสาร {DB: PSU_LOAN.CONTRACT_STEP}
        /// </summary>
        [Parameter] public decimal StepID { get; set; } = 0;
        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public int FromPage { get; set; } = 0;
        [Parameter] public string Role { get; set; } = string.Empty;
        /// <summary>
        /// RoleTypeEnum
        /// </summary>
        [Parameter] public int newRole { get; set; } = 0;
        /// <summary>
        /// LoanApp.Models.BackRootPageEnum
        /// </summary>
        [Parameter] public int rootPage { get; set; } = 0;
        /// <summary>
        /// RequestID ต้นทางที่จะสามารถกลับไปยังหน้าเดิมได้
        /// </summary>
        [Parameter] public decimal rootRequestID { get; set; } = 0;

        #endregion

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;


        #endregion

        private List<ListDocModel> ResultDocList { get; set; } = new();
        private VLoanRequestContract? Request { get; set; } = new();
        private TransactionDocModel transactionDocModel { get; set; } = new();
        private ImgOtherModel imgOtherModel { get; set; } = new();

        private string[] DayInstallments { get; set; } = new string[] { };
        private bool IsMobile { get; set; } = false;
        private string? HrefPath { get; set; } = null;
        private bool loading { get; set; } = true;
        private bool LoadingResultImg { get; set; } = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                loading = true;
                transactionDocModel.IsShowTransactionDoc = false;

                try
                {
                    if (RequestID != 0)
                    {
                        Request = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);
                        if (Request != null)
                        {
                            DateTime? PaidDate = null;
                            if (Request?.PaidDate != null)
                            {
                                PaidDate = Request?.PaidDate;
                            }
                            else
                            {
                                PaidDate = Request?.ContractDate;
                            }

                            DayInstallments = await TransactionService.SetDateWithPaymentTransactionAsync(Request?.ContractId,
                                Request?.ContractLoanNumInstallments,
                                PaidDate);

                            await CheckTransactionDocAsync(Request);
                            imgOtherModel = await psuLoan.CheckFileUploadOther(Request);
                        }

                    }

                    IsMobile = await JS.InvokeAsync<bool>("isDevice");
                    StateHasChanged();

                    if (IsMobile)
                    {
                        HrefPath = GeneratePDFService.DownloadPdf(RequestID);
                    }

                    loading = false;
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    loading = false;
                    StateHasChanged();
                    await Error.ProcessError(ex);
                }
            }
        }

        private async Task<string> GetNameGuarantorAsync(VLoanRequestContract ReqCon)
        {
            string? staffID = string.Empty;
            string mess = string.Empty;

            staffID = (!string.IsNullOrEmpty(ReqCon.ContractGuarantorStaffId) ?
                ReqCon.ContractGuarantorStaffId :
                ReqCon.LoanRequestGuaranStaffId);

            if (!string.IsNullOrEmpty(staffID))
            {
                mess = $"{userService.GetFullName(staffID)} ({await GetFacNameAsync(staffID)})";
            }
            return mess;
        }

        private string GetLoanAmount(VLoanRequestContract? ReqCon)
        {
            string mess = "ยอดเงินกู้รวมดอกเบี้ย";

            if (ReqCon == null)
            {
                return string.Empty;
            }

            decimal loanAmount = (ReqCon.ContractLoanTotalAmount != null ?
                ReqCon.ContractLoanTotalAmount.Value :
                TransactionService.FindLoanTotalAmount(ReqCon.ContractId));


            return $"{mess} : {string.Format("{0:n2}", loanAmount)} บาท";
        }

        private void Back()
        {
            if (!string.IsNullOrEmpty(StaffID))
            {
                if (newRole != 0)
                {
                    if (rootPage != 0)
                    {
                        switch (rootPage)
                        {
                            case (int)BackRootPageEnum.Admin_RequestDetail:

                                if (rootRequestID == 0)
                                {
                                    return;
                                }
                                else if (StepID == 0)
                                {
                                    navigationManager.NavigateTo($"/{RoleTypeEnum.Admin}/RequestDetail/{rootRequestID}");
                                    return;
                                }

                                navigationManager.NavigateTo($"/{newRole}/AgreementDetailPage/{FromPage}/{StaffID}/{RequestID}/{rootPage}/{rootRequestID}");
                                break;

                            case (int)BackRootPageEnum.LoanAgreementOld:
                                navigationManager.NavigateTo($"/{newRole}/AgreementDetailPage/{FromPage}/{StaffID}/{RequestID}/{rootPage}/{rootRequestID}");
                                break;

                        }
                    }
                }
                else if (Role != "Manager")
                {
                    navigationManager.NavigateTo($"/Admin/AgreementDetailPage/{FromPage}/{StaffID}/{RequestID}");
                }
                else
                {
                    navigationManager.NavigateTo($"/Manager/AgreementDetailPage/{FromPage}/{StaffID}/{RequestID}");
                }
            }
            else
            {
                navigationManager.NavigateTo($"/AgreementDetailPage/{RequestID}");
            }
        }

        private void PdfAgreement()
        {
            var page = "AgreementDetail";
            if (!string.IsNullOrEmpty(StaffID))
            {
                if (newRole != 0)
                {
                    if (rootPage != 0)
                    {
                        switch (rootPage)
                        {
                            case (int)BackRootPageEnum.Admin_RequestDetail:
                                if (rootRequestID == 0)
                                {
                                    return;
                                }
                                navigationManager.NavigateTo($"/{newRole}/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}/{StaffID}/{rootPage}/{rootRequestID}");
                                break;

                            case (int)BackRootPageEnum.LoanAgreementOld:
                                navigationManager.NavigateTo($"/{newRole}/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}/{StaffID}/{rootPage}/{rootRequestID}");
                                break;
                        }
                    }
                }
                else if (Role != "Manager")
                {
                    navigationManager.NavigateTo($"Admin/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}");
                }
                else
                {
                    navigationManager.NavigateTo($"Manager/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}");
                }
            }
        }

        private async Task SetLoanDocAsync()
        {
            LoadingResultImg = true;
            ResultDocList = new();
            await Task.Delay(1);
            var step1 = await SaveFileAndImgService.GetDocByStapAsync(1, Request, true);
            var step2 = await SaveFileAndImgService.GetDocByStapAsync(2, Request, true);
            var step3 = await SaveFileAndImgService.GetDocByStapAsync(3, Request, true);
            if (step1 != null)
            {
                ResultDocList.Add(step1);
            }
            if (step2 != null)
            {
                ResultDocList.Add(step2);
            }
            if (step3 != null)
            {
                ResultDocList.Add(step3);
            }

            LoadingResultImg = false;
            StateHasChanged();
        }

        private decimal GetBalanceTotal()
        {
            decimal Balance = TransactionService.GetBalanceTotal(Request?.ContractId, Request!.ContractLoanAmount!.Value);
            return Balance;
        }

        private async Task<string> GetFacNameAsync(string staffId)
        {
            var FacName = string.Empty;
            var Detail = await psuLoan.GetUserDetailAsync(staffId);
            if (Detail != null && !string.IsNullOrEmpty(Detail.FacNameThai))
            {
                FacName = Detail.FacNameThai;
            }
            return FacName;
        }

        private async Task CheckTransactionDocAsync(VLoanRequestContract? vLoanRequestContract)
        {
            if (vLoanRequestContract != null && vLoanRequestContract.CurrentStatusId == 99)
            {
                transactionDocModel.IsShowTransactionDoc = true;
                var path = Utility.GetFullOtherDir(vLoanRequestContract.LoanRequestId, vLoanRequestContract.DebtorStaffId);
                PaymentTransaction? paymentTransaction = await psuLoan.GetLastPaymentTransactionByContractIdAsync(vLoanRequestContract.ContractId);
                transactionDocModel.paymentTransaction = (paymentTransaction != null ? paymentTransaction : new());

                if (!string.IsNullOrEmpty(path))
                {
                    var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

                    var di = new DirectoryInfo($"{physicalFilePath}\\{path}");
                    if (di.Exists)
                    {
                        ContractAttachment? contractAttachment = await psuLoan.GetOtherFileFromContractAttachmentByLoanRequestIdAsync(vLoanRequestContract.LoanRequestId);
                        if (contractAttachment != null && !string.IsNullOrEmpty(contractAttachment.AttachmentAddr))
                        {
                            var dir = $"{physicalFilePath}\\{contractAttachment.AttachmentAddr}";
                            transactionDocModel.dir = (File.Exists(dir) ? contractAttachment.AttachmentAddr : string.Empty);
                        }
                    }
                }
            }
            StateHasChanged();
        }

        /// <summary>
        /// ใช้สำหรับเก็บข้อมูล model ที่ใช้แสดง หลักฐานการปิดยอด
        /// </summary>
        public class TransactionDocModel
        {
            public PaymentTransaction paymentTransaction { get; set; } = new();
            /// <summary>
            /// แสดงเอกสารตอนจ่ายเงินสำเร็จ (กรณีปิดยอดก่อน) default val = false (ไม่แสดง)
            /// </summary>
            public bool IsShowTransactionDoc { get; set; } = false;
            public string dir { get; set; } = string.Empty;
        }
    }
}
