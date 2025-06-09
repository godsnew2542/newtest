using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Components.AdminOption
{
    public partial class IncreaseInterestOption
    {
        [Parameter] public EventCallback<FormAdminOptionModel> OnIncreaseInterestChange { get; set; }
        [Parameter] public VLoanRequestContract ReqCon { get; set; } = new();
        [Parameter] public FormAdminOptionModel FormOption { get; set; } = new();

        private decimal? NewInterest { get; set; } = 7.50m;

        protected override void OnInitialized()
        {
            FormOption.IncreaseInterest.LoanInterestNow = ReqCon.ContractLoanInterest;
            FormOption.IncreaseInterest.NewLoanInterest = NewInterest;
            FormOption.IncreaseInterest.LoanNumInstallments = ReqCon.LoanRequestNumInstallments;
            FormOption.IncreaseInterest.LoanInstallment = (ReqCon.ContractLoanInstallment == null ? null : ReqCon.ContractLoanInstallment);
            FormOption.IncreaseInterest.BalanceAmount = TransactionService.GetBalanceTotal(FormOption.ContractId, FormOption.LoanAmount);
        }

        private decimal SplitFormatNumber(decimal? _value)
        {
            var Fnumber = FormatNumber(_value);
            var pp = Fnumber.Split(".");
            decimal value;

            if (pp[1] == "00")
            {
                value = Convert.ToDecimal(_value);
            }
            else
            {
                value = Convert.ToDecimal(Fnumber);
            }
            return value;
        }


        private async Task NewLoanInterestChangeAsync(decimal? _value)
        {
            FormOption.IncreaseInterest.NewLoanInterest = SplitFormatNumber(_value);
            await OnIncreaseInterestChange.InvokeAsync(FormOption);
        }
    }
}
