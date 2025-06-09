using LoanApp.DatabaseModel.LoanEntities;

using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace LoanApp.Components.Document
{
    public partial class RequestAttrachment
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #region Parameter
        [Parameter] public LoanType Loan { get; set; } = new();
        [Parameter] public VLoanStaffDetail? StaffDetail { get; set; } = null;
        [Parameter] public ApplyLoanModel Other { get; set; } = new();
        [Parameter] public DocumentOptionModel Option { get; set; } = new();
        [Parameter] public VStaffAddress StaffAssress { get; set; } = new();
        [Parameter] public string TitleDocument { get; set; } = "สัญญา";

        #endregion

        #region Inject
        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan PsuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;

        #endregion

        private List<VLoanRequestContract> LoanAgreement { get; set; } = new();
        private string? CapmSelectNow { get; set; } = null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    CapmSelectNow = StateProvider?.CurrentUser.CapmSelectNow;

                    if (string.IsNullOrEmpty(CapmSelectNow))
                    {
                        CapmSelectNow = (await PsuLoan.GetUserDetailAsync(StateProvider?.CurrentUser.StaffId))?.CampId;
                    }
                    else if (CapmSelectNow == "00")
                    {
                        CapmSelectNow = null;
                    }

                    if (StaffDetail != null)
                    {
                        LoanAgreement = await CheckLoanAgreement(StaffDetail?.StaffId);
                    }

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
                }
            }
        }

        private async Task<List<VLoanRequestContract>> CheckLoanAgreement(string? staffId)
        {
            List<decimal> statusId = new() { 6m, 7m, 8m, 9m, 80m, 81m, 82m, 200m };

            if (string.IsNullOrEmpty(staffId))
            {
                return new List<VLoanRequestContract>();
            }

            try
            {
                return await _context.VLoanRequestContracts
                    .Where(c => statusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => c.DebtorStaffId == staffId)
                    .Where(c => (c.ContractDate == null) ||
                    ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
                return new List<VLoanRequestContract>();
            }
        }

        private string GetNumberToText(string text)
        {
            return userService.ArabicNumberToText(text);
        }

        private static string GetStaffType(string? staffType)
        {
            string mess = string.Empty;
            StaffTypeModel SType = new();

            if (string.IsNullOrEmpty(staffType))
            {
                return mess;
            }

            if (SType.GovernmentOfficer.Contains(staffType))
            {
                mess = "[/] ข้าราชการ";
            }
            else
            {
                mess = "[] ข้าราชการ";
            }

            if (SType.Employee.Contains(staffType))
            {
                mess = $"{mess} [/] ลูกจ้างประจำ";
            }
            else
            {
                mess = $"{mess} [] ลูกจ้างประจำ";
            }

            if (SType.UniversityStaff.Contains(staffType))
            {
                mess = $"{mess} [/] พนักงานมหาวิทยาลัย";
            }
            else
            {
                mess = $"{mess} [] พนักงานมหาวิทยาลัย";
            }

            if (SType.IncomeEmployee.Contains(staffType))
            {
                mess = $"{mess} [/] พนักงานเงินรายได้";
            }
            else
            {
                mess = $"{mess} [] พนักงานเงินรายได้";
            }

            return mess;
        }

        public async Task<string> GetBoByHtmlAsync()
        {
            var html = await JS.InvokeAsync<string>("getBobyHtml", "pdf-Attrachment");
            return html;
        }

        private string GetStaffSalaryId(string? staffId)
        {
            var StaffSalaryId = "ไม่มี";
            VSStaff? vSStaff = userService.GetVSStaff(staffId);

            if (!string.IsNullOrEmpty(vSStaff?.StaffSalaryId))
            {
                StaffSalaryId = vSStaff.StaffSalaryId;
            }
            return StaffSalaryId;
        }

        private string GetTitleDoc(string LoanParentName, byte? LoanTypeId)
        {
            var Name = $"ปิดและเปิด ป๊อปอัพ ใหม่";
            if (!string.IsNullOrEmpty(LoanParentName))
            {
                Name = $"{TitleDocument} {LoanParentName}";
            }
            else if (LoanTypeId != null)
            {
                var loan = userService.GetLoanType(LoanTypeId);
                Name = $"{TitleDocument} {loan?.LoanParentName}";
            }
            return Name;
        }
    }
}
