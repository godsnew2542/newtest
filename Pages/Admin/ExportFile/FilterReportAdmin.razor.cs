using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;

using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Models.Data;
using LoanApp.Services.Services.LoanDb;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace LoanApp.Pages.Admin.ExportFile
{
    public partial class FilterReportAdmin
    {
        [CascadingParameter] public Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; } = null!;

        [Parameter] public string Role { get; set; } = string.Empty;

        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<LoanType> Loans { get; set; } = new();
        private List<YearModel> LoanYear { get; set; } = new();
        private SelectYearModel SelectYear { get; set; } = new();
        private List<CCampus> ListCampus { get; set; } = new();

        /// <summary>
        /// list ประเภทกู้ยืม
        /// </summary>
        private List<byte> SelectLoanTypeId { get; set; } = new();

        private string StorageName { get; set; } = "ReportAdmin";
        //private int LessthanYear { get; } = 5
        private bool IsShowCampus { get; set; } = false;
        private DateTime DataTimeNow { get; set; } = DateTime.Now;
        private decimal? fiscalYear { get; set; } = null;

        private bool isError { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Loans = await _context.LoanTypes.ToListAsync();
                ListCampus = EntitiesCentralService.GetCampusList();
                LoanYear = await SetDefaultAsync();

                var myTodo = ListCampus.Find(x => x.CampId == "00");

                if (myTodo != null)
                {
                    ListCampus.Remove(myTodo);
                }

                ListCampus.Insert(0, new CCampus() { CampId = string.Empty, CampNameThai = "เลือกวิทยาเขต" });

                if (LoanYear.Count != 0)
                {
                    LoanYear.Insert(0, new YearModel() { Year = 0, Name = "เลือกปี" });
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow))
                {
                    try
                    {
                        fiscalYear = userService.GetFiscalYear(DataTimeNow);

                        SelectYear = await CheckDataInStorageAsync();
                        //await CheckAdminSetUpAsync();

                        SelectYear.CampId = StateProvider!.CurrentUser.CapmSelectNow == "00" ? string.Empty : StateProvider!.CurrentUser.CapmSelectNow;

                        IsShowCampus = StateProvider!.CurrentUser.CapmSelectNow == "00" ? true : false;

                        if (!string.IsNullOrEmpty(SelectYear.CampId))
                        {
                            int index_CampId = ListCampus.FindIndex(a => a.CampId == SelectYear.CampId);
                            await JS.InvokeVoidAsync("SelectedTagID", "camp-id", index_CampId);

                            int index_StartYear = LoanYear.FindIndex(a => a.Year == SelectYear.Start);
                            await JS.InvokeVoidAsync("SelectedTagID", "start-year", index_StartYear);

                            int index_EndYear = LoanYear.FindIndex(a => a.Year == SelectYear.End);
                            await JS.InvokeVoidAsync("SelectedTagID", "end-year", index_EndYear);

                            for (int i = 0; i < SelectYear.SelectLoanTypeId.Count; i++)
                            {
                                var loanTypeId = SelectYear.SelectLoanTypeId[i];

                                SelectLoanTypeId.Add(loanTypeId);
                                await JS.InvokeVoidAsync("SetCheckedCheckBox", loanTypeId);
                            }
                        }
                        StateHasChanged();
                    }
                    catch (Exception ex)
                    {
                        await Error.ProcessError(ex);
                        throw;
                    }
                }
                else
                {
                    isError = true;
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// เช็ค Admin camp
        /// </summary>
        /// <returns></returns>
        private async Task CheckAdminSetUpAsync()
        {
            List<DTEventArgs> AccessRightsList = new();

            var role = await localStorage.GetItemAsync<string>("role");
            string staffId = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

            AccessRightsList.Add(SetAccessRights("adminCampus", 2));
            AccessRightsList.Add(SetAccessRights("manager", 3));

            if (!string.IsNullOrEmpty(staffId))
            {
                if (!string.IsNullOrEmpty(role))
                {
                    for (int i = 0; i < AccessRightsList.Count; i++)
                    {
                        var AccessRights = AccessRightsList[i].Params;
                        string AccessRoleName = $"{AccessRights[0]}";
                        decimal AccessRoleId = (decimal)AccessRights[1];

                        if (role.Equals(AccessRoleName))
                        {
                            SelectYear.CampId = "01";
                            IsShowCampus = false;
                            //lStaffPrivilege = userService.GetRole(staffId)

                            //LoanStaffPrivilege? staffPrivilege = await _context.LoanStaffPrivileges
                            //    .Where(c => c.StaffId == staffId)
                            //    .Where(c => c.Active == 1)
                            //    .Where(c => c.GroupId == AccessRoleId)
                            //    .FirstOrDefaultAsync()

                            LoanStaffPrivilege? staffPrivilege = await psuLoan.CheckPeopleLoanStaffPrivilege(staffId, null, AccessRoleId, 1);

                            if (staffPrivilege != null)
                            {
                                SelectYear.CampId = !string.IsNullOrEmpty(staffPrivilege.CampId) ? staffPrivilege.CampId : string.Empty;
                            }
                            else
                            {
                                var staffDetail = userService.GetUserDetail(staffId);
                                if (staffDetail?.CampId != null)
                                {
                                    SelectYear.CampId = staffDetail.CampId;
                                }
                                string alert = $"ขณะนี้คุณได้เข้าดูในโหมด Dev => CheckAdminSetUpAsync(), DevTestGetCamId(staffId)";
                                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                            }
                        }
                    }
                }
                else { }
            }
            else { }
        }

        private DTEventArgs SetAccessRights(string roleName, decimal roleId)
        {
            DTEventArgs AccessRights = new();
            AccessRights.Params.Add(roleName);
            AccessRights.Params.Add(roleId);
            return AccessRights;
        }

        private async Task<SelectYearModel> CheckDataInStorageAsync()
        {
            SelectYearModel select = new();
            var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
            if (!string.IsNullOrEmpty(checkData))
            {
                select = await sessionStorage.GetItemAsync<SelectYearModel>(StorageName);
                StateHasChanged();
            }
            else { }
            return select;
        }

        private async Task<List<YearModel>> SetDefaultAsync()
        {
            List<YearModel> years = new();
            decimal[] status = new[] { 0m, 1m, 2m, 3m, 4m, 100m };

            //var checkYear = await _context.ContractMains
            //    .Where(c => !status.Contains(c.ContractStatusId!.Value))
            //    .Where(c => (c.PaidDate != null ? c.PaidDate.Value.Year >= Utility.ShowDataYear : true) ||
            //    (c.ContractDate != null ? c.ContractDate.Value.Year >= Utility.ShowDataYear : true))
            //    .Select(c => new CheckReportYear
            //    {
            //        Paid = c.PaidDate != null ? c.PaidDate.Value.Year : null,
            //        Contract = c.ContractDate != null ? c.ContractDate.Value.Year : null,
            //    })
            //    .Distinct()
            //    .ToListAsync();

            var checkYear = await psuLoan.GetListCheckReportYearFormContractMain(status.ToList(), Utility.ShowDataYear);

            List<int> resultYear = new();
            foreach (var item in checkYear)
            {
                if (item.Paid != null && item.Paid >= Utility.ShowDataYear)
                {
                    resultYear.Add(item.Paid.Value);
                }
                else if (item.Contract != null && item.Contract >= Utility.ShowDataYear)
                {
                    resultYear.Add(item.Contract.Value);
                }
            }

            if (resultYear.Count != 0)
            {
                var Max = resultYear.Distinct().Max(t => t) + 1;
                var Min = resultYear.Distinct().Min(t => t) - 1;
                var length = Max - Min + 1;

                for (int i = 0; i < length; i++)
                {
                    var year = Min + i;
                    YearModel model = new()
                    {
                        Year = year,
                        Name = $"{year + 543}",
                    };
                    years.Add(model);
                }
            }
            return years;
        }

        private async Task SetOrClearCheckedAsync(bool data)
        {
            for (int i = 0; i < Loans.Count; i++)
            {
                var loan = Loans[i];

                if (data)
                {
                    bool isExist = SelectLoanTypeId.Contains(loan.LoanTypeId);
                    if (!isExist)
                    {
                        SelectLoanTypeId.Add(loan.LoanTypeId);
                        await JS.InvokeVoidAsync("SetCheckedCheckBox", loan.LoanTypeId);
                    }
                }
                else
                {
                    bool isExist = SelectLoanTypeId.Contains(loan.LoanTypeId);
                    if (isExist)
                    {
                        SelectLoanTypeId.Remove(loan.LoanTypeId);
                        await JS.InvokeVoidAsync("ClearCheckedCheckBox", loan.LoanTypeId);
                    }
                }

            }
        }

        private string GetLoanActive(int active)
        {
            var ActiveName = "ปิด";

            if (active == 1)
            {
                ActiveName = "เปิด";
            }
            return ActiveName;
        }

        private void CheckboxClicked(byte loanTypeId, object checkedValue)
        {
            if ((bool)checkedValue)
            {
                if (!SelectLoanTypeId.Contains(loanTypeId))
                {
                    SelectLoanTypeId.Add(loanTypeId);
                }
            }
            else
            {
                if (SelectLoanTypeId.Contains(loanTypeId))
                {
                    SelectLoanTypeId.Remove(loanTypeId);
                }
            }
        }

        protected void SelectStartYear(ChangeEventArgs e)
        {
            SelectYear.Start = Convert.ToDecimal(e.Value!.ToString());
            SelectYear.End = Convert.ToDecimal(e.Value.ToString());
        }

        protected void SelectEndYear(ChangeEventArgs e)
        {
            SelectYear.End = Convert.ToDecimal(e.Value!.ToString());
        }

        protected void SelectCampus(ChangeEventArgs e)
        {
            SelectYear.CampId = $"{e.Value}";
        }

        private async Task OpenReportAsync()
        {
            var PassSelectYear = CheckSelectYear(SelectYear);
            //var IsLessthanYear5 = CheckLessthanYear5(SelectYear)

            if (string.IsNullOrEmpty(SelectYear.CampId))
            {
                string alert = $"กรุณาเลือกวิทยาเขตที่ต้องการ";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                return;
            }

            if (!PassSelectYear)
            {
                string alert = $"กรุณาระบุปีให้ถูกต้องสมบรูณ์";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                return;
            }

            if (SelectLoanTypeId.Count == 0)
            {
                string alert = $"กรุณาเลือกประเภทเงินกู้ที่ต้องการ";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                return;
            }

            //if (!IsLessthanYear5)
            //{
            //    string alert = $"ไม่สามารถทำรายการได้เนื่องจากคุณเลือกปีที่ต้องการเกิน 5 ปี";
            //    await JS.InvokeVoidAsync("displayTickerAlert", alert);
            //    return;
            //}

            SelectYear.SelectLoanTypeId = SelectLoanTypeId;
            await sessionStorage.SetItemAsync(StorageName, SelectYear);
            navigationManager.NavigateTo("/Admin/PdfAll");
        }

        private void BackPage()
        {
            if (IsShowCampus)
            {
                navigationManager.NavigateTo("/Admin/AdminUniHome");
            }
            else
            {
                if (Role == "Manager")
                {
                    navigationManager.NavigateTo("/Manager/ManagerHome");
                }
                else
                {
                    navigationManager.NavigateTo("/Admin/AdminHome");
                }
            }
        }

        /// <summary>
        /// เช็คตามปีที่เลือก
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        private bool CheckSelectYear(SelectYearModel year)
        {
            bool pass = false;

            if (year.Start != 0)
            {
                pass = true;
            }
            return pass;
        }

        //private bool CheckLessthanYear5(SelectYearModel year)
        //{
        //    bool pass = false;
        //    var Ystart = SelectYear.Start + LessthanYear;

        //    if (SelectYear.Start != 0 || SelectYear.End != 0)
        //    {
        //        if (Ystart >= SelectYear.End)
        //        {
        //            pass = true;
        //        }
        //    }
        //    return pass;
        //}
    }

    public class YearModel
    {
        /// <summary>
        /// คศ
        /// </summary>
        public decimal? Year { get; set; }
        /// <summary>
        /// พศ
        /// </summary>
        public string Name { get; set; } = null!;
    }

    public class SelectYearModel
    {
        public decimal Start { get; set; } = 0;
        public decimal End { get; set; } = 0;
        public List<byte> SelectLoanTypeId { get; set; } = new();
        public string CampId { get; set; } = string.Empty;
    }
}
