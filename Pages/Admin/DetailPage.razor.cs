using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.Admin
{
    public partial class DetailPage
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        [Parameter] public decimal LoanTypeId { get; set; } = 0;

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private LoanType? Ltype { get; set; } = new();
        private List<VAttachmentRequired> ListRequired { get; set; } = new();
        private List<ContractStep> Listcontract { get; set; } = new();
        private ContractAttachment? File { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (LoanTypeId != 0)
                {
                    await GetLoandataAsync(LoanTypeId);
                    await GetContractstepAsync();
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }

        }

        private async Task GetLoandataAsync(decimal ID)
        {
            Ltype = await psuLoan.GetLoanTypeAsync((byte?)ID);
            ListRequired = await _context.VAttachmentRequireds
                .Where(c => c.LoanTypeId == ID)
                .ToListAsync();

            File = await psuLoan.GetContractAttachmentByAttachmentId(6);
        }

        private async Task GetContractstepAsync()
        {
            Listcontract = await _context.ContractSteps.ToListAsync();
        }

        private void Back()
        {
            navigationManager.NavigateTo("./Admin/ManageTypeLoan");
        }
    }
}
