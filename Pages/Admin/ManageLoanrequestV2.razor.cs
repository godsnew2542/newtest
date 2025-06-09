using LoanApp.DatabaseModel.LoanEntities;

using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace LoanApp.Pages.Admin;

public partial class ManageLoanrequestV2
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #endregion

    #region Parameter
    [Parameter] public decimal StatusID { get; set; } = 0;
    [Parameter] public bool BACKPAGE { get; set; } = false;
    [Parameter] public decimal LOANTYPEID { get; set; } = 0;

    #endregion

    #region Inject
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private INotificationService notificationService { get; set; } = null!;

    #endregion

    /// <summary>
    /// .OrderBy(c => c.StatusAdminRequestOrder)
    /// .ThenBy(c => c.DefaultDateByStatus)
    /// </summary>
    private IOrderedQueryable<VLoanRequestContract> loanRequestContractsTemp { get; set; } = null!;
    private List<VLoanRequestContract> newLoanRequestContracts { get; set; } = new();
    private List<LoanType> LoanTypeList { get; set; } = new();
    private ApplyLoanModel ModelApplyLoan { get; set; } = new();
    private List<ContractStatus> Status { get; set; } = new();
    private List<SelectModel> Select { get; set; } = new();
    private SearchModel Search { get; set; } = new();
    private SelectModel SelectRemark { get; set; } = new();

    private DateTime dateTime { get; set; } = DateTime.Now;
    public decimal[] AllStatusId { get; } = new[] { 1m, 4m, 2m, };
    private string Message { get; set; } = "คำขอกู้นัดหมายทำสัญญาวันนี้";
    private string? Remark { get; set; } = null;
    public string? SearchView { get; set; } = null;
    private bool? LoanCheckbook { get; set; } = false;
    private bool changType { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Search.Title = string.Empty;
            ModelApplyLoan.LoanTypeID = 0;
            ModelApplyLoan.ContractStatusId = 0;

            if (!string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow))
            {
                string adminCapmId = StateProvider.CurrentUser.CapmSelectNow;

                loanRequestContractsTemp = _context.VLoanRequestContracts
                    .Where(c => AllStatusId.Contains(c.CurrentStatusId.Value))
                    .Where(c => c.DebtorCampusId == adminCapmId)
                    .OrderBy(c => c.StatusAdminRequestOrder)
                    .ThenBy(c => c.DefaultDateByStatus);
            }

            if (BACKPAGE)
            {
                #region back จากการดูข้อมูล
                if (StatusID != 0)
                {
                    ModelApplyLoan.ContractStatusId = StatusID;
                }
                if (LOANTYPEID != 0)
                {
                    ModelApplyLoan.LoanTypeID = (byte?)LOANTYPEID;
                }
                await DataTableV2(SearchView, ModelApplyLoan);
                #endregion
            }
            else if (StatusID != 0)
            {
                ModelApplyLoan.ContractStatusId = StatusID;
                await DataTableV2(SearchView, ModelApplyLoan);
            }
            else
            {
                await DataTableV2(SearchView, ModelApplyLoan);
            }

            Select = SetDataSelect();

            LoanTypeList = await psuLoan.GetAllLoanType();
            LoanTypeList.Insert(0, new LoanType()
            {
                LoanTypeId = 0,
                LoanTypeName = "ทุกประเภท"
            });

            Status = await psuLoan.GetAllContractStatus(AllStatusId.ToList());
            Status.Insert(0, new ContractStatus()
            {
                ContractStatusId = 0,
                ContractStatusName = "ทุกสถานะ"
            });
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
            try
            {
                var checkData = await sessionStorage.GetItemAsStringAsync("BackToManageLoanRequest");
                if (!string.IsNullOrEmpty(checkData))
                {
                    await sessionStorage.RemoveItemAsync("BackToManageLoanRequest");
                }

                if (LOANTYPEID != 0)
                {
                    int? index = LoanTypeList.FindIndex(a => a.LoanTypeId == LOANTYPEID);
                    if (index != null)
                    {
                        await JS.InvokeVoidAsync("SelectedTagID", "selectLoanTypeId", index);
                    }
                }

                if (StatusID != 0)
                {
                    int? index = Status.FindIndex(a => a.ContractStatusId == StatusID);
                    if (index != null)
                    {
                        await JS.InvokeVoidAsync("SelectedTagID", "selectStatusId", index);

                    }
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }
    }

    private string ChangeDate(DateTime? date)
    {
        string showDate = string.Empty;
        string fomathDate = "dd-MM-yyyy เวลา HH:mm น.";
        if (date != null)
        {
            showDate = dateService.ChangeDate(date, fomathDate, Utility.DateLanguage_TH);
        }
        return showDate;
    }

    private void SelectType(SelectModel selectmodel, decimal? LoanRequestId)
    {
        SelectRemark = new();
        SelectRemark = selectmodel;
        SelectRemark.LoanRequestId = LoanRequestId;
        Remark = selectmodel.Name;
        ToggleButton(true);
    }

    private List<SelectModel> SetDataSelect()
    {
        List<SelectModel> SelectModel = new()
        {
            new SelectModel { Name = "ไม่ทำสัญญาภายในวันที่กำหนด", ID = 1 },
            new SelectModel { Name = "ไม่นัดหมายภายในวันที่กำหนด", ID = 2 },
            new SelectModel { Name = "ไม่ประสงค์ทำสัญญา", ID = 3 },
            new SelectModel { Name = "เจ้าตัวประสงค์ยกเลิกคำขอกู้", ID = 4 },
            new SelectModel { Name = "อื่นๆ โปรดระบุ", ID = 99 }
        };

        return SelectModel;
    }

    private void ToggleButton(bool close)
    {
        if (close)
        {
            changType = false;
        }
        else
        {
            changType = !changType;
        }
    }

    private async Task OnSearch(string? val, ApplyLoanModel Loan)
    {
        try
        {
            if (string.IsNullOrEmpty(val))
            {
                await DataTableV2(null, ModelApplyLoan);
                return;
            }
            else if ((val.Trim()).Length < Utility.SearchMinlength)
            {
                await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
                return;
            }

            await DataTableV2(val, Loan);
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task DataTableV2(string? searchName, ApplyLoanModel Loan)
    {
        newLoanRequestContracts = new();

        await Task.Delay(1);
        StateHasChanged();

        try
        {
            newLoanRequestContracts = await loanRequestContractsTemp
                .Where(c => string.IsNullOrEmpty(searchName) ||
                (c.DebtorNameTh.Contains(searchName) ||
                c.DebtorSnameTh.Contains(searchName) ||
                (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                (c.DebtorNameEng.ToLower()).Contains(searchName.ToLower()) ||
                (c.DebtorSnameEng.ToLower()).Contains(searchName.ToLower()) ||
                (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower())))
                .Where(c => (Loan.LoanTypeID == null || Loan.LoanTypeID == 0) || c.LoanTypeId == Loan.LoanTypeID)
                .Where(c => Loan.ContractStatusId == 0 || c.CurrentStatusId == Loan.ContractStatusId)
                .Where(c => LoanCheckbook == true ?
                (c.DefaultDateByStatus!.Value.Date == dateTime.Date &&
                c.DefaultDateByStatus.Value.Month == dateTime.Month &&
                c.DefaultDateByStatus.Value.Year == dateTime.Year) :
                LoanCheckbook != null)
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    protected async Task SelectLoanTypeIDAsync(ChangeEventArgs e)
    {
        var TypeID = Convert.ToInt32(e.Value.ToString());
        ModelApplyLoan.LoanTypeID = (byte?)TypeID;
        await DataTableV2(Search.Title, ModelApplyLoan);
    }

    protected async Task SelectContractStatusAsync(ChangeEventArgs e)
    {
        ModelApplyLoan.ContractStatusId = Convert.ToInt32(e.Value.ToString());
        await DataTableV2(Search.Title, ModelApplyLoan);
    }

    private async Task CheckrequestAsync(VLoanRequestContract agreement)
    {
        await SetStorageAsync(ModelApplyLoan);
        navigationManager.NavigateTo($"/Admin/RequestDetail/{agreement.LoanRequestId}");
    }

    private async Task OpenPdgePDFAsync(VLoanRequestContract agreement)
    {
        await SetStorageAsync(ModelApplyLoan);
        navigationManager.NavigateTo($"/Admin/Pdf/{agreement.LoanRequestId}");
    }

    private async Task SetStorageAsync(ApplyLoanModel val, string key = "BackToManageLoanRequest")
    {
        await sessionStorage.SetItemAsync(key, val);
    }

    /// <summary>
    /// true = "แสดงเฉพาะที่ยังไม่สิ้นสุดสัญญา" || false = "แสดงสัญญาทั้งหมด"
    /// </summary>
    /// <returns></returns>
    private async Task CheckboxSwitch()
    {
        var interopResult = !LoanCheckbook;

        ModelApplyLoan.ContractStatusId = interopResult == true ? 4 : 0;
        LoanCheckbook = interopResult;

        await DataTableV2(SearchView, ModelApplyLoan);
    }

    private async Task ChoosedateAsync(VLoanRequestContract req)
    {
        var Role = "Admin";
        var Detail = await psuLoan.GetLoanStaffDetailByStaffId(req.DebtorStaffId);

        if (Detail != null)
        {
            await SetStorageAsync(ModelApplyLoan);
            navigationManager.NavigateTo($"/{Role}/ChooseDate/{req.DebtorStaffId}/{req.LoanRequestId}");
        }
        else
        {
            string alert = $"กรุณาระบุหมายเลขบัญชีธนาคารที่ต้องการรับเงินกู้ยืม ที่เมนู 'ข้อมูลส่วนตัว'";
            await JS.InvokeVoidAsync("displayTickerAlert", alert);
        }
    }

    private void DefaultRemark(List<SelectModel> searches, decimal? RequestId, int? selectId = null)
    {
        Remark = null;
        SelectRemark = new();
        SelectModel? _searches = null;
        SelectRemark.LoanRequestId = RequestId;

        if (selectId != null)
        {
            _searches = searches.Find(x => x.ID == selectId);
        }

        if (_searches != null)
        {
            SelectRemark.ID = _searches.ID;
            SelectRemark.Name = _searches.Name;
            Remark = _searches.Name;
        }
    }

    private async Task SaveToDbAsync(SelectModel select, string? remark)
    {
        decimal? RequesStatusId = null;
        decimal? ContractStatusId = null;
        decimal DisabledLoanStatusId = 3m;

        try
        {
            string staffId = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
            ContractMain? main = null;
            LoanRequest? request = await psuLoan.GetLoanRequestByLoanRequestId(select.LoanRequestId);

            if (request != null)
            {
                RequesStatusId = request.LoanStatusId;
                request.LoanStatusId = DisabledLoanStatusId;
                request.LoanRemark = $"{remark}";

                await psuLoan.UpdateLoanRequest(request);
                main = await psuLoan.GeContractMainByLoanRequestId(select.LoanRequestId);

                if (main != null)
                {
                    ContractStatusId = main.ContractStatusId;
                    main.ContractStatusId = DisabledLoanStatusId;
                    main.ContractEndDate = DateTime.Now;

                    await psuLoan.UpdateContractMain(main);
                }
            }

            await SaveToHistoryAsync(request, RequesStatusId, main, ContractStatusId, staffId);
            navigationManager.NavigateTo($"/Admin/ManageLoanRequest/{false}/{ModelApplyLoan.LoanTypeID}/{ModelApplyLoan.ContractStatusId}", true);
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task SaveToHistoryAsync(LoanRequest? Request = null, decimal? RequestStatusId = null, ContractMain? Contract = null, decimal? ContractStatusId = null, string? ModifyBy = null)
    {
        if (Contract != null)
        {
            await LogService.GetHisContractMainByContractIDAsync(Contract.ContractId,
                ContractStatusId.Value,
                ModifyBy);
        }

        if (Request != null)
        {
            await LogService.GetHisLoanRequestByRequestIDAsync(Request.LoanRequestId,
               RequestStatusId.Value,
               ModifyBy);
        }
    }

}
