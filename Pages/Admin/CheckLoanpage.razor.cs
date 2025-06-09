using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.Admin;

public partial class CheckLoanpage
{
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #region Parameter
    [Parameter] public string StaffID { get; set; } = string.Empty;
    [Parameter] public string Role { get; set; } = string.Empty;

    #endregion

    #region Inject
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private INotificationService notificationService { get; set; } = null!;

    #endregion

    private VLoanStaffDetail? StaffDetail { get; set; } = new();
    private List<VLoanStaffDetail> ListStaffDetail { get; set; } = new();
    private VStaffFamily StaffFamily { get; set; } = new();
    private List<LoanType> LoanData { get; set; } = new();
    private ApplyLoanModel Search { get; set; } = new();
    private MonthModel model_month { get; set; } = new();
    private List<VLoanRequestContract> Agreement { get; set; } = new();

    #region สัญญาเก่า กรณีที่เคยเปลี่ยน staffId
    private List<VLoanStaffDetail> ListStaffIdOld { get; set; } = new();
    private List<VLoanRequestContract> LoanAgreementDebtor { get; set; } = new();
    private List<VLoanRequestContract> LoanAgreementGuaran { get; set; } = new();

    #endregion

    public string? SearchView { get; set; } = string.Empty;
    private string StorageName { get; } = "CheckLoanpage";
    private bool CheckSearchVeiw { get; set; } = false;
    private decimal[] AllStatusId { get; } = new[] { 0m, 1m, 3m, 98m, 99m, 100m, 200m };
    private decimal[] AgreementStatusActive { get; } = new[] { 6m, 7m, 8m, 9m, 80m, 81m, 82m, 200m };
    private string HeadRole { get; set; } = string.Empty;
    private bool LoadingData { get; set; } = false;
    private int CountAgreementGuarantor { get; set; } = 0;
    private bool isStaffDepart { get; set; } = false;

    protected async override Task OnInitializedAsync()
    {
        Search.SalaryNetAmount = 0;

        try
        {
            if (!string.IsNullOrEmpty(StaffID))
            {
                var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
                if (checkData != null)
                {
                    Search = await sessionStorage.GetItemAsync<ApplyLoanModel>(StorageName);
                    SearchView = Search.Debtor;
                    VLoanStaffDetail? people = await psuLoan.GetUserDetailAsync(Search.DebtorId);

                    if (people != null)
                    {
                        await ChangeValStaff(people, true);
                    }
                }

                VStaffFamily? family = await psuLoan.GetUserFamilyAsync(StaffID);
                if (family != null)
                {
                    StaffFamily = family;
                }
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task SearchDataAsync(string? text)
    {
        try
        {
            string? adminCampId = StateProvider?.CurrentUser.CapmSelectNow;

            StaffDetail!.StaffId = string.Empty;
            Search.Debtor = (!string.IsNullOrEmpty(text) ?
                text :
                string.Empty);
            ListStaffDetail = new();

            if (!string.IsNullOrEmpty(text))
            {
                Search.OptionSearchName = true;

                if ((text.Trim()).Length < Utility.SearchMinlength)
                {
                    await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
                }
                else
                {
                    text = text.Trim();

                    ListStaffDetail = await psuLoan.FilterSearchValueFormVLoanStaffDetail(text, adminCampId, staffDepart: !isStaffDepart ? "3" : null);
                }
            }

            CheckSearchVeiw = ListStaffDetail.Any() ? false : true;

            if (ListStaffDetail.Count == 1)
            {
                await ChangeValStaff(ListStaffDetail[0], true);
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task ChangeValStaff(VLoanStaffDetail people, bool option)
    {
        ListStaffDetail = new();
        StaffDetail = new();
        LoanAgreementDebtor = new();
        LoanAgreementGuaran = new();
        ListStaffIdOld = new();

        var fullName = userService.GetFullName(people.StaffId);
        var StaffId = people.StaffId;
        var facType = people.FacNameThai;

        if (option)
        {
            Search.Debtor = fullName;
            SearchView = fullName + "( " + facType + " )";
        }
        else
        {
            Search.Debtor = StaffId;
            SearchView = StaffId;
        }
        Search.DebtorId = StaffId;
        StaffDetail = await psuLoan.GetUserDetailAsync(StaffId);

        if ((!string.IsNullOrEmpty(StaffDetail?.StaffId)))
        {
            var family = await psuLoan.GetUserFamilyAsync(StaffDetail.StaffId);
            CountAgreementGuarantor = await AgreementGuarantorAsync(StaffId);

            if (family != null)
            {
                StaffFamily = family;
            }
        }

        GetLoanTypes();

        //listStaffIdOld
        if (!string.IsNullOrEmpty(people.StaffPersId))
        {
            ListStaffIdOld = await psuLoan.GetListStaffIdByStaffPersId(staffPersId: people.StaffPersId, staffId: StaffId, isRemoveStaffIdNow: true);

            if (ListStaffIdOld.Any())
            {
                var resultAgreement = await psuLoan.GetAllLoanByStaffId(staffIds: ListStaffIdOld.Select(x => x.StaffId).ToList(), statusIdOther: new List<decimal>() { 98, 99, 3 });
                //var resultAgreement = await psuLoan.GetAllLoanByStaffId(staffIds: ListStaffIdOld.Select(x => x.StaffId).ToList())

                LoanAgreementDebtor = resultAgreement.Item1;
                LoanAgreementGuaran = resultAgreement.Item2;
            }
        }

        StateHasChanged();
    }

    private string ChangeDate(string? StringDate, string[] selectMonth)
    {
        var ShowDate = string.Empty;
        DateModel Date = Utility.ChangeDateMonth(StringDate, selectMonth);

        if (!string.IsNullOrEmpty(Date.Day))
        {
            ShowDate = $"{Date.Day} {Date.Month} {Date.Year}";
        }
        return ShowDate;
    }

    private List<LoanType> GetLoanPrivacyRights(string staffId)
    {
        //List<LoanType> listLoan = _context.LoanTypes.Where(c => c.Active == 1).ToList()
        List<LoanType> listLoan = psuLoan.GetAllLoanTypeNotAsync(1);

        List<byte> LoanTypeId = userService.GetTypeIdByStaff(staffId, AllStatusId);

        if (LoanTypeId.Any())
        {
            listLoan = DistinctLoanType(LoanTypeId, listLoan);
        }

        return listLoan;
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
                    var myTodo = loanData.Find(x => x.LoanTypeId == typeId);

                    if (myTodo != null)
                    {
                        loanData.Remove(myTodo);
                    }
                }
            }
        }
        return loanData;
    }

    private void GetLoanTypes()
    {
        LoanData = new();
        //LoanData = _context.LoanTypes.Where(c => c.Active == 1).ToList()
        LoanData = psuLoan.GetAllLoanTypeNotAsync(1);
    }

    private async Task CreateLoanAsync(LoanType loan)
    {
        bool pass = await CheckDataLoanOtherAsync(Search, loan);
        if (pass)
        {
            Search.LoanTypeID = loan.LoanTypeId;
            await sessionStorage.SetItemAsync(StorageName, Search);
            navigationManager.NavigateTo($"./Admin/CheckLoanAdmin");
        }
    }

    private async Task<bool> CheckDataLoanOtherAsync(ApplyLoanModel data, LoanType loan)
    {
        bool pass = false;

        if (data.SalaryNetAmount == null || data.SalaryNetAmount < 1)
        {
            string alert = $"กรุณาระบุเงินเดือนคงเหลือสุทธิ";
            //await notificationService.AlertMess(alert)

            _ = Task.Run(() => { notificationService.Warning(alert); });
            return pass;
        }

        //if (data.LoanAmount == null || data.LoanAmount < 1)
        if (data.LoanAmount <= 0)
        {
            string alert = $"กรุณาระบุจำนวนเงินที่ต้องการกู้ให้ถูกต้อง";
            //await notificationService.AlertMess(alert)

            _ = Task.Run(() => { notificationService.Warning(alert); });

            return pass;
        }
        else if (loan.LoanMaxAmount != 0)
        {
            if (data.LoanAmount > loan.LoanMaxAmount)
            {
                string alert = $"จำนวนเงินที่ต้องการกู้มากกว่าวงเงินกู้สูงสุดที่กู้ได้";
                //await notificationService.AlertMess(alert)

                _ = Task.Run(() => { notificationService.Warning(alert); });

                return pass;
            }
        }

        //if (data.LoanNumInstallments == null || data.LoanNumInstallments < 1)
        if (data.LoanNumInstallments <= 0)
        {
            string alert = $"กรุณาระบุจำนวนงวดที่ต้องการผ่อนให้ถูกต้อง";
            //await notificationService.AlertMess(alert)

            _ = Task.Run(() => { notificationService.Warning(alert); });

            return pass;
        }
        else if (data.LoanNumInstallments > loan.LoanNumInstallments)
        {
            string alert = $"จำนวนงวดที่ต้องการผ่อนมากกว่าจำนวนงวดสูงสุดที่กู้ได้";
            //await notificationService.AlertMess(alert)

            _ = Task.Run(() => { notificationService.Warning(alert); });

            return pass;
        }

        pass = true;
        return pass;
    }

    private decimal SumAmount(string? staffId)
    {
        decimal Amount = 0;

        if (!string.IsNullOrEmpty(staffId))
        {
            //List<VLoanRequestContract> reqCon = _context.VLoanRequestContracts
            //    .Where(c => c.DebtorStaffId == staffId)
            //    .Where(c => AgreementStatusActive.Contains(c.CurrentStatusId!.Value))
            //    .Where(c => c.ContractLoanAmount != null)
            //    .ToList()

            List<VLoanRequestContract> reqCon = psuLoan.GetListDebtorStaffIdFormVLoanRequestContractNotAsync(staffId, AgreementStatusActive.ToList());

            if (reqCon.Any())
            {
                foreach (var LoanAmount in reqCon)
                {
                    if (LoanAmount.ContractLoanAmount != null)
                    {
                        Amount += LoanAmount.ContractLoanAmount.Value;
                    }
                }
            }
        }
        return Amount;
    }

    private async Task<decimal> BalanceTotalAsync(string? staffId)
    {
        decimal Balance = 0;
        if (!string.IsNullOrEmpty(staffId))
        {
            //List<VLoanRequestContract> reqCon = await _context.VLoanRequestContracts
            //    .Where(c => c.DebtorStaffId == staffId)
            //    .Where(c => AgreementStatusActive.Contains(c.CurrentStatusId!.Value))
            //    .Where(c => c.ContractLoanAmount != null)
            //    .ToListAsync()

            List<VLoanRequestContract> reqCon = await psuLoan.GetListDebtorStaffIdFormVLoanRequestContract(staffId, AgreementStatusActive.ToList());

            if (reqCon.Any())
            {
                for (int i = 0; i < reqCon.Count; i++)
                {
                    var data = reqCon[i];

                    //PaymentTransaction? Transactions = _context.PaymentTransactions
                    //    .Where(c => c.ContractId == data.ContractId)
                    //   .Select(c => new PaymentTransaction
                    //   {
                    //       ContractId = c.ContractId,
                    //       InstallmentNo = c.InstallmentNo,
                    //       BalanceAmount = c.BalanceAmount,
                    //   })
                    //   .Distinct()
                    //   .OrderByDescending(c => c.InstallmentNo)
                    //   .FirstOrDefault()

                    PaymentTransaction? Transactions = psuLoan.GetPaymentTransactionByContractIdSelectDataNoneAsync(data.ContractId);

                    if (Transactions?.BalanceAmount != null)
                    {
                        Balance += Transactions.BalanceAmount.Value;
                    }
                    else
                    {
                        if (data.ContractLoanAmount != null)
                        {
                            Balance += data.ContractLoanAmount.Value;
                        }
                    }
                }
            }
        }
        return Balance;
    }

    private int AgreementDebtor(string? staffId)
    {
        int count = 0;
        if (!string.IsNullOrEmpty(staffId))
        {
            count = _context.VLoanRequestContracts
               .Where(c => c.DebtorStaffId == staffId)
               .Where(c => AgreementStatusActive.Contains(c.CurrentStatusId!.Value))
               .Where(c => c.ContractLoanAmount != null)
               .Where(c => (c.ContractDate == null) ||
            ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
               .Count();
        }
        return count;
    }

    private async Task<int> AgreementGuarantorAsync(string staffId)
    {
        int count = 0;
        if (staffId != null)
        {
            List<VLoanRequestContract> listAgreement = await _context.VLoanRequestContracts
                .Where(c => c.LoanRequestGuaranStaffId == staffId || c.ContractGuarantorStaffId == staffId)
                .Where(c => AgreementStatusActive.Contains(c.CurrentStatusId!.Value))
                .Where(c => c.ContractLoanAmount != null)
                .Where(c => (c.ContractDate == null) ||
                ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                .ToListAsync();

            if (listAgreement.Any())
            {
                /* Check เปลี่ยนผู้ค้ำ หลังทำสัญญา */
                var data = Utility.CheckChangeGuarantor(staffId, listAgreement);
                return data.Count;
            }
        }
        return count;
    }

    private List<VLoanRequestContract> CheckChangeGuarantor(string staffID, List<VLoanRequestContract> Agreement)
    {
        var listAgreement = Agreement;
        for (int i = 0; i < listAgreement.Count; i++)
        {
            var item = listAgreement[i];
            var id = item.LoanRequestId;
            /* Check ผู้ค้ำว่าไปเซ็นสัญญาหรือยัง */
            if (item.ContractGuarantorStaffId != null)
            {
                /* Check ผู้ค้ำก่อน/หลัง เซ็นสัญญา ว่าเป็นคนเดี่ยวกันไหม */
                if (item.LoanRequestGuaranStaffId != item.ContractGuarantorStaffId)
                {
                    /* กรณีไม่ใช่คนเดี่ยวกันให้ Check หลังเซ็นสัญญาว่าเป็น คนที่เข้าใช้งานอยู่ไหม */
                    if (item.ContractGuarantorStaffId != staffID)
                    {
                        var myTodo = Agreement.Find(x => x.LoanRequestId == id);
                        if (myTodo != null)
                        {
                            Agreement.Remove(myTodo);
                        }
                    }
                }
            }
        }
        return Agreement;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="page">คำขอกู้ = 1 || สัญญากู้ยืมเงิน = 2 || สัญญาค้ำประกัน = 3</param>
    /// <param name="StaffId"></param>
    /// <returns></returns>
    private async Task TopageAgreementAsync(int page, string? StaffId)
    {
        await sessionStorage.SetItemAsync(StorageName, Search);
        if (page == 1)
        {
            if (Role == "Manager")
            {
                navigationManager.NavigateTo($"/Manager/CheckRequestAgreement/{StaffId}");
            }
            else
            {
                navigationManager.NavigateTo($"/Admin/CheckRequestAgreement/{StaffId}");
            }
        }
        if (page == 2)
        {
            if (Role == "Manager")
            {
                navigationManager.NavigateTo($"/Manager/CheckAgreement/{StaffId}");
            }
            else
            {
                navigationManager.NavigateTo($"/Admin/CheckAgreement/{StaffId}");
            }
        }
        if (page == 3)
        {
            if (Role == RoleTypeEnum.Manager.ToString())
            {
                navigationManager.NavigateTo($"/{RoleTypeEnum.Manager}/CheckGurantorAgreement/{StaffId}");
            }
            else
            {
                //navigationManager.NavigateTo($"/{RoleTypeEnum.Admin}/CheckGurantorAgreement/{StaffId}")
                navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Admin}/CheckGurantorAgreement/{StaffId}/0");
            }
        }
    }

    private async void TopageLoanAgreementOld(List<VLoanRequestContract> debtor, List<VLoanRequestContract> guaran, List<VLoanStaffDetail> staffIdOld, string? staffId)
    {
        LoanAgreementOldModel oldModel = new()
        {
            StaffId = staffId,
            StaffList = staffIdOld.Select(x => x.StaffId).ToList(),
            DebtorRequestId = debtor.Select(x => x.LoanRequestId).ToList(),
            GuaranRequestId = guaran.Select(x => x.LoanRequestId).ToList(),
            //IsShowDebtorSuccess = false,
            //IsShowGuaranSuccess = false,
        };

        await sessionStorage.SetItemAsync(StorageName, Search);
        await sessionStorage.SetItemAsync("OldAgreement", oldModel);

        navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Admin}/LoanAgreementOld");
    }

    private async Task<int> RequestDebtor(string? StaffId)
    {
        List<decimal> StutusID = new() { 0m, 1m, 2m, 4m };
        if (string.IsNullOrEmpty(StaffId))
        {
            return 0;
        }

        return await _context.VLoanRequestContracts
            .Where(c => c.DebtorStaffId == StaffId)
            .Where(c => StutusID.Contains(c.CurrentStatusId!.Value))
            .CountAsync();
    }

}
