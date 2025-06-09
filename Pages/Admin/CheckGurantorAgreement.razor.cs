using AntDesign.TableModels;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.Admin
{
    public partial class CheckGurantorAgreement
    {
        [CascadingParameter] public Error Error { get; set; } = null!;

        #region Parameter
        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public string Role { get; set; } = string.Empty;

        /// <summary>
        ///  LoanApp.Models.RoleTypeEnum (enum)
        /// </summary>
        [Parameter] public int newRole { get; set; } = 0;
        [Parameter] public decimal RequestID { get; set; } = 0;

        #endregion

        [Inject] LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<VLoanRequestContract> ListAgreement { get; set; } = new();
        private List<VLoanRequestContract> ListAgreementSuccess { get; set; } = new();
        //private PanelFooterModel Footer { get; set; } = new()
        //private PanelFooterModel FooterSuccess { get; set; } = new()

        private decimal[] StutusID { get; set; } = new[] { 0m, 3m, 99m, 98m };
        private decimal[] StutusIDSuccess { get; set; } = { 3m, 99m, 98m };
        private string FormathDate { get; set; } = "dd-MM-yyyy";
        private string FormathTime { get; set; } = "HH:mm";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (!string.IsNullOrEmpty(StaffID))
                    {
                        List<VLoanRequestContract> agreements = await psuLoan.GetListGuarantorStaffFormVLoanRequestContractByNotStatusReq(StaffID, StutusID.ToList(), Utility.ShowDataYear);

                        List<VLoanRequestContract> agreementSuccess = await psuLoan.GetListGuarantorStaffFormVLoanRequestContract(StaffID, StutusIDSuccess.ToList(), Utility.ShowDataYear);

                        if (agreements.Any())
                        {
                            ListAgreement = Utility.CheckChangeGuarantor(StaffID, agreements);
                        }

                        if (agreementSuccess.Any())
                        {
                            ListAgreementSuccess = Utility.CheckChangeGuarantor(StaffID, agreementSuccess);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }

                StateHasChanged();
            }
        }

        private void BackPage()
        {
            if (!string.IsNullOrEmpty(StaffID))
            {
                if (Role == RoleTypeEnum.Manager.ToString())
                {
                    navigationManager.NavigateTo($"Manager/CheckLoanpage/{StaffID}");
                }
                else if (newRole == 3) /// newRole == LoanApp.Models.RoleTypeEnum (enum)
                {
                    if (RequestID != 0)
                    {
                        navigationManager.NavigateTo($"Admin/RequestDetail/{RequestID}");
                    }
                    else
                    {
                        navigationManager.NavigateTo($"Admin/CheckLoanpage/{StaffID}");
                    }
                }
                else
                {
                    navigationManager.NavigateTo($"Admin/CheckLoanpage/{StaffID}");
                }
            }
            else
            {
                navigationManager.NavigateTo($"HomeUser");
            }
        }

        private void TopageAgreementDetailPage(decimal LoanRequestId)
        {
            if (Role == RoleTypeEnum.Manager.ToString())
            {
                navigationManager.NavigateTo($"/Manager/GuarantDetailPage/{(int)PageControl.AdminCheckGurantorAgreement}/{StaffID}/{LoanRequestId}");
            }
            else if (newRole == 3)
            {
                if (RequestID != 0)
                {
                    navigationManager.NavigateTo($"/{newRole}/GuarantDetailPage/{(int)PageControl.AdminCheckGurantorAgreement}/{StaffID}/{LoanRequestId}/{(int)BackRootPageEnum.Admin_RequestDetail}/{RequestID}");
                }
                else
                {
                    navigationManager.NavigateTo($"/{newRole}/GuarantDetailPage/{(int)PageControl.AdminCheckGurantorAgreement}/{StaffID}/{LoanRequestId}/{(int)BackRootPageEnum.CheckGurantorAgreement}/{RequestID}");
                }
            }
            else
            {
                navigationManager.NavigateTo($"/{RoleTypeEnum.Admin}/GuarantDetailPage/{(int)PageControl.AdminCheckGurantorAgreement}/{StaffID}/{LoanRequestId}");
            }
        }

        private void OnRowClick(RowData<VLoanRequestContract> row)
        {
            TopageAgreementDetailPage(row.Data.LoanRequestId);
        }
    }
}
