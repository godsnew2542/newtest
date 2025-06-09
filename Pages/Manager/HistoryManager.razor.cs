using LoanApp.DatabaseModel.LoanEntities;

using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Pages.Manager
{
    public partial class HistoryManager
    {
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        private List<VLoanRequestContract> ReqCon { get; set; } = new();
        private List<YearModel> Year { get; set; } = new();
        private List<HistoryManagerMonthModel> Month { get; set; } = new();
        private MonthModel Model_month { get; set; } = new();
        private FilterModel FilterOption { get; set; } = new();
        private PanelFooterModel Footer { get; set; } = new();

        private string StaffId { get; set; } = string.Empty;

        protected override void OnInitialized()
        {
            StaffId = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);

            if (!string.IsNullOrEmpty(StaffId))
            {
                StartTable();
                GetYear();
                GetMonth();
            }
        }

        private void GetYear()
        {
            Year = new();
            var Iyear = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveDate != null)
                .Select(std => std.ContractApproveDate!.Value.Year)
                .Distinct()
                .ToList();

            if (Iyear.Count != 0)
            {
                foreach (var i in Iyear)
                {
                    YearModel temrYear = new();
                    temrYear.Name = $"{i}";
                    temrYear.Year = i;
                    Year.Add(temrYear);
                }
            }
            Year.Insert(0, new YearModel() { Year = 0, Name = "ทุกปี" });
        }

        private void GetMonth()
        {
            Month = new()
            {
               new() { Month = 0, Name = "ทุกเดือน" },
               new() { Month = 1, Name = "มกราคม" },
               new() { Month = 2, Name = "กุมภาพันธ์" },
               new() { Month = 3, Name = "มีนาคม" },
               new() { Month = 4, Name = "เมษายน" },
               new() { Month = 5, Name = "พฤษภาคม" },
               new() { Month = 6, Name = "มิถุนายน" },
               new() { Month = 7, Name = "กรกฎาคม" },
               new() { Month = 8, Name = "สิงหาคม" },
               new() { Month = 9, Name = "กันยายน" },
               new() { Month = 10, Name = "ตุลาคม" },
               new() { Month = 11, Name = "พฤศจิกายน" },
               new() { Month = 12, Name = "ธันวาคม" },
            };
        }

        private string ChangeDate(DateTime? date)
        {
            string showDate = string.Empty;
            string fomathDate = "dd MMMM yyyy HH:mm น.";
            if (date != null)
            {
                DateTime oDate = dateService.ConvertToDateTime(date);
                showDate = dateService.ChangeDate(oDate, fomathDate, Utility.DateLanguage_TH);
            }
            return showDate;
        }

        private string GetLoanName(byte? ID)
        {
            var loan = userService.GetLoanType(ID);
            string loanName = userService.GetLoanName(loan);
            return loanName;
        }

        public void StartTable()
        {
            var total = CountVLoanRequestContracts();
            SetUserView(total);
            DataTable(0, Footer.Limit, FilterOption.Year, FilterOption.Month);
        }

        public int CountVLoanRequestContracts()
        {
            var total = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveStaffId == StaffId)
                .Count();
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

        public void DataTable(int start, int end, decimal? year, int month)
        {
            ReqCon = new();

            if (year != 0 && month != 0)
            {
                ReqCon = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveStaffId == StaffId)
                .Where(c => c.ContractApproveDate!.Value.Year == year)
                .Where(c => c.ContractApproveDate!.Value.Month == month)
                .Skip(start)
                .Take(end)
                .ToList();
            }
            else if (year != 0)
            {
                ReqCon = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveStaffId == StaffId)
                .Where(c => c.ContractApproveDate!.Value.Year == year)
                .Skip(start)
                .Take(end)
                .ToList();
            }
            else if (month != 0)
            {
                ReqCon = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveStaffId == StaffId)
                .Where(c => c.ContractApproveDate!.Value.Month == month)
                .Skip(start)
                .Take(end)
                .ToList();
            }
            else
            {
                var total = CountVLoanRequestContracts();
                SetUserView(total);
                ReqCon = _context.VLoanRequestContracts
                    .Where(c => c.ContractApproveStaffId == StaffId)
                    .Skip(start)
                    .Take(end)
                    .ToList();
            }
        }

        protected void SelectPageSize(ChangeEventArgs e)
        {
            Footer.Limit = Convert.ToInt32(e.Value!.ToString());
            Footer.TotalPages = (int)Math.Ceiling(Footer.Count / (double)Footer.Limit);
            Footer.CurrentPage = 1;
            UpdateList(Footer.CurrentPage);
        }

        protected void UpdateList(int CurPage)
        {
            var end = (Footer.Limit * CurPage);
            var statr = (Footer.Limit * CurPage) - Footer.Limit;
            Footer.CurrentPage = CurPage;
            DataTable(statr, Footer.Limit, FilterOption.Year, FilterOption.Month);
        }

        protected void NavigateTo(string Direction)
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

            UpdateList(Footer.CurrentPage);
        }

        protected void SelectCurrentPage(ChangeEventArgs e)
        {
            Footer.CurrentPage = Convert.ToInt32(e.Value!.ToString());
            UpdateList(Footer.CurrentPage);
        }

        private void SelectYear(ChangeEventArgs e)
        {
            FilterOption.Year = decimal.Parse($"{e.Value!}");
            if (!string.IsNullOrEmpty(StaffId))
            {
                SearchData(FilterOption.Year, FilterOption.Month);
            }
        }

        private void SelectMonth(ChangeEventArgs e)
        {
            FilterOption.Month = Convert.ToInt32(e.Value!.ToString());
            if (!string.IsNullOrEmpty(StaffId))
            {
                SearchData(FilterOption.Year, FilterOption.Month);
            }
        }

        public void SearchData(decimal? year, int month)
        {
            Footer.CurrentPage = 1;
            if (year != 0 && month != 0)
            {
                var total = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveStaffId == StaffId)
                .Where(c => c.ContractApproveDate!.Value.Year == year && c.ContractApproveDate.Value.Month == month)
                .Count();

                SumTable(total, year, month);
            }
            else if (year != 0)
            {
                var total = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveStaffId == StaffId)
                .Where(c => c.ContractApproveDate!.Value.Year == year)
                .Count();

                SumTable(total, year, month);
            }
            else if (month != 0)
            {
                var total = _context.VLoanRequestContracts
                .Where(c => c.ContractApproveStaffId == StaffId)
                .Where(c => c.ContractApproveDate!.Value.Month == month)
                .Count();

                SumTable(total, year, month);
            }
            else
            {
                StartTable();
            }
        }

        private void SumTable(int total, decimal? year, int month)
        {

            if (total != 0)
            {
                SetUserView(total);
                DataTable(0, Footer.Limit, year, month);
            }
            else
            {
                ReqCon = new List<VLoanRequestContract>();
                SetUserView(1);
            }
        }

        public void Back()
        {
            navigationManager.NavigateTo("./Manager/RequestDetailManager");
        }
    }

    public class YearModel
    {
        public decimal? Year { get; set; }
        public string Name { get; set; } = null!;
    }

    public class HistoryManagerMonthModel
    {
        public int Month { get; set; }
        public string Name { get; set; } = null!;
    }

    public class FilterModel
    {
        public decimal? Year { get; set; } = 0;
        public int Month { get; set; } = 0;
    }
}
