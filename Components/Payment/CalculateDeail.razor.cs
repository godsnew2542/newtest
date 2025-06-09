using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Pages.User;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace LoanApp.Components.Payment
{
    public partial class CalculateDeail
    {
        [Parameter] public ApplyLoanModel? ModelApplyLoan { get; set; } = null;
        [Parameter] public List<LoanType> LoanTypeList { get; set; } = new();
        [Parameter] public decimal TotalAmount { get; set; } = 0;
        [Parameter] public DateTime? LoanDate { get; set; } = null;

        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        private LoanType? loan { get; set; } = null;

        private List<PaymentTransaction> transactions { get; set; } = new();

        protected override void OnInitialized()
        {
            if (ModelApplyLoan != null)
            {
                loan = LoanTypeList.Find(x => x.LoanTypeId == ModelApplyLoan.LoanTypeID);

                transactions = SetTransactionsCalculate(ModelApplyLoan, LoanDate);
            }
        }

        private List<PaymentTransaction> SetTransactionsCalculate(ApplyLoanModel model, DateTime? date)
        {
            if (date == null || model.LoanTypeID == null || loan?.LoanInterest == null)
            {
                return new List<PaymentTransaction>();
            }

            try
            {
                List<PaymentTransaction> results = new();
                var totalInstallments = Convert.ToDecimal(model.LoanNumInstallments);
                decimal balanceAmount = model.LoanAmount;

                List<DateTime> listDate = TransactionService.SetPayDateReturnDateTime(date!.Value, model.LoanNumInstallments);

                var paidInstallment = TransactionService.GetTransactionByInstallment(model.LoanAmount, (decimal)model.LoanNumInstallments, loan!.LoanInterest!.Value);

                for (int i = 0; i < model.LoanNumInstallments; i++)
                {
                    decimal interest = 0;

                    if (listDate.Any())
                    {
                        interest = TransactionService.GetTransactionByInterest(totalInstallments, i + 1, date!.Value, listDate[i], balanceAmount, loan!.LoanInterest!.Value);
                    }

                    decimal? balance = paidInstallment - interest;

                    balanceAmount = balanceAmount - balance!.Value;
                    PaymentTransaction payment = new()
                    {
                        InstallmentNo = i + 1,
                        PayDate = listDate.Any() ? listDate[i] : null,
                        PrincipleAmount = balance,
                        InterestAmont = interest,
                        TotalAmount = paidInstallment,
                        BalanceAmount = balanceAmount,
                    };

                    // งวดสุดท้าย
                    if (i + 1 == model.LoanNumInstallments)
                    {
                        balance = results.FirstOrDefault(x => x.InstallmentNo == i)?.BalanceAmount;
                        paidInstallment = balance!.Value + interest;

                        payment.PrincipleAmount = balance;
                        payment.TotalAmount = paidInstallment;
                        payment.BalanceAmount = payment.PrincipleAmount - balance;
                    }

                    results.Add(payment);
                }

                return results;
            }
            catch (Exception ex)
            {
                Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
                return new List<PaymentTransaction>();
            }
        }
    }
}
