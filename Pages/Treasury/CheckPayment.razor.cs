using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Pages.Treasury
{
    public partial class CheckPayment
    {
        private SearchModel Search { get; set; } = new();
        private List<ContractStatus> Status { get; set; } = new();
        private List<VLoanRequestContract> ReqCon { get; set; } = new();
        private PanelFooterModel Footer { get; set; } = new();

        private Decimal StatusID { get; set; } = 0;
        private Decimal StaId { get; set; } = 0;
        public string SearchView { get; set; } = string.Empty;
        public decimal[] AllowedStatus { get; set; } = new decimal[0];

        protected override void OnInitialized()
        {
            AllowedStatus = new[] { 6m, 7m, 8m, 80m, 81m, 82m, 99m };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (StatusID != 0)
                {
                    int index = Status.FindIndex(a => a.ContractStatusId == StatusID);
                    await JS.InvokeVoidAsync("SelectedTagName", index);
                }
                StateHasChanged();
            }
        }

        public void StartTable()
        {
            var total = CountVLoanRequestContracts();
            SetUserView(total);
            DataTable(0, Footer.Limit, Search.Title, StaId);
        }

        public int CountVLoanRequestContracts()
        {
            var total = _context.VLoanRequestContracts
                .Where(c => AllowedStatus.Contains(c.CurrentStatusId!.Value))
                .Count();
            return total;
        }

        public void DataTable(int start, int end, string? searchName, Decimal StatusID)
        {
            ReqCon = new();


            if (!string.IsNullOrEmpty(searchName) && StatusID != 0)
            {
                ReqCon = _context.VLoanRequestContracts
                    .Where(c => c.DebtorNameTh!.Contains(searchName) ||
                        c.DebtorSnameTh!.Contains(searchName) ||
                        (c.DebtorNameEng!).ToLower().Contains(searchName.ToLower()) ||
                        (c.DebtorSnameEng!).Contains(searchName.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower()))
                    .Where(c => c.CurrentStatusId == StatusID)
                    .Skip(start)
                    .Take(end)
                    .ToList();
            }
            else if (!string.IsNullOrEmpty(searchName))
            {
                ReqCon = _context.VLoanRequestContracts
                   .Where(c => c.DebtorNameTh!.Contains(searchName) ||
                        c.DebtorSnameTh!.Contains(searchName) ||
                        (c.DebtorNameEng!).ToLower().Contains(searchName.ToLower()) ||
                        (c.DebtorSnameEng!).Contains(searchName.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower()))
                    .Where(c => AllowedStatus.Contains(c.CurrentStatusId!.Value))
                    .Skip(start)
                    .Take(end)
                    .ToList();
            }
            else if (StatusID != 0)
            {
                ReqCon = _context.VLoanRequestContracts
                .Where(c => c.CurrentStatusId == StatusID)
                .Skip(start)
                .Take(end)
                .ToList();
            }
            else
            {
                var total = CountVLoanRequestContracts();
                ReqCon = _context.VLoanRequestContracts
                    .Where(c => AllowedStatus.Contains(c.CurrentStatusId!.Value))
                    .Skip(start)
                    .Take(end)
                    .ToList();
            }
        }

        protected void SetUserView(int count)
        {
            if (count > 0)
            {
                Footer.Count = count;
                Footer.TotalPages = (int)Math.Ceiling(count / (double)Footer.Limit);
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
            DataTable(statr, Footer.Limit, Search.Title, StaId);
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

        protected void SelectStatus(ChangeEventArgs e)
        {
            StaId = Convert.ToDecimal(e.Value!.ToString());
            SearchData(Search.Title, StaId);
        }
        public void SearchData(string? text, decimal StatusID)
        {
            Search.Title = text;
            Footer.CurrentPage = 1;
            if (!string.IsNullOrEmpty(text) && StatusID != 0)
            {
                var total = _context.VLoanRequestContracts
                    .Where(c => c.DebtorNameTh!.Contains(text) ||
                        c.DebtorSnameTh!.Contains(text) ||
                        (c.DebtorNameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorSnameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(text) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(text.ToLower()))
                    .Where(c => c.CurrentStatusId == StatusID)
                    .Count();
                SumTable(total, text, StatusID);
            }
            else if (!string.IsNullOrEmpty(text))
            {
                var total = _context.VLoanRequestContracts
                     .Where(c => c.DebtorNameTh!.Contains(text) ||
                        c.DebtorSnameTh!.Contains(text) ||
                        (c.DebtorNameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorSnameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(text) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(text.ToLower()))
                    .Where(c => AllowedStatus.Contains(c.CurrentStatusId!.Value))
                    .Count();
                SumTable(total, text, StatusID);
            }
            else if (StatusID != 0)
            {
                var total = _context.VLoanRequestContracts
                    .Where(c => c.CurrentStatusId == StatusID)
                    .Count();
                SumTable(total, text, StatusID);
            }
            else
            {
                StartTable();
            }
        }

        public void checkPayment()
        {
            navigationManager.NavigateTo("/Treasury/ManageCheckPayment");
        }

        private void SumTable(int total, string? text, decimal StatusID)
        {
            if (total != 0)
            {
                SetUserView(total);
                DataTable(0, Footer.Limit, text, StatusID);
            }
            else
            {
                ReqCon = new();
                SetUserView(1);
            }
        }

    }
}
