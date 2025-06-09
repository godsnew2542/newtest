using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.CarouselSlide
{
    public partial class CarouselSlide
    {
        [Parameter]
        public List<List<VLoanRequestContract>> ListOverview { get; set; } = new();

        private int GetPercent(decimal? LoanAmount, decimal? ContractId)
        {
            decimal BalanceAmount = GetBalanceAmount(ContractId);
            decimal Amount = 0;
            decimal Percent = 0;

            if (LoanAmount != null)
            {
                Amount = LoanAmount.Value;
            }
            else
            {
                Amount = TransactionService.FindLoanTotalAmount(ContractId);
            }

            if (BalanceAmount != 0 && Amount != 0)
            {
                Percent = 100 - ((BalanceAmount / Amount) * 100);
            }

            return Convert.ToInt32(Percent);
        }

        private decimal GetBalanceAmount(decimal? ContractId)
        {
            decimal BalanceAmount = 0;

            int PaymentTransaction = _context.PaymentTransactions
                .Where(c => c.ContractId == ContractId)
                .Select(c => new PaymentTransaction
                {
                    ContractId = c.ContractId,
                    InstallmentNo = c.InstallmentNo
                })
                .Count();

            if (PaymentTransaction != 0)
            {
                PaymentTransaction? Payment = _context.PaymentTransactions
               .Where(c => c.ContractId == ContractId &&
               c.InstallmentNo == Convert.ToDecimal(PaymentTransaction))
               .FirstOrDefault();

                if (Payment != null)
                {
                    BalanceAmount = (Payment.BalanceAmount != null ? Payment.BalanceAmount.Value : 0);
                }
            }
            return BalanceAmount;
        }

    }


}
