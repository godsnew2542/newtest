using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using LoanApp.Model.Models;
using LoanApp.Shared;
using static LoanApp.Pages.User.AgreementDetailPage;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using AntDesign.TableModels;
using NPOI.POIFS.Crypt.Dsig;

namespace LoanApp.Pages.Admin
{
    public partial class CheckAgreement
    {
        [CascadingParameter] public Error Error { get; set; } = null!;

        #region Parameter
        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public string Role { get; set; } = string.Empty;
        [Parameter] public int newRole { get; set; } = 0;
        [Parameter] public decimal rootRequestID { get; set; } = 0;

        #endregion

        [Inject] Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<VLoanRequestContract> ListAgreement { get; set; } = new();
        private List<VLoanRequestContract> ListAgreementSuccess { get; set; } = new();
        //private PanelFooterModel Footer { get; set; } = new()
        //private PanelFooterModel FooterSuccess { get; set; } = new()

        private decimal[] StutusID { get; set; } = new[] { 0m, 1m, 2m, 3m, 4m, 5m, 99m, 98m };
        private decimal[] StutusIDSuccess { get; set; } = { 3m, 99m, 98m };

        //protected async override Task OnInitializedAsync()
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(StaffID))
        //        {
        //            await StartTableAsync();
        //            await StartTableSueecssAsync();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await Error.ProcessError(ex);
        //    }
        //}

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (!string.IsNullOrEmpty(StaffID))
                    {
                        //ListAgreement = await _context.VLoanRequestContracts
                        //    .Where(c => c.DebtorStaffId == StaffID)
                        //    .Where(c => !StutusID.Contains(c.CurrentStatusId!.Value))
                        //    .Where(c => (c.ContractDate == null) || ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                        //    .ToListAsync()

                        //ListAgreementSuccess = await _context.VLoanRequestContracts
                        //    .Where(c => c.DebtorStaffId == StaffID)
                        //    .Where(c => StutusIDSuccess.Contains(c.CurrentStatusId!.Value))
                        //    .Where(c => (c.ContractDate == null) ||
                        //    ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                        //    .ToListAsync()

                        ListAgreement = await psuLoan.GetListAgreementDataFormVLoanRequestContractByNotStatusReq(StaffID, StutusID.ToList(), Utility.ShowDataYear);

                        ListAgreementSuccess = await psuLoan.GetListAgreementDataFormVLoanRequestContract(StaffID, StutusIDSuccess.ToList(), Utility.ShowDataYear);

                        StateHasChanged();
                    }
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
            }
        }

        //private async Task StartTableAsync()
        //{
        //    try
        //    {
        //        //var total = CountAgreement()
        //        var total = await psuLoan.GetListAgreementDataFormVLoanRequestContractByNotStatusReq(StaffID, StutusID.ToList(), Utility.ShowDataYear);

        //        SetUserView(total.Count());
        //        await DataTableAsync(0, Footer.Limit);
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //private int CountAgreement()
        //{
        //    var total = _context.VLoanRequestContracts
        //        .Where(c => c.DebtorStaffId == StaffID)
        //        .Where(c => !StutusID.Contains(c.CurrentStatusId!.Value))
        //        .Where(c => (c.ContractDate == null) ||
        //        ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
        //        .Count();
        //    return total;
        //}

        //protected void SetUserView(int count)
        //{
        //    if (count > 0)
        //    {
        //        Footer.Count = count;
        //        Footer.TotalPages = (int)Math.Ceiling(count / (double)Footer.Limit);
        //    }
        //}

        //protected async Task SelectPageSizeAsync(ChangeEventArgs e)
        //{
        //    Footer.Limit = Convert.ToInt32(e.Value!.ToString());
        //    Footer.TotalPages = (int)Math.Ceiling(Footer.Count / (double)Footer.Limit);
        //    Footer.CurrentPage = 1;
        //    await UpdateListAsync(Footer.CurrentPage);
        //}

        //protected async Task UpdateListAsync(int CurPage)
        //{
        //    var statr = (Footer.Limit * CurPage) - Footer.Limit;
        //    Footer.CurrentPage = CurPage;
        //    await DataTableAsync(statr, Footer.Limit);
        //}

        //protected async Task NavigateToAsync(string Direction)
        //{
        //    if (Direction == "Prev" && Footer.CurrentPage != 1)
        //    {
        //        Footer.CurrentPage -= 1;
        //    }
        //    if (Direction == "Next" && Footer.CurrentPage != Footer.TotalPages)
        //    {
        //        Footer.CurrentPage += 1;
        //    }
        //    if (Direction == "First")
        //    {
        //        Footer.CurrentPage = 1;
        //    }
        //    if (Direction == "Last")
        //    {
        //        Footer.CurrentPage = Footer.TotalPages;
        //    }

        //    await UpdateListAsync(Footer.CurrentPage);
        //}

        //protected async Task SelectCurrentPageAsync(ChangeEventArgs e)
        //{
        //    Footer.CurrentPage = Convert.ToInt32(e.Value!.ToString());
        //    await UpdateListAsync(Footer.CurrentPage);
        //}

        //private async Task DataTableAsync(int start, int end)
        //{
        //    ListAgreement = new();
        //    try
        //    {
        //        ListAgreement = await _context.VLoanRequestContracts
        //                .Where(c => c.DebtorStaffId == StaffID)
        //                .Where(c => !StutusID.Contains(c.CurrentStatusId!.Value))
        //                .Where(c => (c.ContractDate == null) ||
        //                ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
        //                .Skip(start)
        //                .Take(end)
        //                .ToListAsync();
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //private async Task StartTableSueecssAsync()
        //{
        //    try
        //    {
        //        var total = CountAgreementSuccess();
        //        SetUserViewSuccess(total);
        //        await DataTableSuccessAsync(0, FooterSuccess.Limit);
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //private int CountAgreementSuccess()
        //{
        //    var total = _context.VLoanRequestContracts
        //        .Where(c => c.DebtorStaffId == StaffID)
        //        .Where(c => StutusIDSuccess.Contains(c.CurrentStatusId!.Value))
        //        .Where(c => (c.ContractDate == null) ||
        //        ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
        //        .Count();
        //    return total;
        //}

        //protected void SetUserViewSuccess(int count)
        //{
        //    if (count > 0)
        //    {
        //        FooterSuccess.Count = count;
        //        FooterSuccess.TotalPages = (int)Math.Ceiling(count / (double)FooterSuccess.Limit);
        //    }
        //}

        //protected async Task SelectPageSizeSuccessAsync(ChangeEventArgs e)
        //{
        //    FooterSuccess.Limit = Convert.ToInt32(e.Value!.ToString());
        //    FooterSuccess.TotalPages = (int)Math.Ceiling(FooterSuccess.Count / (double)FooterSuccess.Limit);
        //    FooterSuccess.CurrentPage = 1;
        //    await UpdateListSueecssAsync(FooterSuccess.CurrentPage);
        //}

        //protected async Task UpdateListSueecssAsync(int CurPage)
        //{
        //    var statr = (FooterSuccess.Limit * CurPage) - FooterSuccess.Limit;
        //    FooterSuccess.CurrentPage = CurPage;
        //    await DataTableSuccessAsync(statr, FooterSuccess.Limit);
        //}

        //protected async Task NavigateToSuccessAsync(string Direction)
        //{
        //    if (Direction == "Prev" && FooterSuccess.CurrentPage != 1)
        //    {
        //        FooterSuccess.CurrentPage -= 1;
        //    }
        //    if (Direction == "Next" && FooterSuccess.CurrentPage != FooterSuccess.TotalPages)
        //    {
        //        FooterSuccess.CurrentPage += 1;
        //    }
        //    if (Direction == "First")
        //    {
        //        FooterSuccess.CurrentPage = 1;
        //    }
        //    if (Direction == "Last")
        //    {
        //        FooterSuccess.CurrentPage = FooterSuccess.TotalPages;
        //    }

        //    await UpdateListSueecssAsync(FooterSuccess.CurrentPage);
        //}

        //protected async Task SelectCurrentPageSuccessAsync(ChangeEventArgs e)
        //{
        //    FooterSuccess.CurrentPage = Convert.ToInt32(e.Value!.ToString());
        //    await UpdateListSueecssAsync(FooterSuccess.CurrentPage);
        //}

        //private async Task DataTableSuccessAsync(int start, int end)
        //{
        //    ListAgreementSuccess = new();

        //    try
        //    {
        //        ListAgreementSuccess = await _context.VLoanRequestContracts
        //             .Where(c => c.DebtorStaffId == StaffID)
        //             .Where(c => StutusIDSuccess.Contains(c.CurrentStatusId!.Value))
        //             .Where(c => (c.ContractDate == null) ||
        //             ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
        //             .Skip(start)
        //             .Take(end)
        //             .ToListAsync();
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        private void BackPage()
        {
            if (!string.IsNullOrEmpty(StaffID))
            {
                if (Role == RoleTypeEnum.Manager.ToString())
                {
                    navigationManager.NavigateTo($"{RoleTypeEnum.Manager}/CheckLoanpage/{StaffID}");
                }
                else if (newRole == 3) /// newRole == LoanApp.Models.RoleTypeEnum (enum)
				{
                    if (rootRequestID != 0)
                    {
                        navigationManager.NavigateTo($"{RoleTypeEnum.Admin}/RequestDetail/{rootRequestID}");
                    }
                }
                else
                {
                    navigationManager.NavigateTo($"{RoleTypeEnum.Admin}/CheckLoanpage/{StaffID}");
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
                navigationManager.NavigateTo($"/{RoleTypeEnum.Manager}/AgreementDetailPage/{(int)PageControl.AdminCheckAgreement}/{StaffID}/{LoanRequestId}");
            }
            else if (newRole == 3) /// newRole == LoanApp.Models.RoleTypeEnum (enum)
            {
                if (rootRequestID != 0)
                {
                    navigationManager.NavigateTo($"/{newRole}/AgreementDetailPage/{(int)PageControl.AdminCheckAgreement}/{StaffID}/{LoanRequestId}/{(int)BackRootPageEnum.Admin_RequestDetail}/{rootRequestID}");
                }
            }
            else
            {
                navigationManager.NavigateTo($"/{RoleTypeEnum.Admin}/AgreementDetailPage/{(int)PageControl.AdminCheckAgreement}/{StaffID}/{LoanRequestId}");
            }
        }

        /// <summary>
        /// check status 200
        /// </summary>
        /// <param name="loanRequestId"></param>
        /// <param name="isNewupload">true == status 200</param>
        private void UploadAgreementPremise(decimal loanRequestId, bool isNewupload = false)
        {
            if (!isNewupload)
            {
                navigationManager.NavigateTo($"/{RoleTypeEnum.Admin}/AgreementPremise/{loanRequestId}/{3}/{(int)PageControl.AdminCheckAgreement}");
            }
            else
            {
                navigationManager.NavigateTo($"/{RoleTypeEnum.Admin}/AgreementPremise/{loanRequestId}/{3}/{(int)PageControl.AdminCheckAgreement}/{isNewupload}");
            }
        }

        private void OnRowClick(RowData<VLoanRequestContract> row)
        {
            List<decimal?> notResPage = new() { 6, 200 };

            if (!notResPage.Contains(row.Data.CurrentStatusId))
            {
                TopageAgreementDetailPage(row.Data.LoanRequestId);
            }
        }
    }
}
