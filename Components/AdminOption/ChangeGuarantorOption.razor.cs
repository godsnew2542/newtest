using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Services.IServices.LoanDb;
using LoanApp.Shared;
using LoanApp.Services.IServices;
using NPOI.OpenXmlFormats.Dml;

namespace LoanApp.Components.AdminOption;

public partial class ChangeGuarantorOption
{
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #region Parameter
    [Parameter] public VLoanRequestContract ReqCon { get; set; } = new();
    [Parameter] public FormAdminOptionModel FormOption { get; set; } = new();
    [Parameter] public EventCallback<FormAdminOptionModel> OnChangeGuarantorChange { get; set; }

    #endregion

    [Inject] private IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private INotificationService notificationService { get; set; } = null!;

    private ApplyLoanModel ModelApplyLoan { get; set; } = new();
    private List<VLoanStaffDetail> GuarantorList { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        FormOption.ChangeGuarantor.LoanInterest = ReqCon.LoanRequestLoanInterest;
        FormOption.ChangeGuarantor.LoanNumInstallments = ReqCon.LoanRequestNumInstallments;
        FormOption.ChangeGuarantor.LoanInstallment = (ReqCon.ContractLoanInstallment == null ?
            null : ReqCon.ContractLoanInstallment);
        FormOption.ChangeGuarantor.BalanceAmount = TransactionService.GetBalanceTotal(FormOption.ContractId, FormOption.LoanAmount);
        FormOption.ChangeGuarantor.GuarantorStaffIdNow = !string.IsNullOrEmpty(ReqCon.ContractGuarantorStaffId) ? ReqCon.ContractGuarantorStaffId : string.Empty;
        FormOption.ChangeGuarantor.NewGuarantorStaffId = null;

        await OnChangeGuarantorChange.InvokeAsync(FormOption);
    }

    private async Task FindGuarantorAsync()
    {
        GuarantorList = new();
        FormOption.ChangeGuarantor.NewGuarantorStaffId = null;

        if (!string.IsNullOrEmpty(ModelApplyLoan.Guarantor) &&
            ModelApplyLoan.Guarantor.Length >= Utility.SearchMinlength)
        {
            string? adminCapmId = StateProvider?.CurrentUser.CapmSelectNow;

            GuarantorList = await psuLoan.FilterSearchValueFormVLoanStaffDetail(searchText: ModelApplyLoan.Guarantor, campId: null);

            if (!string.IsNullOrEmpty(adminCapmId) && adminCapmId != "00")
            {
                GuarantorList = GuarantorList
                    .Where(c => c.CampusId == adminCapmId)
                    .ToList();
            }

            //GuarantorList = await _context.VLoanStaffDetails
            //      .Where(c => c.StaffDepart == "3" &&
            //      (c.StaffNameThai!.Contains(ModelApplyLoan.Guarantor) ||
            //      c.StaffSnameThai!.Contains(ModelApplyLoan.Guarantor) ||
            //      (c.StaffNameEng!).ToLower().Contains(ModelApplyLoan.Guarantor.ToLower()) ||
            //      (c.StaffSnameEng!).ToLower().Contains(ModelApplyLoan.Guarantor.ToLower()) ||
            //      (c.StaffNameThai + " " + c.StaffSnameThai).Contains(ModelApplyLoan.Guarantor) ||
            //      (c.StaffNameEng + " " + c.StaffSnameEng).ToLower().Contains(ModelApplyLoan.Guarantor.ToLower())
            //      ))
            //      .ToListAsync()
        }

        if (ModelApplyLoan.Guarantor.Length < Utility.SearchMinlength)
        {
            string alert = $"ในการค้นหาต้องกรอกอย่างน้อย {Utility.SearchMinlength} ตัวอักษรขึ้นไป";

            _ = Task.Run(() => notificationService.Warning(alert));
            //await JS.InvokeVoidAsync("displayTickerAlert", alert)
        }
        else if (!GuarantorList.Any())
        {
            ModelApplyLoan.Guarantor = string.Empty;

            string alert = "ไม่พบรายชื่อที่คุณค้นหา";
            _ = Task.Run(() => notificationService.Warning(alert));
            //await JS.InvokeVoidAsync("displayTickerAlert", alert)
        }
        await OnChangeGuarantorChange.InvokeAsync(FormOption);
    }

    private async Task ChangeValGuarantorAsync(VLoanStaffDetail people)
    {
        if (people != null)
        {
            GuarantorList = new();
            var fullName = userService.GetFullName(people.StaffId);
            ModelApplyLoan.Guarantor = fullName;
            FormOption.ChangeGuarantor.NewGuarantorStaffId = people.StaffId;
            await OnChangeGuarantorChange.InvokeAsync(FormOption);
        }
    }
}
