using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LoanApp.Shared;
using Microsoft.EntityFrameworkCore;
using LoanApp.DatabaseModel.LoanEntities;

namespace LoanApp.Pages.Admin
{
    public partial class CheckLoanAdmin
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        [Inject] Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private ApplyLoanModel ModelApplyLoan { get; set; } = new();
        private LoanType? Loan { get; set; } = new();

        private string StorageName { get; set; } = "CheckLoanpage";

        protected async override Task OnInitializedAsync()
        {
            try
            {
                var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
                if (!string.IsNullOrEmpty(checkData))
                {
                    ModelApplyLoan = await sessionStorage.GetItemAsync<ApplyLoanModel>(StorageName);
                    Loan = await psuLoan.GetLoanTypeAsync(ModelApplyLoan.LoanTypeID);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private decimal Calculate(LoanType? loan)
        {
            if (loan != null)
            {
                var SumInterest = ModelApplyLoan.LoanAmount * (loan.LoanInterest / 100);
                var TotalSum = ModelApplyLoan.LoanAmount + SumInterest;
                if (ModelApplyLoan.LoanNumInstallments != 0)
                {
                    TotalSum /= ModelApplyLoan.LoanNumInstallments;
                }

                TotalSum = (TotalSum == null ? 0 : TotalSum);
                return Math.Round((decimal)TotalSum, 2);
            }
            return 0;
        }

        private void Backcheckloan(ApplyLoanModel applyLoan)
        {
            if (!string.IsNullOrEmpty(applyLoan.DebtorId))
            {
                navigationManager.NavigateTo($"Admin/CheckLoanpage/{applyLoan.DebtorId}");
            }
            else
            {
                navigationManager.NavigateTo($"Admin/CheckLoanpage");
            }
        }
    }
}