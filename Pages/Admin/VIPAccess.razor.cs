using LoanApp.DatabaseModel.LoanEntities;

using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.Admin
{
    public partial class VIPAccess
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;

        #endregion

        private List<LoanStaffWorkingSpecial> NewWorkingSpecial { get; set; } = new();
        private IQueryable<LoanStaffWorkingSpecial> Query { get; set; } = null!;

        private ApplyLoanModel Search { get; set; } = new();
        private List<LoanStaffWorkingSpecial> ListWorkingSpecial { get; set; } = new();
        private List<VLoanStaffDetail> ListStaffDetail { get; set; } = new();
        private VLoanStaffDetail? StaffDetail { get; set; } = new();
        private MonthModel model_month { get; set; } = new();
        private PanelFooterModel Footer { get; set; } = new();
        private LoanStaffWorkingSpecial RemoveSpecial { get; set; } = new();

        private bool CheckSearchVeiw { get; set; } = false;
        //private bool OptionSearchName { get; set; } = true;
        private string? SearchView { get; set; } = string.Empty;
        private string? SearchTable { get; set; } = null;
        private string Remark { get; set; } = string.Empty;
        private int _pageIndex { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                RefreshQuery();

                //await StartTableAsync();
                await DataTableV2(SearchTable);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private void RefreshQuery()
        {
            string? adminCampId = StateProvider?.CurrentUser.CapmSelectNow;

            Query = _context.LoanStaffWorkingSpecials
                .Where(c => string.IsNullOrEmpty(adminCampId) || c.CampusId == adminCampId)
                .Where(c => c.Status == 1);
        }

        private async Task OnSearch(string? text)
        {
            string? adminCampId = StateProvider?.CurrentUser.CapmSelectNow;

            ListStaffDetail = new();
            StaffDetail = new();
            Search.Debtor = !string.IsNullOrEmpty(text) ? text : string.Empty;
            Remark = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    //Search.OptionSearchName = OptionSearchName;
                    Search.OptionSearchName = true;

                    if ((text.Trim()).Length < Utility.SearchMinlength)
                    {
                        await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
                    }
                    else
                    {
                        text = text.Trim();

                        ListStaffDetail = await _context.VLoanStaffDetails
                            .Where(c => string.IsNullOrEmpty(adminCampId) || c.CampId == adminCampId)
                            .Where(c => c.StaffDepart == "3" &&
                            (c.StaffNameThai!.Contains(text) ||
                            c.StaffSnameThai!.Contains(text) ||
                            (c.StaffNameEng!).ToLower().Contains(text.ToLower()) ||
                            (c.StaffSnameEng!).ToLower().Contains(text.ToLower()) ||
                            (c.StaffNameThai + " " + c.StaffSnameThai).Contains(text) ||
                            (c.StaffNameEng + " " + c.StaffSnameEng).ToLower().Contains(text.ToLower()) ||
                            c.StaffId.Contains(text)))
                            .ToListAsync();
                    }
                }

                CheckSearchVeiw = ListStaffDetail.Any() ? false : true;
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private void ChangeValStaff(VLoanStaffDetail people, bool option)
        {
            ListStaffDetail = new();
            StaffDetail = new();

            var StaffId = people.StaffId;
            var fullName = userService.GetFullName(StaffId);

            if (option)
            {
                Search.Debtor = fullName;
                SearchView = fullName;
            }
            else
            {
                Search.Debtor = StaffId;
                SearchView = StaffId;
            }
            Search.DebtorId = StaffId;
            StaffDetail = userService.GetUserDetail(StaffId);
        }

        private string ChangeDate(string? StringDate, string[] selectMonth)
        {
            var ShowDate = string.Empty;
            DateModel Date = Utility.ChangeDateMonth(StringDate, selectMonth);
            if (!string.IsNullOrEmpty(Date.Day))
            {
                ShowDate = $"{Date.Day} {Date.Month} {Date.Year}";
            }
            return ShowDate;
        }

        private async Task DataTableV2(string? staffId)
        {
            NewWorkingSpecial = new();

            try
            {
                NewWorkingSpecial = await Query
                    .Where(c => string.IsNullOrEmpty(staffId) || c.StaffId!.Contains(staffId))
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// true ขณะนี้กำลังเปิดใช้งานอยู่
        /// </summary>
        /// <param name="staffId"></param>
        /// <returns></returns>
        private async Task<bool> CheckPeopleSpecial(string staffId)
        {
            LoanStaffWorkingSpecial? workingSpecial = await psuLoan.GetLoanStaffWorkingSpecialByStaffId(staffId, 1);

            return workingSpecial != null ? true : false;
        }

        private async Task SaveToDbAsync(VLoanStaffDetail? staffDetail, string remark)
        {
            if (staffDetail == null)
            {
                return;
            }

            try
            {
                LoanStaffWorkingSpecial special = new()
                {
                    StaffId = staffDetail.StaffId,
                    Status = 1,
                    Remark = remark,
                    AdminStaffId = userService.FindStaffId(StateProvider?.CurrentUser.StaffId),
                    CampusId = staffDetail.CampusId,
                };

                //_context.LoanStaffWorkingSpecials.Add(special);
                //await _context.SaveChangesAsync();

                await psuLoan.AddLoanStaffWorkingSpecial(special);
                StaffDetail = new();
                SearchView = string.Empty;

                //await StartTableAsync();
                RefreshQuery();
                await DataTableV2(SearchTable);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task RemovePeopleSpecialAsync(decimal Id)
        {
            try
            {
                string modifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

                LoanStaffWorkingSpecial? special = await psuLoan.GetLoanStaffWorkingSpecialById(Id);

                if (special != null)
                {
                    //_context.LoanStaffWorkingSpecials.Remove(special);
                    //await _context.SaveChangesAsync();

                    await psuLoan.DeleteLoanStaffWorkingSpecial(special);

                    await LogService.GetHisLoanStaffWorkingSpecialAsync(special.SpecialId, special?.StaffId, modifyBy);

                    //await StartTableAsync();
                    RefreshQuery();
                    await DataTableV2(SearchTable);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }
    }
}
