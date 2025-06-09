using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Services.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Components.Test
{
    public partial class PaymentListComponent
    {
        [Inject] private ITransactionService transactionService { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private ApplyLoanModel ModelApplyLoan { get; set; } = new();
        private List<InstallmentDetail> InstallmentList { get; set; } = new();

        private DateTime PaymentTime = DateTime.Now;
        private decimal? ContractId = null;
        private decimal LoanTotalAmount { get; set; } = 0m;
        private decimal? LoanInstallment { get; set; } = null;

        protected override void OnInitialized()
        {
            ModelApplyLoan.LoanInterest = 0;
            ModelApplyLoan.LoanAmount = 0;
        }

        private decimal GetLoanTotalAmount(DateTime date, decimal? LoanNumInstallments, decimal? LoanInterest, decimal LoanAmount)
        {
            decimal totalAmount = 0m;
            decimal TotalInterest = 0m;

            var ListDate = transactionService.SetPayDate(date, LoanNumInstallments!.Value);
            var BalAmount = LoanAmount;

            if (LoanNumInstallments != 0)
            {
                for (int i = 0; i < LoanNumInstallments; i++)
                {
                    var index = i + 1;

                    decimal PaidInstallment = transactionService.GetTransactionByInstallment(LoanAmount, LoanNumInstallments.Value, LoanInterest!.Value);

                    var Interest = GetInterest(ListDate, index, BalAmount, LoanInterest, LoanNumInstallments!.Value, date);
                    TotalInterest += Interest;
                    var Balance = PaidInstallment - Interest;

                    // งวดสุดท้าย
                    if (index == LoanNumInstallments)
                    {
                        Balance = BalAmount;
                        //PaidInstallment = BalAmount + Interest
                    }

                    BalAmount -= Balance;
                }

                totalAmount = Math.Round(LoanAmount + TotalInterest, 2);
            }

            return totalAmount;
        }

        public decimal GetInterest(List<string> ListPayDate,
           int NumInstallments,
           decimal BalanceAmount,
           decimal? LoanInterest,
           decimal LoanNumInstallments,
           DateTime PaidDate)
        {
            MonthModel _month = new MonthModel();

            var TotalInstallments = Convert.ToDecimal(LoanNumInstallments);
            var _BalanceAmount = BalanceAmount;
            var payDateString = ListPayDate[NumInstallments - 1];
            DateTime payDate = transactionService.ChangeFormatPayDate(payDateString, _month.Number);

            decimal Interest = transactionService.GetTransactionByInterest(TotalInstallments, NumInstallments, PaidDate, payDate, _BalanceAmount, LoanInterest!.Value);
            return Interest;
        }

        private decimal SetLoanInstallment(decimal LoanAmount, int LoanNumInstallments, decimal? LoanInterest, ITransactionService _transactionService)
        {
            var Installment = 0m;
            if (LoanNumInstallments != 0)
            {
                Installment = _transactionService.GetTransactionByInstallment(LoanAmount, LoanNumInstallments, LoanInterest!.Value);
            }

            return Installment;
        }

        private void NewOnChange(DateTime? value)
        {
            PaymentTime = dateService.ConvertToDateTime(value);
        }

        private void OpenPayment()
        {
            if (ModelApplyLoan.LoanInterest == null)
            {
                return;
            }
            LoanTotalAmount = GetLoanTotalAmount(PaymentTime, ModelApplyLoan.LoanNumInstallments, ModelApplyLoan.LoanInterest, ModelApplyLoan.LoanAmount);
            LoanInstallment = SetLoanInstallment(ModelApplyLoan.LoanAmount, ModelApplyLoan.LoanNumInstallments, ModelApplyLoan.LoanInterest, transactionService);

            InstallmentList = SetInstallmentDetail(PaymentTime, ModelApplyLoan.LoanNumInstallments, ModelApplyLoan.LoanAmount, ModelApplyLoan.LoanInterest, transactionService, ContractId);

            StateHasChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <param name="LoanNumInstallments"></param>
        /// <param name="LoanAmount"></param>
        /// <param name="LoanInterest"></param>
        /// <param name="_transactionService"></param>
        /// <param name="contractId"></param>
        /// <returns></returns>
        public List<InstallmentDetail> SetInstallmentDetail(DateTime date, int LoanNumInstallments, decimal LoanAmount, decimal? LoanInterest, ITransactionService _transactionService, decimal? contractId)
        {
            List<InstallmentDetail> installmentList = new();
            try
            {
                List<DateTime> ListDate = _transactionService.SetPayDateReturnDateTime(date, LoanNumInstallments);
                var BalanceAmount = LoanAmount;

                for (int i = 0; i < LoanNumInstallments; i++)
                {
                    var index = i + 1;

                    var PaidInstallment = SetLoanInstallment(LoanAmount, LoanNumInstallments, LoanInterest, _transactionService);

                    var Interest = GetInterest2(ListDate[i], index, BalanceAmount, LoanInterest, LoanNumInstallments, date, _transactionService);

                    var Balance = PaidInstallment - Interest;

                    // งวดสุดท้าย
                    if (index == LoanNumInstallments)
                    {
                        Balance = BalanceAmount;
                        PaidInstallment = BalanceAmount + Interest;
                    }
                    BalanceAmount = BalanceAmount - Balance;


                    InstallmentDetail Installment = new()
                    {
                        ContractId = contractId,
                        InstallmentNo = index,
                        DueDate = ListDate[i],
                        PrincipleAmount = Balance,
                        InterestAmont = Interest,
                        TotalAmount = PaidInstallment
                    };
                    installmentList.Add(Installment);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return installmentList;
        }

        private decimal GetInterest2(DateTime payDate, int NumInstallments, decimal BalanceAmount, decimal? LoanInterest, decimal LoanNumInstallments, DateTime PaidDate, ITransactionService _transactionService)
        {
            var TotalInstallments = Convert.ToDecimal(LoanNumInstallments);
            var _BalanceAmount = BalanceAmount;

            decimal Interest = _transactionService.GetTransactionByInterest(TotalInstallments, NumInstallments, PaidDate, payDate, _BalanceAmount, LoanInterest!.Value);

            return Interest;
        }

        private async void UpdateData(List<InstallmentDetail> dataList, ContractMain main)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];

                InstallmentDetail? installmentDetail = await _context.InstallmentDetails
                    .Where(c => c.ContractId == main.ContractId)
                    .Where(c => c.InstallmentNo == (i + 1))
                    .FirstOrDefaultAsync();

                if (installmentDetail != null)
                {
                    installmentDetail.DueDate = data.DueDate;
                    installmentDetail.PrincipleAmount = data.PrincipleAmount;
                    installmentDetail.InterestAmont = data.InterestAmont;
                    installmentDetail.TotalAmount = data.TotalAmount;

                    await psuLoan.UpdateInstallmentDetail(installmentDetail);
                    //_context.Update(installmentDetail)
                    //await _context.SaveChangesAsync()
                }
            }
        }
    }
}
