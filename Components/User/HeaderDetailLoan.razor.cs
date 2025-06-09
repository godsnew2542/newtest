using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.User;

public partial class HeaderDetailLoan
{
    /// <summary>
    /// defult ไม่สามารถแก้ได้ false
    /// </summary>
    [Parameter] public bool IsEditContractNo { get; set; } = false;
    [Parameter] public VLoanRequestContract? ReqCon { get; set; } = null;

    [Inject] LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

    private bool EditContractNoVisible { get; set; } = false;

    private string? GetHeaderFullName(VLoanRequestContract data)
    {
        return $"คุณ{data.DebtorNameTh} {data.DebtorSnameTh} (StaffId : {(!string.IsNullOrEmpty(ReqCon?.DebtorStaffId) ? ReqCon?.DebtorStaffId : "ไม่พบ StaffId")})";
    }

    private string? GetHeaderLoanTypeName(VLoanRequestContract data)
    {
        try
        {
            var loneType = userService.GetLoanType(data.LoanTypeId);
            var typeName = userService.GetLoanName(loneType);

            return $"ประเภท {typeName}";
        }
        catch (Exception ex)
        {
            _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });

            return null;
        }
    }

    private void CallbackData(bool e)
    {
        EditContractNoVisible = e;
        var url = navigationManager.Uri.Split(navigationManager.BaseUri);
        if (url.Length == 2)
        {
            navigationManager.NavigateTo(url[1], true);
        }
    }
}
