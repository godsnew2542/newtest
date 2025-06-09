using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Org.BouncyCastle.Asn1.Ocsp;

namespace LoanApp.Components.Admin
{
    public partial class LoanAgreementGuarantorTable
    {
        [Parameter] public string TitleName { get; set; } = "สัญญาค้ำประกัน";
        [Parameter] public decimal RequestID { get; set; } = 0;
        [Parameter] public List<VLoanRequestContract> requestContracts { get; set; } = new();

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private void TopageGuarantDetail(decimal _requestID, string? staffId)
        {
            if (string.IsNullOrEmpty(staffId))
            {
                return;
            }

            navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Admin}/GuarantDetail/{staffId}/{_requestID}/{(int)BackRootPageEnum.Admin_RequestDetail}/{RequestID}");
        }
    }
}
