using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Components
{
    public partial class StatCount
    {
        #region
        [Parameter] public decimal[] ContractStatusID { get; set; } = Array.Empty<decimal>();
        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public CountType CountNoType { get; set; }
        [Parameter] public string? AdminCampId { get; set; } = null;

        #endregion

        private int CountNo { get; set; } = -1;

        public enum CountType
        {
            LoanRequest,
            DebtorAgreement,
            GuarantAgreement
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CountNo = -1;
                switch (CountNoType)
                {
                    case CountType.LoanRequest:
                        CountNo = await CountVLoanRequestContractsAsync(ContractStatusID);
                        break;
                    case CountType.DebtorAgreement:
                        CountNo = await GetLoanAgreementCountDebtorAsync(ContractStatusID);
                        break;
                    case CountType.GuarantAgreement:
                        CountNo = await GetGuarantAgreementCountAsync(ContractStatusID);
                        break;
                    default: break;
                }

                //await Task.Delay(5000)
                if (CountNo == -1)
                {
                    CountNo = 0;
                }
                StateHasChanged();
            }
        }

        private async Task<int> CountVLoanRequestContractsAsync(decimal[] StatusId)
        {
            int total = await _context.VLoanRequestContracts
                .Where(c => StatusId.Contains(c.CurrentStatusId!.Value))
                .Where(c => (c.ContractDate == null) ||
                 ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                .Where(c => string.IsNullOrEmpty(AdminCampId) || c.DebtorCampusId == AdminCampId)
                .CountAsync();
            return total;
        }

        private async Task<int> GetLoanAgreementCountDebtorAsync(decimal[] StatusId)
        {
            int CountAgreement = await _context.VLoanRequestContracts
                 .Where(c => StatusId.Contains(c.CurrentStatusId!.Value) &&
                 c.DebtorStaffId == StaffID)
                 .CountAsync();

            return CountAgreement;
        }

        private async Task<int> GetGuarantAgreementCountAsync(decimal[] Status)
        {
            int CountAgreement = 0;
            List<VLoanRequestContract> Contract = await _context.VLoanRequestContracts
                 .Where(c => Status.Contains(c.CurrentStatusId!.Value))
                 .Where(c => c.LoanRequestGuaranStaffId == StaffID || c.ContractGuarantorStaffId == StaffID)
                 .ToListAsync();

            if (Contract.Count != 0)
            {
                for (int i = 0; i < Contract.Count; i++)
                {
                    var item = Contract[i];
                    if (item.LoanRequestGuaranStaffId == item.ContractGuarantorStaffId)
                    {
                        ++CountAgreement;
                    }
                    else
                    {
                        if (item.ContractGuarantorStaffId == StaffID)
                        {
                            ++CountAgreement;
                        }
                    }
                }
            }
            return CountAgreement;
        }
    }
}
