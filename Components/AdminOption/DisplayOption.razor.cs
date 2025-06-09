using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Components.AdminOption
{
    public partial class DisplayOption
    {
        [Parameter] public FormAdminOptionModel FormOption { get; set; } = new();

        private int CountNo { get; set; } = -1;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CountNo = -1;

                FormOption.Display = new();
                DisplayModel? display = await _context.VLoanRequestContracts
                    .Where(c => c.LoanRequestId.Equals(FormOption.LoanRequestId))
                    .Select(c => new DisplayModel
                    {
                        LoanTotalAmount = (c.ContractLoanTotalAmount != null ?
                        c.ContractLoanTotalAmount :
                        TransactionService.FindLoanTotalAmount(FormOption.ContractId)),
                        LoanInterest = c.LoanRequestLoanInterest,
                        LoanNumInstallments = c.LoanRequestNumInstallments,
                        LoanInstallment = c.ContractLoanInstallment,
                        BalanceAmount = TransactionService.GetBalanceTotal(FormOption.ContractId, FormOption.LoanAmount, false)
                    })
                    .FirstOrDefaultAsync();

                if (display != null)
                {
                    FormOption.Display = display;
                }

                //await Task.Delay(2000)

                if (CountNo == -1)
                {
                    CountNo = 0;
                }
                StateHasChanged();
            }
        }
    }
}
