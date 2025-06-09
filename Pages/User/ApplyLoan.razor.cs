using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LoanApp.Pages.User;

public partial class ApplyLoan
{
    #region CascadingParameter
    [CascadingParameter] public Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

    #endregion

    #region Parameter
    [Parameter] public decimal LoanTypeID { get; set; } = 0;

    [Parameter] public decimal RequestID { get; set; } = 0;

    #endregion

    #region Inject
    [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private INotificationService notificationService { get; set; } = null!;

    #endregion

    private ApplyLoanModel ModelApplyLoan { get; set; } = new();
    private List<VLoanStaffDetail> GuarantorList { get; set; } = new();
    private List<LoanType> LoanTypeList { get; set; } = new();
    private LoanRequest? Request { get; set; } = new();
    private LoanType LoanTypeModel { get; set; } = new();
    private StepsUserApplyLoanModel StepsUser { get; set; } = new() { Current = 0 };
    private ContractAttachment? AttachmentPdf { get; set; } = new();
    private VLoanStaffDetail? vLoanStaffDetailDebtor { get; set; } = null;

    /// <summary>
    /// ถ้ามีข้อมมูลจะไม่สามารถกู้ได้ เนื่อจจากไม่ส่งหลักฐานหลังได้รับเงิน
    /// </summary>
    private List<VLoanRequestContract> loanRequestContractsData { get; set; } = new();

    private string GuarantorStaffId { get; set; } = string.Empty;
    private string DebtorStaffId { get; set; } = string.Empty;
    private decimal[] AllowedStatus { get; } = new[] { 0m, 3m, 98m, 99m, 100m };
    private int MaxGuarant { get; } = 2;
    private bool IsSentParameterByRequestID { get; set; } = false;
    private string? SearchView { get; set; } = string.Empty;
    private decimal SelectedValue { get; set; } = 0;

    private bool IsCalculateDetail { get; set; } = false;

    protected async override Task OnInitializedAsync()
    {
        try
        {
            LoanTypeList = await psuLoan.GetAllLoanType(1);
            LoanTypeList.Insert(0, new() { LoanTypeId = 0, LoanTypeName = "กรุณาเลือกประเภทกู้ยืม" });

            DebtorStaffId = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);

            if (!string.IsNullOrEmpty(DebtorStaffId))
            {
                var loanTypeId = userService.GetTypeIdByStaff(DebtorStaffId, AllowedStatus);

                if (loanTypeId.Any())
                {
                    LoanTypeList = DistinctLoanType(loanTypeId, LoanTypeList);
                }

                if (LoanTypeID != 0 || RequestID != 0)
                {
                    if (LoanTypeID != 0)
                    {
                        SetData(LoanTypeID);
                    }
                    if (RequestID != 0)
                    {
                        IsSentParameterByRequestID = true;
                        await SetParameterByRequestID(RequestID);
                    }
                }
                else
                {
                    ModelApplyLoan.LoanTypeID = 0;
                    ModelApplyLoan.LoanAmount = 0;
                    ModelApplyLoan.LoanNumInstallments = 0;
                    ModelApplyLoan.Guarantor = string.Empty;
                    ModelApplyLoan.SalaryNetAmount = 0;
                    ModelApplyLoan.GuarantorId = string.Empty;
                }

                var FromLoan1 = await sessionStorage.GetItemAsStringAsync("FromLoan_1");
                var FromLoan2 = await sessionStorage.GetItemAsStringAsync("FromLoan_2");

                if (!string.IsNullOrEmpty(FromLoan1))
                {
                    await sessionStorage.RemoveItemAsync("FromLoan_1");
                }

                if (!string.IsNullOrEmpty(FromLoan2))
                {
                    await sessionStorage.RemoveItemAsync("FromLoan_2");
                }

            }
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
                vLoanStaffDetailDebtor = await psuLoan.GetUserDetailAsync(userStateProvider?.CurrentUser.StaffId);

                if (!Utility.CheckStaffTypeByDebtor(vLoanStaffDetailDebtor?.StaffType))
                {
                    ModelApplyLoan.LoanTypeID = 0;
                    ModelApplyLoan.LoanAmount = 0;
                    ModelApplyLoan.LoanNumInstallments = 0;
                    ModelApplyLoan.Guarantor = string.Empty;
                    ModelApplyLoan.SalaryNetAmount = 0;
                    ModelApplyLoan.GuarantorId = string.Empty;

                    LoanTypeID = 0;
                    RequestID = 0;
                }
                else
                {
                    loanRequestContractsData = await psuLoan.GetListAgreementDataFormVLoanRequestContract(userStateProvider?.CurrentUser.StaffId, new List<decimal>() { 6 }, 0);

                    if (loanRequestContractsData.Any())
                    {
                        loanRequestContractsData = loanRequestContractsData.OrderBy(x => x.PaidDate).ToList();

                        ModelApplyLoan.LoanTypeID = 0;
                        ModelApplyLoan.LoanAmount = 0;
                        ModelApplyLoan.LoanNumInstallments = 0;
                        ModelApplyLoan.Guarantor = string.Empty;
                        ModelApplyLoan.SalaryNetAmount = 0;
                        ModelApplyLoan.GuarantorId = string.Empty;

                        LoanTypeID = 0;
                        RequestID = 0;
                    }
                    else
                    {
                        if (LoanTypeID != 0)
                        {
                            SelectedValue = LoanTypeList.Find(x => x.LoanTypeId == LoanTypeID)!.LoanTypeId;

                            if (!IsSentParameterByRequestID)
                            {
                                var CheckLoanRecycle = await psuLoan.GetLoanRequestByStaffId(staffId: DebtorStaffId, loanTypeId: ModelApplyLoan.LoanTypeID, loanStatusId: 0);

                                if (CheckLoanRecycle != null)
                                {
                                    await SetParameterByRequestID(CheckLoanRecycle.LoanRequestId);
                                    RequestID = CheckLoanRecycle.LoanRequestId;
                                }
                                else
                                {
                                    RequestID = 0;
                                }
                            }
                        }

                        if (RequestID != 0)
                        {
                            var request = await psuLoan.GetLoanRequestByLoanRequestId(RequestID);

                            if (request != null)
                            {
                                SelectedValue = LoanTypeList.Find(x => x.LoanTypeId == request.LoanTypeId)!.LoanTypeId;
                            }
                        }
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

    private async Task SetParameterByRequestID(decimal requestId)
    {
        try
        {
            LoanRequest? request = await psuLoan.GetLoanRequestByLoanRequestId(requestId);

            if (request != null)
            {
                VLoanStaffDetail? guarantorData = await psuLoan.GetUserDetailAsync(request!.GuarantorStaffId);

                SetData((decimal)request.LoanTypeId!);
                ModelApplyLoan.LoanNumInstallments = (request.LoanNumInstallments != null ? (int)request.LoanNumInstallments : 0);
                ModelApplyLoan.LoanAmount = (request.LoanAmount != null ? (decimal)request.LoanAmount : 0);
                ModelApplyLoan.SalaryNetAmount = (request.SalaryNetAmount != null ? request.SalaryNetAmount : 0);
                ChangeValGuarantor(guarantorData);
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
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

    protected async Task SelectLoanTypeV2(string? value)
    {
        ModelApplyLoan.LoanTypeID = !string.IsNullOrEmpty(value) ? Convert.ToByte(value) : null;

        if (ModelApplyLoan.LoanTypeID != null || ModelApplyLoan.LoanTypeID != 0)
        {
            try
            {
                LoanType? loan = userService.GetLoanType(ModelApplyLoan.LoanTypeID);

                if (loan != null)
                {
                    ModelApplyLoan.LoanAmount = (loan!.LoanMaxAmount != null ? loan.LoanMaxAmount.Value : 0);
                    ModelApplyLoan.LoanNumInstallments = loan!.LoanNumInstallments;

                    if (!IsSentParameterByRequestID)
                    {
                        var CheckLoanRecycle = _context.LoanRequests
                            .Where(c => c.DebtorStaffId == DebtorStaffId)
                            .Where(c => c.LoanTypeId == ModelApplyLoan.LoanTypeID)
                            .Where(c => c.LoanStatusId == 0)
                            .FirstOrDefault();

                        if (CheckLoanRecycle != null)
                        {
                            await SetParameterByRequestID(CheckLoanRecycle.LoanRequestId);
                            RequestID = CheckLoanRecycle.LoanRequestId;
                        }
                        else
                        {
                            RequestID = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }
        }
        else
        {
            ModelApplyLoan.LoanAmount = 0;
            ModelApplyLoan.LoanNumInstallments = 0;
        }
    }

    private void SetData(decimal id)
    {
        LoanType? loanType = userService.GetLoanType((byte?)id);
        if (loanType != null)
        {
            LoanTypeModel = loanType;
            ModelApplyLoan.LoanTypeID = LoanTypeModel.LoanTypeId;
            ModelApplyLoan.LoanNumInstallments = LoanTypeModel.LoanNumInstallments;
            ModelApplyLoan.LoanAmount = (LoanTypeModel.LoanMaxAmount != null ?
                (decimal)LoanTypeModel.LoanMaxAmount : 0);
            ModelApplyLoan.Guarantor = "";
            ModelApplyLoan.SalaryNetAmount = 0;
        }
    }

    private async Task SaveToDbAsync()
    {
        try
        {
            var ControlStatus = "Add";

            #region เช็คว่าเคยยื่นกู้ไปไหม Stutus == 0
            LoanRequest? IsDistinctLoan = _context.LoanRequests
                .Where(c => c.DebtorStaffId == DebtorStaffId)
                .Where(c => c.LoanTypeId == ModelApplyLoan.LoanTypeID)
                .Where(c => c.LoanStatusId == 0)
                .FirstOrDefault();
            #endregion

            #region เคยกู้จะทำการ เข้าไปแก้ไขใน LoanRequestId อันเก่า
            if (IsDistinctLoan != null)
            {
                RequestID = IsDistinctLoan.LoanRequestId;
            }
            #endregion

            var ParentId = userService.GetLoanType(ModelApplyLoan.LoanTypeID);

            decimal LoanInstallment = SetLoanInstallment(ModelApplyLoan.LoanAmount, ModelApplyLoan.LoanNumInstallments, ModelApplyLoan.LoanTypeID);
            decimal LoanTotalAmount = SumTotalAmount(ModelApplyLoan);

            if (RequestID != 0)
            {
                ControlStatus = "Update";
                Request = _context.LoanRequests.Where(c => c.LoanRequestId == RequestID).FirstOrDefault();

                if (Request != null)
                {
                    ModelApplyLoan.LoanRequestId = (int)RequestID;
                    Request.LoanRequestId = ModelApplyLoan.LoanRequestId;
                    Request.LoanInstallment = LoanInstallment;
                    Request.LoanTotalAmount = LoanTotalAmount;
                }
            }

            if (!string.IsNullOrEmpty(DebtorStaffId))
            {
                Request!.DebtorStaffId = DebtorStaffId;
                Request.SalaryNetAmount = ModelApplyLoan.SalaryNetAmount;
                Request.GuarantorStaffId = ModelApplyLoan.GuarantorId;
                Request.LoanTypeId = ModelApplyLoan.LoanTypeID;
                Request.LoanAmount = ModelApplyLoan.LoanAmount;
                Request.LoanInterest = ParentId?.LoanInterest;
                Request.LoanNumInstallments = ModelApplyLoan.LoanNumInstallments;
                Request.LoanStatusId = 0;
                Request.LoanInstallment = LoanInstallment;
                Request.LoanTotalAmount = LoanTotalAmount;

                if (!string.IsNullOrEmpty(ControlStatus))
                {
                    if (ControlStatus == "Add")
                    {
                        _context.LoanRequests.Add(Request);
                    }

                    if (ControlStatus == "Update")
                    {
                        _context.Update(Request);
                    }

                    await _context.SaveChangesAsync();
                }

                /* เก็บ LoanRequestId */
                if (RequestID == 0)
                {
                    var id = Request.LoanRequestId;
                    ModelApplyLoan.LoanRequestId = (int)id;
                }

                // Check LoanRequestId != 0
                if (ModelApplyLoan.LoanRequestId == 0)
                {
                    LoanRequest? LRequest = _context.LoanRequests
                        .Where(c => c.DebtorStaffId == Request.DebtorStaffId)
                        .Where(c => c.GuarantorStaffId == Request.GuarantorStaffId)
                        .Where(c => c.LoanStatusId == 0)
                        .Where(c => c.SalaryNetAmount == Request.SalaryNetAmount)
                        .Where(c => c.LoanTypeId == Request.LoanTypeId)
                        .Where(c => c.LoanAmount == Request.LoanAmount)
                        .Where(c => c.LoanNumInstallments == Request.LoanNumInstallments)
                        .FirstOrDefault();
                    if (LRequest != null)
                    {
                        ModelApplyLoan.LoanRequestId = (int)LRequest.LoanRequestId;
                    }
                    else
                    {
                        navigationManager.NavigateTo($"/HomeUser");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
        }
    }

    private decimal SumTotalAmount(ApplyLoanModel? model)
    {
        decimal LoanTotalAmount = 0m;
        decimal TotalInterest = 0m;

        if (model == null)
        {
            return LoanTotalAmount;
        }

        var ListDate = SetPayDate((decimal)model.LoanNumInstallments);
        var loan = userService.GetLoanType(model.LoanTypeID);
        var BalAmount = model.LoanAmount;

        if (model.LoanNumInstallments != 0)
        {
            for (int i = 0; i < model.LoanNumInstallments; i++)
            {
                var index = i + 1;

                var PaidInstallment = SetLoanInstallment(model.LoanAmount, model.LoanNumInstallments, model.LoanTypeID);

                var Interest = GetInterest(ListDate, index, BalAmount, loan?.LoanInterest);
                TotalInterest += Interest;
                var Balance = PaidInstallment - Interest;

                // งวดสุดท้าย
                if (index == model.LoanNumInstallments)
                {
                    Balance = BalAmount;
                }

                BalAmount -= Balance;
            }

            LoanTotalAmount = Math.Round(model.LoanAmount + TotalInterest, 2);
        }
        return LoanTotalAmount;
    }

    /// <summary>
    /// เช็คจำนวนงวด
    /// </summary>
    /// <param name="LoanType"></param>
    /// <param name="staff"></param>
    /// <returns></returns>
    private bool CheckInstallments(LoanType LoanType, VLoanStaffDetail staff)
    {
        bool pass = false;
        if (!CheckStaffTypeByIncomeEmployee(staff.StaffType))
        {
            var Installment = ChangeDateToInstallments(staff.StaffRemainWorkingYear, staff.StaffRemainWorkingMonth);

            if (ModelApplyLoan.LoanNumInstallments > Installment)
            {
                // คุณไม่สามรถกู้ได้เพราะคุณเหลือจำนวนงวดไม่เพียงพอ
                return pass;
            }
        }

        if (ModelApplyLoan.LoanNumInstallments > LoanType.LoanNumInstallments)
        {
            // ระบุจำนวนงวดไม่ถูกต้อง จำนวนงวดต้องไม่เกิน {LoanType.LoanNumInstallments} งวด
            return pass;
        }

        pass = true;
        return pass;
    }

    /// <summary>
    ///  คำนวนวันหมดอายุงาน ทำงานมาเกิน 2 ปี
    /// </summary>
    /// <param name="staff"></param>
    /// <param name="roleType">1 ผู้กู้ || 2 ผู้ค้ำ</param>
    /// <param name="year"></param>
    /// <returns>false ไม่ได้ || true ได้</returns>
    private async Task<bool> CheckStaffTwoYear(VLoanStaffDetail? staff, int roleType = 1, int year = 2)
    {
        if (staff == null)
        {
            return false;
        }

        LoanStaffWorkingSpecial? specialVip = await psuLoan.GetLoanStaffWorkingSpecialByStaffId(staff.StaffId, 1);

        if (specialVip != null)
        {
            // กู้ได้เลย ไม่ตรวจสอบ อายุงาน
            return true;
        }

        // พนักงานเงินรายได้
        if (CheckStaffTypeByIncomeEmployee(staff.StaffType))
        {
            if (roleType == 2)
            {
                if (staff.StaffWorkingYear < 5)
                {
                    // อายุงานไม่ถึง 5 ปี
                    return false;
                }
            }
            else
            {
                if (staff.StaffWorkingYear < year)
                {
                    // อายุงานไม่ถึง 2 ปี
                    return false;
                }
            }
        }

        return await psuLoan.CheckWorkYearForStaff(staff, year);
    }

    private async Task<bool> CheckGuarantor()
    {
        VLoanStaffDetail? guarantorData = await psuLoan.GetUserDetailAsync(GuarantorStaffId);
        bool pass = true;

        if (guarantorData == null)
        {
            return false;
        }

        #region เช็คว่าคนที่ค้ำยังทำงานอยู่ไหม => StaffDepart
        if (guarantorData.StaffDepart != "3")
        {
            return false;
        }

        #endregion

        if (!await CheckStaffTwoYear(guarantorData, 2))
        {
            return false;
        }

        if (!CheckSamePeople(DebtorStaffId, GuarantorStaffId))
        {
            return false;
        }

        //if (await GetCountGuarant(GuarantorStaffId, AllowedStatus) >= MaxGuarant)
        if (await psuLoan.CountLoanAgreementGuarant(GuarantorStaffId, AllowedStatus.ToList()) >= MaxGuarant)
        {
            return false;
        }

        if (!Utility.CheckStaffTypeByGuarantor(guarantorData.StaffType))
        {
            return false;
        }

        // ผุ้ค้ำ
        if (!CheckMobileTel(GuarantorStaffId).IsPass)
        {
            return false;
        }

        // ผุ้ค้ำ
        if (!CheckWorkMobileTel(GuarantorStaffId).IsPass)
        {
            return false;
        }

        return pass;
    }

    /// <summary>
    /// เช็ค StaffType by Guarantor
    /// </summary>
    /// <param name="staffType"></param>
    /// <returns></returns>
    //private bool CheckStaffTypeByGuarantor(string? staffType)
    //{
    //    bool pass = false;
    //    StaffTypeModel SType = new();

    //    if (string.IsNullOrEmpty(staffType))
    //    {
    //        return pass;
    //    }

    //    if (SType.GovernmentOfficer.Contains(staffType))
    //    {
    //        return true;
    //    }
    //    else if (SType.Employee.Contains(staffType))
    //    {
    //        return true;
    //    }
    //    else if (SType.UniversityStaff.Contains(staffType))
    //    {
    //        return true;
    //    }

    //    return pass;
    //}

    /// <summary>
    /// พนักงานเงินรายได้
    /// </summary>
    /// <param name="staffType"></param>
    /// <returns></returns>
    private bool CheckStaffTypeByIncomeEmployee(string? staffType)
    {
        StaffTypeModel Stype = new();

        // พนักงานเงินรายได้
        if (!string.IsNullOrEmpty(staffType) && Stype.IncomeEmployee.Contains(staffType))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// เช็ค ผู้ค้ำเกิน 2 สัญญา
    /// </summary>
    /// <param name="StaffId"></param>
    /// <param name="AllowedStatus"></param>
    /// <returns></returns>
    //private async Task<int> GetCountGuarant(string StaffId, decimal[] AllowedStatus)
    //{
    //    List<VLoanRequestContract> checkGuarant = await psuLoan.GetAllGuarantor(StaffId, AllowedStatus.ToList());

    //    int Count = 0;
    //    if (checkGuarant.Any())
    //    {
    //        for (int i = 0; i < checkGuarant.Count; i++)
    //        {
    //            var GuarantorDetail = checkGuarant[i];
    //            if (GuarantorDetail.CurrentStatusId == 1)
    //            {
    //                ++Count;
    //            }
    //            else if (GuarantorDetail.LoanRequestGuaranStaffId == StaffId
    //                && GuarantorDetail.ContractGuarantorStaffId == StaffId)
    //            {
    //                ++Count;
    //            }
    //            else if (GuarantorDetail.ContractGuarantorStaffId == StaffId)
    //            {
    //                ++Count;
    //            }
    //        }
    //    }
    //    return Count;
    //}

    private async Task SubmitAsync()
    {
        var LoanType = LoanTypeList.Find(x => x.LoanTypeId == ModelApplyLoan.LoanTypeID);
        var debtorStaff = userService.GetUserDetail(DebtorStaffId);

        if (LoanType == null || debtorStaff == null)
        {
            return;
        }

        #region เช็ค Email หากไม่พบ ไม่ให้กู้(ผู้กู้)
        if (string.IsNullOrEmpty(debtorStaff.StaffEmail))
        {
            string alert = $"ท่านไม่สามารถยื่นกู้ได้เนื่องจากระบบไม่พบข้อมูล Email ในข้อมูลส่วนของท่าน";
            //await JS.InvokeVoidAsync("displayTickerAlert", alert)
            await notificationService.WarningDefult(alert);
            return;
        }

        #endregion

        #region เช็คผู้กู้ ทำงาน >= 2 year
        if (!await CheckStaffTwoYear(debtorStaff))
        {
            return;
        }

        #endregion

        #region เช็คผู้กู้ ประเภทพนักงาน
        if (!Utility.CheckStaffTypeByDebtor(debtorStaff.StaffType))
        {
            return;
        }

        #endregion

        #region เช็คเงินเดือนคงเหลือ
        if (ModelApplyLoan.SalaryNetAmount < 1)
        {
            return;
        }

        #endregion

        #region เช็คจำนวนเงินที่ต้องการกู้
        if (LoanType!.LoanMaxAmount != 0)
        {
            if (ModelApplyLoan.LoanAmount == 0)
            {
                return;
            }
            if (ModelApplyLoan.LoanAmount > LoanType.LoanMaxAmount)
            {
                // จำนวนเงินที่คุณขอยื่นกู้มากกว่าวงเงินสูงสุด
                return;
            }
        }

        #endregion

        #region เช็คจำนวนงวด [ตรวจเฉพาะของผู้กู้ ให้ admin ตรวจของผู้ค้ำ]
        if (ModelApplyLoan.LoanNumInstallments == 0)
        {
            return;
        }
        else
        {
            bool passCheckInstallmentsAsync = CheckInstallments(LoanType, debtorStaff);
            if (!passCheckInstallmentsAsync)
            {
                return;
            }
        }

        #endregion

        #region เบอร์โทรที่ทำงาน [ผู้กู้]
        if (!CheckMobileTel(DebtorStaffId).IsPass)
        {
            return;
        }

        #endregion

        #region เบอร์โทรศัพท์มือถือ [ผู้กู้]
        if (!CheckWorkMobileTel(DebtorStaffId).IsPass)
        {
            return;
        }

        #endregion

        #region เช็คผู้ค้ำ
        if (string.IsNullOrEmpty(GuarantorStaffId))
        {
            return;
        }
        else
        {
            // เรื่องงวดของผู้ค้ำให้ Admin เป็นคนเช็ค
            if (!await CheckGuarantor())
            {
                return;
            }
        }
        #endregion

        // เช็คเงินเดือน > 10%
        int NetPercent = Convert.ToInt32(debtorStaff.Salary * Utility.percentAmountTotal / 100);
        var PaidInstallment = SetLoanInstallment(ModelApplyLoan.LoanAmount, ModelApplyLoan.LoanNumInstallments, ModelApplyLoan.LoanTypeID);

        // payment = เงินเดือนคงเหลือ - เงินที่ต้องผ่อน
        int payment = Convert.ToInt32(ModelApplyLoan.SalaryNetAmount) - Convert.ToInt32(PaidInstallment);

        // ไม่สามารถกู้ได้ เนื่องจากเงินเดือนคงเหลือหลังหักหนี้สินไม่เพียงพอ
        if (payment < NetPercent)
        {
            return;
        }

        try
        {
            await SaveToDbAsync();
            await sessionStorage.SetItemAsync("FromLoan_1", ModelApplyLoan);
            navigationManager.NavigateTo($"/uploaddoc/{ModelApplyLoan.LoanTypeID}");
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    /// <summary>
    /// เช็คเงินเดือนคงเหลือ >= Utility.percentAmountTotal
    /// </summary>
    /// <param name="salary"></param>
    /// <param name="percentAmountTotal">เปอร์เซ็นต์</param>
    /// <returns></returns>
    private Action CheckSalaryAmount(decimal? salary, int percentAmountTotal)
    {
        var DebtorStaff = userService.GetUserDetail(DebtorStaffId);
        Action action = new()
        {
            IsPass = true,
            Message = $"เงินเดือนคงเหลือผ่านเกณฑ์"
        };

        int NetPercent = Convert.ToInt32(DebtorStaff?.Salary * percentAmountTotal / 100);
        var PaidInstallment = SetLoanInstallment(ModelApplyLoan.LoanAmount, ModelApplyLoan.LoanNumInstallments, ModelApplyLoan.LoanTypeID);

        // payment = เงินเดือนคงเหลือ - เงินที่ต้องผ่อน
        int payment = Convert.ToInt32(salary) - Convert.ToInt32(PaidInstallment);

        if (payment < NetPercent)
        {
            action.IsPass = false;
            action.Message = $"เงินเดือนคงเหลือหลังหักหนี้สินไม่เพียงพอ";
        }
        return action;
    }

    private int ChangeDateToInstallments(decimal? year, decimal? month)
    {
        var sum_Installments = 0;
        if (year != null || month != null)
        {
            sum_Installments = (int)((year! * 12) + month!);
        }
        return sum_Installments;
    }

    private void OpenPDF(LoanType? data)
    {
        if (data != null)
        {
            var StringTypeID = Convert.ToString(data.LoanTypeId);
            AttachmentPdf = _context.ContractAttachments
                .Where(c => c.AttachmentTypeName == StringTypeID)
                .FirstOrDefault();
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

    private void ChangeValGuarantor(VLoanStaffDetail? people)
    {
        if (people != null)
        {
            GuarantorList = new();
            var fullName = $"{userService.GetFullName(people)} ( {people.FacNameThai} )";
            ModelApplyLoan.Guarantor = fullName;
            SearchView = fullName;
            GuarantorStaffId = people.StaffId;
            ModelApplyLoan.GuarantorId = GuarantorStaffId;
        }
    }

    private Action LoanAmountNotification(decimal current, decimal? db)
    {
        Action Action = new();
        if (current <= db || db == 0)
        {
            Action.Message = $"จำนวนเงินกู้ผ่านเกณฑ์";
            Action.IsPass = true;
        }
        else
        {
            Action.Message = $"จำนวนเงินกู้มากกว่าเกณฑ์";
            Action.IsPass = false;
        }
        return Action;
    }

    /// <summary>
    /// Check จำนวนงวด
    /// </summary>
    /// <param name="current"></param>
    /// <param name="Max"></param>
    /// <returns></returns>
    private Action LoanNumInstallmentsNotification(int current, int? Max)
    {
        Action Action = new();

        if (Max != null)
        {
            if (current <= Max)
            {
                Action.Message = $"จำนวนงวดผ่านเกณฑ์";
                Action.IsPass = true;
            }
            else
            {
                Action.Message = $"จำนวนงวดมากกว่าเกณฑ์";
                Action.IsPass = false;
            }
        }

        return Action;
    }

    /// <summary>
    /// check อายุงาน ผู้กู้
    /// </summary>
    /// <param name="current"></param>
    /// <param name="db"></param>
    /// <returns></returns>
    private Action AgeStaffNotification(int current, int? db)
    {
        Action action = new();

        if (db != null)
        {
            var LoanType = LoanTypeList.Find(x => x.LoanTypeId == ModelApplyLoan.LoanTypeID);
            var DebtorStaff = userService.GetUserDetail(DebtorStaffId);

            if (LoanType != null && DebtorStaff != null)
            {
                bool passCheckInstallmentsAsync = CheckInstallments(LoanType, DebtorStaff);

                if (current <= db)
                {
                    if (passCheckInstallmentsAsync)
                    {
                        action.Message = $"อายุงานคงเหลือผ่านเกณฑ์";
                        action.IsPass = true;
                    }
                    else
                    {
                        action.Message = $"อายุงานคงเหลือไม่เพียงพอสำหรับการชำระเงินกู้";
                        action.IsPass = false;
                    }
                }
                else
                {
                    action.Message = $"อายุงานคงเหลือไม่เพียงพอสำหรับการชำระเงินกู้";
                    action.IsPass = false;
                }
            }
        }

        return action;
    }

    private async Task<Action> GuarantorNotification()
    {
        Action Action = new();

        if (!string.IsNullOrEmpty(ModelApplyLoan.GuarantorId))
        {
            var guarantorData = userService.GetUserDetail(GuarantorStaffId);

            if (guarantorData != null)
            {
                int countGuarant = await psuLoan.CountLoanAgreementGuarant(GuarantorStaffId, AllowedStatus.ToList());

                if (!await CheckStaffTwoYear(guarantorData, 2) ||
                    countGuarant >= MaxGuarant ||
                    !Utility.CheckStaffTypeByGuarantor(guarantorData.StaffType))
                {
                    Action.Message = $"ผู้ค้ำไม่ผ่านเกณฑ์";
                    Action.IsPass = false;
                }
                else if (!CheckSamePeople(DebtorStaffId, GuarantorStaffId))
                {
                    Action.Message = $"ไม่สามารถค้ำให้ตนเองได้";
                    Action.IsPass = false;
                }
                else
                {
                    Action.Message = $"ผู้ค้ำผ่านเกณฑ์";
                    Action.IsPass = true;
                }
            }
        }
        else
        {
            // ไม่มีเลือกคนค้ำ
            Action.Message = $"ผู้ค้ำไม่ผ่านเกณฑ์";
            Action.IsPass = false;
        }
        return Action;
    }

    /// <summary>
    /// เช็ค ผู้กู้ และ ผู้ค้ำ เป็คคนเดี่ยวกันไหม
    /// </summary>
    /// <param name="DebtorId"></param>
    /// <param name="GuarantorId"></param>
    /// <returns></returns>
    private static bool CheckSamePeople(string DebtorId, string GuarantorId)
    {
        bool pass = true;
        if (DebtorId == GuarantorId)
        {
            pass = false;
        }
        return pass;
    }

    private async Task<string> GetAttachment(byte? loanTypeId, decimal stepID)
    {
        //decimal stepID = 1
        string nameAttList = string.Empty;

        List<VAttachmentRequired> ListAttachment = await psuLoan.GetListVAttachmentRequired(loanTypeId, stepID);

        if (ListAttachment.Any())
        {
            for (int i = 0; i < ListAttachment.Count; i++)
            {
                var attachment = ListAttachment[i];
                var attName = attachment.AttachmentNameThai;

                if (i == 0)
                {
                    nameAttList = $"{attName}";
                }
                else
                {
                    nameAttList = $"{nameAttList}, {attName}";
                }
            }
        }
        else
        {
            nameAttList = "ไม่มี";
        }
        return nameAttList;
    }

    private decimal SetLoanInstallment(decimal LoanAmount, int LoanNumInstallments, byte? loanTypeId)
    {
        LoanType? loan = userService.GetLoanType(loanTypeId);
        var Installment = 0m;

        if (LoanNumInstallments != 0)
        {
            Installment = TransactionService.GetTransactionByInstallment(LoanAmount,
                (decimal)LoanNumInstallments,
                loan!.LoanInterest!.Value);
        }

        return Installment;
    }

    private decimal GetInterest(List<string> ListPayDate, int NumInstallments, decimal BalanceAmount, decimal? LoanInterest)
    {
        MonthModel _month = new();

        var TotalInstallments = Convert.ToDecimal(ModelApplyLoan.LoanNumInstallments);
        var _BalanceAmount = BalanceAmount;
        var payDateString = ListPayDate[NumInstallments - 1];
        DateTime odate = DateTime.Now;
        DateTime payDate = TransactionService.ChangeFormatPayDate(payDateString, _month.Number);

        decimal Interest = TransactionService.GetTransactionByInterest(TotalInstallments,
                       NumInstallments,
                       odate,
                       payDate,
                       _BalanceAmount,
                       LoanInterest!.Value);
        return Interest;
    }

    private List<string> SetPayDate(decimal LoanNumInstallments)
    {
        var ListDate = TransactionService.SetPayDate(DateTime.Now, LoanNumInstallments);
        return ListDate;
    }

    /// <summary>
    /// เช็คเบอร์โทร
    /// </summary>
    /// <param name="staffId"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    private Action CheckMobileTel(string staffId, string role = "")
    {
        Action action = new();
        LoanStaffDetail DebtorDetail = userService.GetLoanStaffDetailByStaffId(staffId);

        if (!string.IsNullOrEmpty(DebtorDetail.MobileTel))
        {
            action.Message = $"มีข้อมูลเบอร์โทรศัพท์มือถือ ({role})";
            action.IsPass = true;
        }
        else
        {
            action.Message = $"ไม่มีข้อมูลเบอร์โทรศัพท์มือถือ ({role})";
            action.IsPass = false;
        }
        return action;
    }

    /// <summary>
    /// เช็คเบอร์ที่ทำงาน
    /// </summary>
    /// <param name="staffId"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    private Action CheckWorkMobileTel(string staffId, string role = "")
    {
        Action action = new();
        LoanStaffDetail DebtorDetail = userService.GetLoanStaffDetailByStaffId(staffId);

        if (!string.IsNullOrEmpty(DebtorDetail.OfficeTel))
        {
            action.Message = $"มีข้อมูลเบอร์โทรที่ทำงาน ({role})";
            action.IsPass = true;
        }
        else
        {
            action.Message = $"ไม่มีข้อมูลเบอร์โทรที่ทำงาน ({role})";
            action.IsPass = false;
        }
        return action;
    }

    /// <summary>
    /// Check อายุการทำงาน
    /// </summary>
    /// <param name="staffId"></param>
    /// <returns></returns>
    private async Task<Action> CheckStartTwoYear(string? staffId)
    {
        Action action = new();
        VLoanStaffDetail? debtorDetail = userService.GetUserDetail(staffId);

        if (debtorDetail != null)
        {
            if (!await CheckStaffTwoYear(debtorDetail) || !Utility.CheckStaffTypeByDebtor(debtorDetail.StaffType))
            {
                action.Message = $"อายุการทำงานไม่ผ่านเกณฑ์";
                action.IsPass = false;
            }
            else
            {
                action.Message = $"อายุการทำงานผ่านเกณฑ์";
                action.IsPass = true;
            }
        }

        return action;
    }

    private async void OnSelectedItemChangedHandler(string? value)
    {
        if (value != null)
        {
            await SelectLoanTypeV2(value);
        }
    }

    private async Task SearchDataAsync(string? text)
    {
        GuarantorList = new();
        ModelApplyLoan.Guarantor = string.Empty;
        ModelApplyLoan.GuarantorId = string.Empty;
        GuarantorStaffId = string.Empty;

        try
        {
            if (!string.IsNullOrEmpty(text))
            {

                ModelApplyLoan.Guarantor = text.Trim();

                if ((text.Trim()).Length < Utility.SearchMinlength)
                {
                    await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
                }
                else
                {
                    GuarantorList = await _context.VLoanStaffDetails
                        .Where(c => c.StaffDepart == "3")
                        .Where(c => c.StaffNameThai!.Contains(ModelApplyLoan.Guarantor) ||
                        c.StaffSnameThai!.Contains(ModelApplyLoan.Guarantor) ||
                        (c.StaffNameEng!).ToLower().Contains(ModelApplyLoan.Guarantor.ToLower()) ||
                        (c.StaffSnameEng!).ToLower().Contains(ModelApplyLoan.Guarantor.ToLower()) ||
                        (c.StaffNameThai + " " + c.StaffSnameThai).Contains(ModelApplyLoan.Guarantor) ||
                        (c.StaffNameEng + " " + c.StaffSnameEng).ToLower().Contains(ModelApplyLoan.Guarantor.ToLower()))
                        //.Distinctby(x => x.StaffId)
                        .ToListAsync();
                }

                if (!GuarantorList.Any())
                {
                    SearchView = null;
                    ModelApplyLoan.Guarantor = string.Empty;
                    await notificationService.WarningDefult($"ไม่พบรายชื่อที่คุณค้นหา");
                }
            }
        }
        catch (Exception ex)
        {
            await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
        }
    }
}

public class Action
{
    public string Message { get; set; } = string.Empty;
    public bool IsPass { get; set; }
}
