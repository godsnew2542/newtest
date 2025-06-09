using DocumentFormat.OpenXml.Spreadsheet;
using LoanApp.Components.Test;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.Contracts;

namespace LoanApp.Components.Admin
{
    public partial class EditContractNoAndPaidDate
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }


        #region Parameter
        [Parameter] public EventCallback<bool> OnCallbackData { get; set; }
        [Parameter] public decimal? ContractId { get; set; } = null;
        [Parameter] public DateTime? PaidDate { get; set; } = null;
        [Parameter] public string? ContractNo { get; set; } = null;

        #endregion

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private Services.IServices.INotificationService notification { get; set; } = null!;

        #endregion

        private string? NewContractNo { get; set; } = null;
        private DateTime PaymentTime { get; set; } = DateTime.Now;

        protected override void OnInitialized()
        {
            if (!string.IsNullOrEmpty(ContractNo))
            {
                NewContractNo = ContractNo;
            }

            if (PaidDate != null)
            {
                PaymentTime = PaidDate.Value;
            }
        }

        private async Task ValidationContractNo(string? val, decimal? contractId)
        {
            if (string.IsNullOrEmpty(val) || val.Trim() == "")
            {
                return;
            }

            try
            {
                ContractMain? checkFormDb = await psuLoan.GeContractMainByContractNo(val.Trim());

                if (checkFormDb != null)
                {
                    _ = Task.Run(() => notification.WarningDefult("พบเลขที่สัญญานี้แล้ว กรุณาตรวจสอบอีกครั้ง"));
                    return;
                }

                ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(contractId);

                if (main != null)
                {
                    main.ContractNo = val.Trim();
                    main.PaidStaffId = StateProvider?.CurrentUser.StaffId;
                    await psuLoan.UpdateContractMain(main);

                    await SaveToHistoryAsync(main.ContractId, main.ContractStatusId, StateProvider?.CurrentUser.StaffId);

                    _ = Task.Run(() => notification.SuccessDefult("เปลี่ยนเลขที่สัญญา สำเร็จ"));

                    await OnCallbackData.InvokeAsync(false);
                }
            }
            catch (Exception ex)
            {
                await notification.ErrorDefult(notification.ExceptionLog(ex));
            }
        }

        private void NewOnChange(DateTime? value)
        {
            PaymentTime = dateService.ConvertToDateTime(value);
        }

        private async Task ValidationPaidDate(DateTime pdate, decimal? contractId)
        {
            try
            {
                ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(contractId);

                if (main != null)
                {
                    List<InstallmentDetail> installmentList = await psuLoan.GetAllInstallmentDetailByContractId(contractId);
                    LoanLendingAmount? loanLending = await psuLoan.GetLoanLendingAmountByContractId(main.ContractId);

                    main.PaidDate = pdate;
                    main.PaidStaffId = StateProvider?.CurrentUser.StaffId;
                    main.BudgetYear = userService.GetFiscalYear(pdate);
                    main.LoanInstallment = TransactionService.GetTransactionByInstallment(main.LoanAmount!.Value, main.LoanNumInstallments!.Value, main.LoanInterest!.Value);
                    //main.LoanTotalAmount = TransactionService.GetLoanTotalAmount(main, pdate);

                    if (loanLending != null)
                    {
                        loanLending.LendingDate = main.PaidDate;
                        loanLending.LendingAmount = main.LoanAmount;
                        loanLending.AdminRecord = StateProvider?.CurrentUser.StaffId;
                    }

                    PaymentListComponent paymentListComponentPage = new();
                    List<InstallmentDetail> installmentDetails = paymentListComponentPage.SetInstallmentDetail(main.PaidDate!.Value, (int)main.LoanNumInstallments!.Value, main.LoanAmount!.Value, main.LoanInterest, TransactionService, main.ContractId);

                    main.LoanTotalAmount = installmentDetails.Sum(c => c.TotalAmount);


                    await psuLoan.UpdateContractMain(main);
                    await SaveToHistoryAsync(main.ContractId, main.ContractStatusId, StateProvider?.CurrentUser.StaffId);

                    if (loanLending != null)
                    {
                        await psuLoan.UpDateLoanLendingAmount(loanLending);
                    }

                    if (installmentList.Any())
                    {
                        await psuLoan.DeleteListInstallmentDetailByContractId(installmentList);
                    }

                    await psuLoan.AddMutilateDataInstallmentDetail(installmentDetails);

                    _ = Task.Run(() => notification.SuccessDefult("เปลี่ยนวันที่กองคลังโอนเงิน สำเร็จ"));

                    await OnCallbackData.InvokeAsync(false);
                }
            }
            catch (Exception ex)
            {
                await notification.ErrorDefult(notification.ExceptionLog(ex));
            }
        }

        private async Task SaveToHistoryAsync(decimal? contractId, decimal? statusId, string? modifyBy)
        {
            try
            {
                await LogService.GetHisContractMainByContractIDAsync(contractId, statusId, modifyBy);
            }
            catch (Exception ex)
            {
                await notification.ErrorDefult(notification.ExceptionLog(ex));

            }
        }
    }
}
