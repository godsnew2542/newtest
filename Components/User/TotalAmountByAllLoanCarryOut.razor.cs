using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.User
{
    public partial class TotalAmountByAllLoanCarryOut
    {
        [Parameter] public decimal[] ContractStatusID { get; set; } = Array.Empty<decimal>();
        [Parameter] public string StaffID { get; set; } = string.Empty;

        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private decimal CountNo = -1m;
        private decimal BalanceAmount = 0;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {

                var test = await GetDataDB(ContractStatusID);
                CountNo = GetTotalAmountAsync(test);
                BalanceAmount = await SumBalanceAmount(test);


                //CountNo = await GetTotalAmountAsync2(ContractStatusID);

                await Task.Delay(5000);
                if (CountNo == -1)
                {
                    CountNo = 0;
                }
                StateHasChanged();
            }
        }

        private async Task<decimal> SumBalanceAmount(List<VLoanRequestContract> result)
        {
            if (result.Any())
            {
                var temp = result
                    .Where(c => !string.IsNullOrEmpty(c.ContractNo))
                    .Select(x => new PaymentTransaction()
                    {
                        ContractNo = x.ContractNo,
                        ContractId = x.ContractId,
                    })
                    .ToList();

                var data = await psuLoan.CheckDataFormImportPayment(temp);

                if (data.Any())
                {
                    return data.Where(c => c.BalanceAmount != null).Sum(x => x.BalanceAmount!.Value);
                }
            }
            return 0;
        }

        private async Task<List<VLoanRequestContract>> GetDataDB(decimal[] StatusId)
        {
            if (!string.IsNullOrEmpty(StaffID))
            {
                return await _context.VLoanRequestContracts
                    .Where(c => StatusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => c.DebtorStaffId == StaffID)
                    .ToListAsync();
            }
            return new List<VLoanRequestContract>();
        }

        private decimal GetTotalAmountAsync(List<VLoanRequestContract> result)
        {
            decimal TotalAmount = 0m;

            if (result.Any())
            {
                foreach (var item in result)
                {
                    decimal AgreementAmount = item.ContractLoanAmount != null ? item.ContractLoanAmount!.Value : 0;
                    TotalAmount += AgreementAmount;
                }
            }
            return TotalAmount;
        }


        private async Task<decimal> GetTotalAmountAsync2(decimal[] StatusId)
        {
            decimal TotalAmount = 0m;

            if (!string.IsNullOrEmpty(StaffID))
            {
                List<VLoanRequestContract> ListAmount = await _context.VLoanRequestContracts
                    .Where(c => StatusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => c.DebtorStaffId == StaffID)
                    .ToListAsync();

                if (ListAmount.Any())
                {
                    for (int i = 0; i < ListAmount.Count; i++)
                    {
                        decimal AgreementAmount = ListAmount[i].ContractLoanAmount != null ? ListAmount[i].ContractLoanAmount!.Value : 0;
                        TotalAmount += AgreementAmount;
                    }
                }
            }
            return TotalAmount;
        }
    }
}
