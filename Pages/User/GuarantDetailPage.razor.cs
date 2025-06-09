using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Pages.Admin;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LoanApp.Pages.User.AgreementDetailPage;

namespace LoanApp.Pages.User
{
    public partial class GuarantDetailPage
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        #endregion

        #region Parameter
        [Parameter] public decimal RequestID { get; set; } = 0;
        [Parameter] public int PageTo { get; set; } = 0;
        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public string Role { get; set; } = string.Empty;
        /// <summary>
        /// LoanApp.Models.RoleTypeEnum
        /// </summary>
        [Parameter] public int newRole { get; set; } = 0;
        /// <summary>
        /// LoanApp.Models.BackRootPageEnum
        /// </summary>
        [Parameter] public int rootPage { get; set; } = 0;
        [Parameter] public decimal rootRequestID { get; set; } = 0;

        #endregion

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private VLoanStaffDetail? DebtorStaff { get; set; } = new();
        private VLoanStaffDetail? GuarantStaff { get; set; } = new();
        private VLoanRequestContract? Request { get; set; } = new();
        private List<StatusUserModel> ListStatusUser { get; set; } = new();
        private bool IsMobile { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                string GuarantStaff_Id = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                if (!string.IsNullOrEmpty(StaffID))
                {
                    GuarantStaff_Id = StaffID;
                }

                if (RequestID != 0 && !string.IsNullOrEmpty(GuarantStaff_Id))
                {
                    Request = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);

                    if (Request != null)
                    {
                        DebtorStaff = await psuLoan.GetUserDetailAsync(Request.DebtorStaffId);
                        GuarantStaff = await psuLoan.GetUserDetailAsync(GuarantStaff_Id);

                        decimal[] AllStatus = new[] { 1m, 2m, 4m, 9m, 6m, 7m, 8m, 80m, 81m, 82m, 99m, 98m, 3m, 200m };
                        ListStatusUser = userService.SetLStatusUser(Request, AllStatus);
                    }
                }
                else { }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private void BackPage()
        {
            if (PageTo != 0)
            {
                if (newRole != 0)
                {
                    if (rootPage != 0)
                    {
                        switch (rootPage)
                        {
                            case (int)BackRootPageEnum.Admin_RequestDetail:
                                if (rootRequestID == 0)
                                {
                                    return;
                                }
                                navigationManager.NavigateTo($"/{newRole}/CheckGurantorAgreement/{StaffID}/{rootRequestID}");
                                return;

                            case (int)BackRootPageEnum.CheckGurantorAgreement:
                                navigationManager.NavigateTo($"/{newRole}/{BackRootPageEnum.CheckGurantorAgreement}/{StaffID}/0");
                                return;

                            case (int)BackRootPageEnum.LoanAgreementOld:
                                navigationManager.NavigateTo($"/{newRole}/LoanAgreementOld");
                                return;
                        }
                    }
                }
                else if (Role != "Manager")
                {
                    switch (PageTo)
                    {

                        case (int)PageControl.AdminCheckGurantorAgreement:
                            navigationManager.NavigateTo($"/Admin/CheckGurantorAgreement/{StaffID}");
                            return;
                            //break

                        default: break;
                    }
                }
                else
                {
                    switch (PageTo)
                    {

                        case (int)PageControl.AdminCheckGurantorAgreement:
                            navigationManager.NavigateTo($"/Manager/CheckGurantorAgreement/{StaffID}");
                            //break
                            return;

                        default: break;
                    }
                }
            }
            else
            {
                navigationManager.NavigateTo("/Guarantor");
            }
        }

        private void ToPageTransaction(VLoanRequestContract agreement)
        {
            if (PageTo != 0)
            {
                if (newRole != 0)
                {
                    if (rootPage != 0)
                    {
                        switch (rootPage)
                        {
                            case (int)BackRootPageEnum.Admin_RequestDetail:
                                if (rootRequestID == 0)
                                {
                                    return;
                                }
                                //"/{newRole:int}/GuarantDetail/{RequestID:decimal}/{StaffID}/{FromPage:int}/{rootPage:int}/{rootRequestID:decimal}"

                                navigationManager.NavigateTo($"/{newRole}/GuarantDetail/{agreement.LoanRequestId}/{StaffID}/{PageTo}/{rootPage}/{rootRequestID}");
                                return;

                            case (int)BackRootPageEnum.CheckGurantorAgreement:
                                navigationManager.NavigateTo($"/{newRole}/GuarantDetail/{agreement.LoanRequestId}/{StaffID}/{PageTo}/{rootPage}/{rootRequestID}");
                                return;

                            case (int)BackRootPageEnum.LoanAgreementOld:
                                navigationManager.NavigateTo($"/{newRole}/GuarantDetail/{agreement.LoanRequestId}/{StaffID}/{PageTo}/{rootPage}/{rootRequestID}");
                                return;
                        }
                    }
                }
                else if (Role != "Manager")
                {
                    navigationManager.NavigateTo($"/Admin/GuarantDetail/{agreement.LoanRequestId}/{StaffID}/{PageTo}");
                }
                else
                {
                    navigationManager.NavigateTo($"/Manager/GuarantDetail/{agreement.LoanRequestId}/{StaffID}/{PageTo}");
                }
            }
            else
            {
                navigationManager.NavigateTo($"/GuarantDetail/{agreement.LoanRequestId}");
            }
        }
    }
}
