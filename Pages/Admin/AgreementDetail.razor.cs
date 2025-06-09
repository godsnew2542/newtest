using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Settings;

namespace LoanApp.Pages.Admin
{
    public partial class AgreementDetail
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        [Parameter] public decimal RequestID { get; set; } = 0;

        #region Inject
        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        #endregion

        private VLoanRequestContract? ReqCon { get; set; } = new();
        private List<ListDocModel> ResultDocList { get; set; } = new();
        private ImgOtherModel imgOtherModel { get; set; } = new();

        private string[] DayInstallments { get; set; } = new string[] { };
        private bool LoadingResultImg { get; set; } = false;
        //private bool EditContractNoVisible { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                if (RequestID != 0)
                {
                    ReqCon = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);
                    if (ReqCon != null)
                    {
                        DateTime? PaidDate = (ReqCon.PaidDate != null ? ReqCon.PaidDate : ReqCon.ContractDate);
                        DayInstallments = await TransactionService.SetDateWithPaymentTransactionAsync(ReqCon.ContractId,
                            ReqCon.ContractLoanNumInstallments,
                            PaidDate);
                    }
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
                imgOtherModel = await psuLoan.CheckFileUploadOther(ReqCon);
                StateHasChanged();
            }
        }

        private void AgreementCheck(VLoanRequestContract? ReqCon)
        {
            navigationManager.NavigateTo($"/Admin/AgreementCheck/{ReqCon?.LoanRequestId}");
        }

        private void Back()
        {
            navigationManager.NavigateTo("/Admin/ManageLoanAgreement");
        }

        private decimal GetLoanAmount(VLoanRequestContract? ReqCon)
        {
            decimal LoanAmount = 0;
            if (ReqCon != null)
            {
                if (ReqCon.ContractLoanTotalAmount != null)
                {
                    LoanAmount = ReqCon.ContractLoanTotalAmount.Value;
                }
                else
                {
                    LoanAmount = TransactionService.FindLoanTotalAmount(ReqCon.ContractId);
                }
            }
            return LoanAmount;
        }

        private string GetNameGuarantor(VLoanRequestContract ReqCon)
        {
            string staffID = string.Empty;
            string mess = string.Empty;

            /* Check ผู้ค้ำว่าไปเซ็นสัญญาหรือยัง */
            if (!string.IsNullOrEmpty(ReqCon.ContractGuarantorStaffId))
            {
                staffID = ReqCon.ContractGuarantorStaffId;
            }
            else
            {
                staffID = (!string.IsNullOrEmpty(ReqCon.LoanRequestGuaranStaffId) ?
                    ReqCon.LoanRequestGuaranStaffId :
                    string.Empty);
            }

            if (!string.IsNullOrEmpty(staffID))
            {
                mess = $"{userService.GetFullName(staffID)} ({GetFacName(staffID)} เบอร์โทรศัพท์:{userService.GetMobileTelFromLoanStaffDetail(staffID)})";
            }
            return mess;
        }

        private string? GetFacName(string staffId)
        {
            string? FacName = string.Empty;
            var Detail = userService.GetUserDetail(staffId);
            if (Detail != null)
            {
                FacName = Detail.FacNameThai;
            }
            return FacName;
        }

        private decimal GetBalanceTotal()
        {
            decimal Balance = 0;

            //PaymentTransaction? Transactions = _context.PaymentTransactions
            //   .Where(c => c.ContractId == ReqCon!.ContractId)
            //   .Select(c => new PaymentTransaction
            //   {
            //       ContractId = c.ContractId,
            //       InstallmentNo = c.InstallmentNo,
            //       BalanceAmount = c.BalanceAmount,
            //   })
            //   .Distinct()
            //   .OrderByDescending(c => c.InstallmentNo)
            //   .FirstOrDefault();

            PaymentTransaction? Transactions = psuLoan.GetPaymentTransactionByContractIdSelectDataNoneAsync(ReqCon!.ContractId);

            if (Transactions != null)
            {
                Balance = (Transactions.BalanceAmount != null ?
                    Transactions.BalanceAmount.Value :
                    0);
            }
            else
            {
                if (ReqCon?.ContractLoanTotalAmount != null)
                {
                    Balance = ReqCon.ContractLoanTotalAmount.Value;
                }
                else if (ReqCon?.LoanRequestLoanTotalAmount != null)
                {
                    Balance = ReqCon.LoanRequestLoanTotalAmount.Value;
                }
                else
                {
                    Balance = (ReqCon?.ContractLoanAmount != null ?
                        ReqCon.ContractLoanAmount.Value :
                        0);
                }

            }
            return Balance;
        }

        private string GetDocUrl(VLoanRequestContract? data)
        {
            var path = string.Empty;
            if (data?.LoanAttachmentId != null)
            {
                //ContractAttachment? ConAttachment = _context.ContractAttachments
                //    .Where(c => c.AttachmentId == data.LoanAttachmentId)
                //    .FirstOrDefault();

                ContractAttachment? ConAttachment = psuLoan.GetContractAttachmentByAttachmentIdNotAsync(data.LoanAttachmentId);

                if (ConAttachment != null)
                {
                    path = $"{AppSettings.Value.RequestFilePath}\\{ConAttachment.AttachmentAddr}";
                }
            }
            return path;
        }

        private async Task SetLoanDocAsync()
        {
            LoadingResultImg = true;
            ResultDocList = new();
            await Task.Delay(1);
            var step1 = await SaveFileAndImgService.GetDocByStapAsync(1, ReqCon, true);
            var step2 = await SaveFileAndImgService.GetDocByStapAsync(2, ReqCon, true);
            var step3 = await SaveFileAndImgService.GetDocByStapAsync(3, ReqCon, true);
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

        /// <summary>
        /// เช็คว่าสามารถแก้ไข เลขที่สัญญาได้ หรือไหม โดยการ  check ว่าเคยมีการชำระเงินหรือยัง (PaymentTransaction) 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>true = สามรถแก้ไขได้ || false = ไม่สามรถแก้ไขได้</returns>
        private async Task<bool> IsChangeContractNo(VLoanRequestContract? data)
        {
            var val = await psuLoan.GetAllPaymentTransactionByContractNo(data?.ContractNo);
            if (!val.Any())
            {
                return true;
            }

            return false;
        }

        //private void CallbackData(bool e)
        //{
        //    EditContractNoVisible = e;
        //    var url = navigationManager.Uri.Split(navigationManager.BaseUri);
        //    if (url.Length == 2)
        //    {
        //        navigationManager.NavigateTo(url[1], true);
        //    }
        //}
    }
}
