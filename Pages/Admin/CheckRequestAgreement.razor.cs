using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoanApp.Shared;
using static LoanApp.Pages.User.AgreementDetailPage;
using LoanApp.DatabaseModel.LoanEntities;

namespace LoanApp.Pages.Admin
{
    public partial class CheckRequestAgreement
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public string Role { get; set; } = string.Empty;

        private List<VLoanRequestContract> ListAgreement { get; set; } = new();
        private PanelFooterModel Footer { get; set; } = new();

        private decimal[] StutusID { get; set; } = new[] { 0m, 1m, 2m, 4m};
        private string FormathDate { get; set; } = "dd-MM-yyyy";
        private string FormathTime { get; set; } = "HH:mm";

        protected async override Task OnInitializedAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(StaffID))
                {
                    await StartTableAsync();
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task StartTableAsync()
        {
            try
            {
                var total = await CountAgreementAsync();
                SetUserView(total);
                await DataTableAsync(0, Footer.Limit);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Choosedate(VLoanRequestContract req)
        {
            string role = RoleTypeEnum.Admin.ToString();
            if (!string.IsNullOrEmpty(Role))
            {
                role = Role;
            }
            navigationManager.NavigateTo($"/{role}/ChooseDate/{req.DebtorStaffId}/{req.LoanRequestId}");
        }


        private async Task<int> CountAgreementAsync()
        {
            var total = await _context.VLoanRequestContracts
                .Where(c => c.DebtorStaffId == StaffID)
                .Where(c => StutusID.Contains(c.CurrentStatusId!.Value))
                .CountAsync();
            return total;
        }

        protected void SetUserView(int count)
        {
            if (count > 0)
            {
                Footer.Count = count;
                Footer.TotalPages = (int)Math.Ceiling(count / (double)Footer.Limit);
            }
        }

        protected async Task SelectPageSizeAsync(ChangeEventArgs e)
        {
            Footer.Limit = Convert.ToInt32(e.Value!.ToString());
            Footer.TotalPages = (int)Math.Ceiling(Footer.Count / (double)Footer.Limit);
            Footer.CurrentPage = 1;
            await UpdateListAsync(Footer.CurrentPage);
        }

        protected async Task UpdateListAsync(int CurPage)
        {
            var statr = (Footer.Limit * CurPage) - Footer.Limit;
            Footer.CurrentPage = CurPage;
            await DataTableAsync(statr, Footer.Limit);
        }

        protected async Task NavigateToAsync(string Direction)
        {
            if (Direction == "Prev" && Footer.CurrentPage != 1)
            {
                Footer.CurrentPage -= 1;
            }
            if (Direction == "Next" && Footer.CurrentPage != Footer.TotalPages)
            {
                Footer.CurrentPage += 1;
            }
            if (Direction == "First")
            {
                Footer.CurrentPage = 1;
            }
            if (Direction == "Last")
            {
                Footer.CurrentPage = Footer.TotalPages;
            }

            await UpdateListAsync(Footer.CurrentPage);
        }

        protected async Task SelectCurrentPageAsync(ChangeEventArgs e)
        {
            Footer.CurrentPage = Convert.ToInt32(e.Value!.ToString());
            await UpdateListAsync(Footer.CurrentPage);
        }

        private async Task DataTableAsync(int start, int end)
        {
            ListAgreement = new();
            try
            {
                ListAgreement = await _context.VLoanRequestContracts
                        .Where(c => c.DebtorStaffId == StaffID)
                        .Where(c => StutusID.Contains(c.CurrentStatusId!.Value))
                        .Skip(start)
                        .Take(end)
                        .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void BackPage()
        {
            if (!string.IsNullOrEmpty(StaffID))
            {
                if (Role == "Manager")
                {
                    navigationManager.NavigateTo($"Manager/CheckLoanpage/{StaffID}");
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

            if (Role == "Manager")
            {
                navigationManager.NavigateTo($"/Manager/AgreementDetailPage/{(int)PageControl.AdminCheckRequestAgreement}/{StaffID}/{LoanRequestId}");
            }
            else
            {
                navigationManager.NavigateTo($"/Admin/AgreementDetailPage/{(int)PageControl.AdminCheckRequestAgreement}/{StaffID}/{LoanRequestId}");
            }

        }
    }
}
