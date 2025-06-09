using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using LoanApp.Shared;
using Microsoft.Extensions.Options;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Settings;
using LoanApp.Model.Helper;

namespace LoanApp.Pages.User;

public partial class CheckLoan
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #endregion

    #region Inject
    [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;

    #endregion

    private List<LoanType> ListLoan { get; set; } = new();
    private List<List<VLoanRequestContract>> ListOverview { get; set; } = new();
    private ContractAttachment? AttachmentPdf { get; set; } = new();
    private LoanType SelectLoan { get; set; } = new();
    private VLoanStaffDetail? vLoanStaffDetail { get; set; } = null;

    private string StaffId { get; set; } = string.Empty;
    private List<byte> LoanTypeId { get; set; } = new();
    private decimal[] AllowedStatus { get; } = new[] { 3m, 98m, 99m, 100m };
    private decimal[] OverviewStatusId { get; } = new[] { 6m, 7m, 8m, 80, 81, 82m, 200m };

    protected async override Task OnInitializedAsync()
    {
        StaffId = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

        try
        {
            if (!string.IsNullOrEmpty(StaffId))
            {
                ListLoan = await psuLoan.GetAllLoanType(1);
                LoanTypeId = userService.GetTypeIdByStaff(StaffId, AllowedStatus);

                if (LoanTypeId.Any())
                {
                    ListLoan = DistinctLoanType(LoanTypeId, ListLoan);
                }

                List<VLoanRequestContract> overviewAll = await psuLoan.GetListAgreementDataFormVLoanRequestContract(staffId: StaffId, status: OverviewStatusId.ToList(), dateLimt: 0);

                if (overviewAll.Any())
                {
                    SubOverview(overviewAll);
                }
            }

            vLoanStaffDetail = await psuLoan.GetUserDetailAsync(StaffId);
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
            await JS.InvokeVoidAsync("Cardimgheader", ListLoan.Count);
        }
    }

    private string GetUrl(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            var path = $"{AppSettings.Value.RequestFilePath}\\{url}";
            return path;
        }
        return string.Empty;
    }

    private void SubOverview(List<VLoanRequestContract> OverviewAll)
    {
        int showCard = 3;
        List<VLoanRequestContract> SubView = new();

        for (int i = 0; i < OverviewAll.Count; i++)
        {
            var Overview = OverviewAll[i];
            SubView.Add(Overview);

            if (SubView.Count == showCard || i == (OverviewAll.Count - 1))
            {
                ListOverview.Add(SubView);
                SubView = new();
            }
        }
    }

    private List<LoanType> DistinctLoanType(List<byte> DistinctLoan, List<LoanType> Lloan)
    {
        var loanData = Lloan;
        for (int x = 0; x < DistinctLoan.Count; x++)
        {
            var typeId = DistinctLoan[x];
            for (int i = 0; i < loanData.Count; i++)
            {
                var eleLoanData = loanData[i];

                if (typeId == eleLoanData.LoanTypeId && !userService.CheckReconcile(eleLoanData))
                {
                    LoanType? myTodo = loanData.Find(x => x.LoanTypeId == typeId);

                    if (myTodo != null)
                    {
                        loanData.Remove(myTodo);
                    }
                }
            }
        }
        return loanData;
    }

    private void OpenPDF(LoanType data)
    {
        SelectLoan = new();
        SelectLoan = data;

        var StringTypeID = Convert.ToString(data.LoanTypeId);
        AttachmentPdf = _context.ContractAttachments
            .Where(c => c.AttachmentTypeName == StringTypeID)
            .FirstOrDefault();
    }

    private static string AddRemark(LoanType loan)
    {
        string Mess = string.Empty;
        string Reconcile = "สามารถกู้ทบยอดได้";

        if (loan.IsReconcile == 1)
        {
            Mess = $"หมายเหตุ : {Mess} {CheckMessIsNull(Mess)} {Reconcile}";
        }
        return Mess;
    }

    private static string CheckMessIsNull(string? text)
    {
        string data = string.Empty;
        if (!string.IsNullOrEmpty(text))
        {
            data = $",";
        }
        return data;
    }

    private void ApplyLoan(LoanType landData)
    {
        if (Utility.CheckStaffTypeByDebtor(vLoanStaffDetail?.StaffType))
        {
            navigationManager.NavigateTo($"/Applyloan/{landData.LoanTypeId}");
        }
        else
        {
            _ = Task.Run(() => notificationService.Warning(mess: "ไม่สามารถยื่นกู้ได้ รองรับคุณสมบัติของผู้กู้ที่เป็นข้าราชการ ลูกจ้างประจำ พนักงานมหาวิทยาลัย และพนักงานเงินรายได้เท่านั้น", title: "แจ้งเตือน", autoClose: false));
        }
    }
}
