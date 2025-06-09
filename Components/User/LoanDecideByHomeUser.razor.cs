using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Components.User
{
    public partial class LoanDecideByHomeUser
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public decimal[] AllowedStatus { get; set; } = Array.Empty<decimal>();

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<LoanRequest> RequestList { get; set; } = new();
        private DeleteApplyLoanModel SelectAppiyLoanDelete { get; set; } = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                decimal[] StatusId = new decimal[] { 0m };

                RequestList = await _context.LoanRequests
                    .Where(c => StatusId.Contains(c.LoanStatusId!.Value))
                    .Where(c => c.DebtorStaffId == StaffID)
                    .ToListAsync();

                StateHasChanged();
            }
        }

        private ActiveModel GetLoanActive(int? activeData, LoanType? loan)
        {
            ActiveModel active = new();

            if (activeData == null || loan == null)
            {
                return active;
            }

            active.ActiveId = activeData.Value;

            if (activeData == 1)
            {
                active.Message = "สามารถเข้าไปดำเนินการต่อได้";
                active.IsPass = true;

                bool IsReconcile = userService.CheckReconcile(loan);
                if (!IsReconcile)
                {
                    var CheckCredit = _context.VLoanRequestContracts
                        .Where(c => c.DebtorStaffId == StaffID)
                        .Where(c => c.LoanTypeId == loan.LoanTypeId)
                        .Where(c => !AllowedStatus.Contains(c.CurrentStatusId!.Value))
                        .Count();

                    if (CheckCredit > 0)
                    {
                        active.Message = "กู้ประเภทนี้อยู่ในขณะนี้";
                        active.IsCredit = true;
                        active.IsPass = false;
                    }
                }
            }
            else
            {
                active.Message = "หมดเขตการยื่นกู้ประเภทนี้";
            }
            return active;
        }

        private void EditAppiyLoan(LoanRequest req)
        {
            navigationManager.NavigateTo($"/Applyloan/Edit/{req.LoanRequestId}");
        }

        private void SelectLoan(LoanRequest req, LoanType? type)
        {
            if (type == null)
            {
                return;
            }

            SelectAppiyLoanDelete.LoanParentName = type.LoanParentName;
            SelectAppiyLoanDelete.LoanTypeName = type.LoanTypeName;
            SelectAppiyLoanDelete.LoanRequestId = req.LoanRequestId;
            SelectAppiyLoanDelete.LoanAmount = req.LoanAmount != null ? req.LoanAmount.Value : 0;
            SelectAppiyLoanDelete.IsLoaing = false;
        }

        private async Task DeleteAppiyLoanAsync(decimal id)
        {
            var requests = await psuLoan.GetLoanRequestByLoanRequestId(id);

            if (requests != null)
            {
                _context.LoanRequests.Remove(requests);
                await _context.SaveChangesAsync();

                string ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
                await LogService.GetHisLoanRequestByRequestIDAsync(requests.LoanRequestId, 0m, ModifyBy, "DELETE");

                navigationManager.NavigateTo("/HomeUser", true);
            }
        }
    }
}
