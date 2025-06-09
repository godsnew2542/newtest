using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.AdminCenter
{
    public partial class ManageTypeLoan
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<LoanType> ListLoan { get; set; } = new();
        private List<LoanType> TempListLoan { get; set; } = new();
        private LoanType SelectLoan { get; set; } = new();
        private LoanType SearchLoan { get; set; } = new();

        private bool Isloading { get; set; } = false;
        private string? searchValue { get; set; } = null;


        protected override void OnInitialized()
        {
            Isloading = false;
            SearchLoan.LoanParentName = string.Empty;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    var orderByOpen = await GetLoanActiveAsync(1);
                    var orderByClose = await GetLoanActiveAsync(0);
                    AddListLoan(orderByOpen);
                    AddListLoan(orderByClose);
                    ListLoan = TempListLoan;

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
            }

        }

        private void OnEnter(string? changedString)
        {
            Submit(changedString);
        }

        private async Task<List<LoanType>> GetLoanActiveAsync(int id)
        {
            try
            {
                return await psuLoan.GetAllLoanType(id, orderBy: 2);
                //return await _context.LoanTypes
                //    .Where(c => c.Active == id)
                //    .OrderBy(c => c.LoanParentId)
                //    .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }

        }

        private void AddListLoan(List<LoanType> data)
        {
            if (data.Any())
            {
                for (int i = 0; i < data.Count; i++)
                {
                    TempListLoan.Add(data[i]);
                }
            }
        }

        private void CreateLoan()
        {
            navigationManager.NavigateTo("/Admin/CreateTypeLoan");
        }

        private void DocumentStep()
        {
            navigationManager.NavigateTo("/Admin/ManageDocument");
        }

        private void DetailPage(LoanType item)
        {
            navigationManager.NavigateTo($"/Admin/DetailPage/{item.LoanTypeId}");
        }

        private void Submit(string? name)
        {
            //ListLoan = _context.LoanTypes
            //    .Where(c => c.LoanParentName.Contains(name))
            //    .ToList();
            if (!string.IsNullOrEmpty(name))
            {
                ListLoan = TempListLoan
                    .Where(c => c.LoanParentName.ToLower().Contains(name.ToLower()) ||
                    c.LoanTypeName.ToLower().Contains(name.ToLower()))
                    .ToList();
            }
            else
            {
                ListLoan = TempListLoan;
            }
        }

        private async Task CheckedDataAsync(LoanType data)
        {
            try
            {
                Isloading = true;
                var loan = await _context.LoanTypes
                    .Where(c => c.LoanTypeId == data.LoanTypeId)
                    .FirstOrDefaultAsync();
                if (loan != null)
                {
                    loan.Active = data.Active == 1 ? 0 : 1;

                    _context.Update(loan);
                    await _context.SaveChangesAsync();
                    navigationManager.NavigateTo("/Admin/ManageTypeLoan", true);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private string GetLoanMaxAmount(decimal? Amount)
        {
            var LoanMaxAmount = string.Empty;
            if (Amount == 0)
            {
                LoanMaxAmount = "ไม่จำกัด";
            }
            else
            {
                LoanMaxAmount = $"{Convert.ToString(string.Format("{0:n2}", Amount))}";
            }
            return LoanMaxAmount;
        }

        private string GetLoanInterest(decimal? Interest)
        {
            var LoanInterest = string.Empty;
            if (Interest == 0)
            {
                LoanInterest = "ไม่มี";
            }
            else
            {
                LoanInterest = $"{Interest}";
            }
            return LoanInterest;
        }
    }
}
