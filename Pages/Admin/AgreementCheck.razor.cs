using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Services.IServices;

namespace LoanApp.Pages.Admin;

partial class AgreementCheck
{
    #region CascadingParameter
    [CascadingParameter] public Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #endregion

    [Parameter] public decimal RequestID { get; set; } = 0;

    #region Inject
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private INotificationService notificationService { get; set; } = null!;

    #endregion

    private List<SelectModel> Select { get; set; } = new();
    private VLoanRequestContract? ReqCon { get; set; } = new();
    private PaymentTransaction? Transaction { get; set; } = new();
    private FormAdminOptionModel FormOption { get; set; } = new();

    private bool IsEqualsLoanNumInstallments = false;

    protected async override Task OnInitializedAsync()
    {
        try
        {
            if (RequestID != 0)
            {
                ReqCon = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);

                if (ReqCon != null)
                {
                    Transaction = await GetPayment(ReqCon);
                    Select = SetDataSelect();

                    FormOption.StaffId = ReqCon?.DebtorStaffId;
                    FormOption.LoanRequestId = ReqCon?.LoanRequestId;
                    FormOption.ContractId = ReqCon?.ContractId;
                    FormOption.LoanAmount = (ReqCon?.ContractLoanAmount != null ?
                        ReqCon.ContractLoanAmount.Value :
                            ReqCon?.LoanRequestLoanAmount != null ?
                            ReqCon!.LoanRequestLoanAmount!.Value : 0);
                    FormOption.Select = new();
                    FormOption.Select = Select.Find(c => c.ID == 0);

                    if (Transaction?.InstallmentNo == ReqCon?.ContractLoanNumInstallments)
                    {
                        IsEqualsLoanNumInstallments = true;
                    }
                }

            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    protected void SelectTypeID(ChangeEventArgs e)
    {
        int _selectID = Convert.ToInt32(e.Value!.ToString());

        SelectModel? _Select = Select.Find(c => c.ID == _selectID);

        if (_Select != null)
        {
            FormOption.Select = Select.Find(c => c.ID == _selectID);
        }
        else
        {
            FormOption.Select = Select.Find(c => c.ID == 0);
        }
    }

    private async Task<PaymentTransaction?> GetPayment(VLoanRequestContract req)
    {
        PaymentTransaction? _paymentTransaction = new();

        try
        {
            //int payment = _context.PaymentTransactions
            //    .Where(c => c.ContractId == req.ContractId)
            //   .Select(c => new PaymentTransaction
            //   {
            //       ContractId = c.ContractId,
            //       InstallmentNo = c.InstallmentNo
            //   })
            //   .Distinct()
            //   .Count();

            int payment = await psuLoan.GetCountPaymentTransactionsByContractId(req.ContractId);

            if (payment != 0)
            {
                //var _transaction = _context.PaymentTransactions
                //    .Where(c => c.ContractId == req.ContractId)
                //    .Where(c => c.InstallmentNo == payment)
                //    .FirstOrDefault();

                var _transaction = await psuLoan.GetPaymentTransactionByInstallmentNo(payment, req.ContractId);

                if (payment == req.ContractLoanNumInstallments)
                {
                    _paymentTransaction = _transaction;
                }
                else
                {
                    _paymentTransaction = SetTransaction(req, payment, 1);

                    //var _InstallDetail = _context.InstallmentDetails
                    //.Where(c => c.ContractId == req.ContractId)
                    //.Where(c => c.InstallmentNo == _paymentTransaction.InstallmentNo)
                    //.FirstOrDefault();

                    var _InstallDetail = await psuLoan.GetInstallmentDetailByContractId(req.ContractId, _paymentTransaction.InstallmentNo);

                    if (_InstallDetail != null)
                    {
                        _paymentTransaction.PrincipleAmount = _InstallDetail.PrincipleAmount;
                        _paymentTransaction.InterestAmont = _InstallDetail.InterestAmont;
                        _paymentTransaction.TotalAmount = _InstallDetail.TotalAmount;
                    }

                    if (payment != 0)
                    {
                        _paymentTransaction.BalanceAmount = _transaction?.BalanceAmount;
                    }
                }
            }
            else
            {
                _paymentTransaction = SetTransaction(req, payment, 1);
            }
            return _paymentTransaction;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private PaymentTransaction SetTransaction(VLoanRequestContract data, int PaymentNumInstallments, int Number)
    {
        return new PaymentTransaction()
        {
            ContractId = data.ContractId,
            InstallmentNo = PaymentNumInstallments + Number,
            PrincipleAmount = 0,
            InterestAmont = 0,
            TotalAmount = 0,
            ContractNo = data.ContractNo,
            BalanceAmount = data.ContractLoanTotalAmount,
        };
    }

    private List<SelectModel> SetDataSelect()
    {
        return new List<SelectModel>
        {
            new() { Name = "กรุณาเลือกการดำเนินการ", ID = 0 },
            new() { Name = "ปิด/โปะยอดการกู้", ID = 1 },
            new() { Name = "ปรับดอกเบี้ยเพิ่ม", ID = 2 },
            new() { Name = "เพิ่ม/เปลี่ยนผู้ค้ำ", ID = 3 }
        };
    }

    private void Back(VLoanRequestContract? ReqCon)
    {
        navigationManager.NavigateTo($"/Admin/AgreementDetail/{ReqCon?.LoanRequestId}");
    }

    private void FormAdminChange(FormAdminOptionModel data)
    {
        FormOption = data;
        StateHasChanged();
    }

    private async Task NextPageAsync()
    {
        switch (FormOption.Select?.ID)
        {
            case 1: /// ปิด/โปะยอดการกู้
                if (!string.IsNullOrEmpty(FormOption.PayOff.ReferenceId1))
                {
                    try
                    {

                        PaymentTransaction? checkInstallmentNo = await psuLoan.GetPaymentTransactionByInstallmentNo(FormOption.PayOff.InstallmentNo, FormOption.ContractId);

                        if (checkInstallmentNo != null)
                        {
                            ContractMain? contract = await psuLoan.GeContractMainByContractIdAsync(FormOption.ContractId);

                            var mess = $"พบข้อมูลงวดที่ {FormOption.PayOff.InstallmentNo} ของคุณ{userService.GetFullNameNoTitleName(contract?.DebtorStaffId)} แล้วจึงไม่สามารถเพิ่มข้อมูลดังกล่าวได้";
                            _ = Task.Run(() => { notificationService.Warning(mess, "แจ้งเตือน", false); });

                            return;
                        }


                        #region งวดสุดท้าย BalanceValue <= 0
                        if (Transaction?.InstallmentNo == ReqCon?.ContractLoanNumInstallments)
                        {
                            //decimal? checkBalanceAmount = FormOption.PayOff.BalanceAmount - FormOption.PayOff.TotalAmount;
                            decimal? checkBalanceAmount = FormOption.PayOff.BalanceValue;

                            if (checkBalanceAmount == null)
                            {
                                string alert = "ยอดเงินกู้คงเหลือ == null";
                                _ = Task.Run(() => { notificationService.WarningDefult(alert); });
                                return;
                            }

                            if (checkBalanceAmount > 0)
                            {
                                string alert = "ไม่สามารถดำเนินการได้ กรุณาตรวจสอบยอดเงิน (งวดสุดท้าย)";
                                _ = Task.Run(() => { notificationService.WarningDefult(alert); });
                                return;
                            }
                        }
                        #endregion

                        string? remark = FormOption.PayOff.ContractRemark;

                        if (!string.IsNullOrEmpty(remark) && remark.Length >= 390)
                        {
                            remark = FormOption.PayOff.ContractRemark.Substring(0, 390) + "...";
                        }

                        PaymentTransaction payment = new()
                        {
                            ContractId = FormOption.ContractId,
                            InstallmentNo = FormOption.PayOff.InstallmentNo,
                            PayDate = FormOption.PayOff.Date,
                            PrincipleAmount = FormOption.PayOff.PrincipleAmount,
                            InterestAmont = FormOption.PayOff.InterestAmont,
                            TotalAmount = FormOption.PayOff.TotalAmount,
                            BalanceAmount = FormOption.PayOff.BalanceAmount,
                            ContractNo = ReqCon?.ContractNo,
                            ReferenceId1 = FormOption.PayOff.ReferenceId1,
                            Remark = remark,
                        };


                        await SavePayOffAsync(payment, FormOption.PayOff);
                    }
                    catch (Exception ex)
                    {
                        await Error.ProcessError(ex);
                    }
                }
                else
                {
                    string alert = "กรุณากรอกใบเสร็จเลขที่";
                    _ = Task.Run(() => { notificationService.WarningDefult(alert); });
                    return;
                }
                break;

            case 2: /// ปรับดอกเบี้ยเพิ่ม
                navigationManager.NavigateTo("/Admin/ManageLoanAgreement");
                break;

            case 3: /// เพิ่ม/เปลี่ยนผู้ค้ำ
                try
                {
                    bool IsPass = await CheckGuarantorAsync(FormOption.ChangeGuarantor.GuarantorStaffIdNow, FormOption.ChangeGuarantor.NewGuarantorStaffId);

                    if (IsPass)
                    {
                        await SaveToDbByChangeGuarantorAsync(FormOption.ChangeGuarantor.NewGuarantorStaffId);
                        navigationManager.NavigateTo("/Admin/ManageLoanAgreement");
                    }
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
                break;
        }
    }

    private async Task<bool> CheckGuarantorAsync(string Old_GuarantorStaffId, string? New_GuarantorId)
    {
        bool pass = false;
        decimal? ResultInstallment = null;
        decimal[] statusLoanGuarantor = new[] { 0m, 3m, 99m, 100m };
        int MaxGuarant = 2;
        string AlertDefault = "ไม่สามารถเปลี่ยนผู้ค้ำได้เนื่องจาก";

        try
        {
            #region New_GuarantorId == null
            if (string.IsNullOrEmpty(New_GuarantorId))
            {
                FormOption.ChangeGuarantor.NewGuarantorStaffId = null;
                string alert = "กรุณาค้นหารายชื่อผู้ค้ำที่ต้องการเปลี่ยน";
                await notificationService.Warning(alert, "แจ้งเตือน", false);
                return pass;
            }

            #endregion

            ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(FormOption.ContractId);

            if (main != null)
            {
                /* ข้อมูลที่ต้องตรวจสอบของการเปลี่ยนผู้ค้ำ */
                VLoanStaffDetail? GuarantorDetail = await psuLoan.GetUserDetailAsync(New_GuarantorId);
                List<GuarantorWorkingSpecialModel> guarantorWorkings = new();

                if (GuarantorDetail == null)
                {
                    await notificationService.Warning("ไม่พบข้อมูล บุคคลนี้", "แจ้งเตือน", false);
                }

                // เช็คว่าได้สิทธิพิเศษจากการ เพิ่มของ Admin หรือไหม
                LoanStaffWorkingSpecial? SpecialVip = await psuLoan.GetLoanStaffWorkingSpecialByStaffId(GuarantorDetail?.StaffId);

                #region 1.เช็คงวด
                var Installment = ChangeDateToInstallments(GuarantorDetail?.StaffRemainWorkingYear,
                     GuarantorDetail?.StaffRemainWorkingMonth);

                int userPayment = await psuLoan.GetCountPaymentTransactionsByContractId(main.ContractId);

                ResultInstallment = main.LoanNumInstallments - userPayment;

                if (ResultInstallment == null)
                {
                    string alert = $"ไม่พบจำนวนงวด";
                    await notificationService.Warning(alert, "แจ้งเตือน", false);
                    return pass;
                }

                if (ResultInstallment!.Value > Installment)
                {
                    string alert = $"{AlertDefault} จำนวนงวดของผู้ค้ำไม่เพียงพอ";
                    await notificationService.Warning(alert, "แจ้งเตือน", false);
                    return pass;
                }
                #endregion

                #region 3. ทำงานอยู่ไหม
                if (GuarantorDetail?.StaffDepart != "3")
                {
                    string alert = $"{AlertDefault} ผู้ค้ำได้ทำการลาออกไปแล้ว";
                    await notificationService.Warning(alert, "แจ้งเตือน", false);
                    return pass;
                }
                #endregion

                #region 5. ผู้กู้ และ ผู้ค้ำ เป็คคนเดี่ยวกันไหม
                if (ReqCon?.DebtorStaffId == New_GuarantorId)
                {
                    string alert = $"{AlertDefault} ผู้กู้และผู้ค้ำ คือคนเดียวกัน";
                    await notificationService.Warning(alert, "แจ้งเตือน", false);
                    return pass;
                }
                #endregion

                #region 6. ผู้ค้ำเดิม และผู้ค้ำใหม่ เป็คคนเดี่ยวกันไหม
                if (Old_GuarantorStaffId == New_GuarantorId)
                {
                    string alert = $"{AlertDefault} เป็นผู้ค้ำคนเดิม";
                    await notificationService.Warning(alert, "แจ้งเตือน", false);
                    return pass;
                }

                #endregion

                #region 7. เช็ค StaffType {SpecialVip}
                if (!Utility.CheckStaffTypeByGuarantor(GuarantorDetail.StaffType))
                {
                    if (SpecialVip == null)
                    {
                        string alert = $"{AlertDefault} ประเภทบุคลากรไม่ถูกต้อง";
                        await notificationService.Warning(alert, "แจ้งเตือน", false);
                        return pass;
                    }
                    else
                    {
                        GuarantorWorkingSpecialModel specialModel = new()
                        {
                            CaseNo = (int)CaseCheckLoanGuarantor.AgreementCount,
                            Message = $" ประเภทบุคลากรไม่ถูกต้อง"
                        };
                        guarantorWorkings.Add(specialModel);
                    }
                }

                #endregion

                #region 2. เกิน 2 สัญญา {SpecialVip}
                if (await psuLoan.CountLoanAgreementGuarant(New_GuarantorId, statusLoanGuarantor.ToList()) >= MaxGuarant)
                {
                    if (SpecialVip == null)
                    {
                        string alert = $"{AlertDefault} ผู้ค้ำ ค้ำมากกว่า {MaxGuarant} สัญญา";
                        await notificationService.Warning(alert, "แจ้งเตือน", false);
                        return pass;
                    }
                    else
                    {
                        GuarantorWorkingSpecialModel specialModel = new()
                        {
                            CaseNo = (int)CaseCheckLoanGuarantor.AgreementCount,
                            Message = $" ผู้ค้ำ ค้ำมากกว่า {MaxGuarant} สัญญา"
                        };
                        guarantorWorkings.Add(specialModel);
                    }
                }

                #endregion

                #region 4. ทำงานมาเกิน 2 ปี {SpecialVip}

                StaffTypeModel sType = new();
                int year = sType.IncomeEmployee.Contains(GuarantorDetail.StaffType) ? 5 : 2;

                if (!await psuLoan.CheckWorkYearForStaff(GuarantorDetail, year))
                {
                    string alert = $"{AlertDefault} ผู้ค้ำทำงานมาไม่เกิน {year} ปี";

                    if (SpecialVip == null)
                    {
                        await notificationService.Warning(alert, "แจ้งเตือน", false);
                        return pass;
                    }
                    else
                    {
                        GuarantorWorkingSpecialModel specialModel = new()
                        {
                            CaseNo = (int)CaseCheckLoanGuarantor.Workless,
                            Message = $" ผู้ค้ำทำงานมาไม่เกิน {year} ปี"
                        };
                        guarantorWorkings.Add(specialModel);
                    }
                }


                #endregion

                if (guarantorWorkings.Any())
                {
                    string resultMess = $"ต้องการยืนยันการเปลี่ยน / เพิ่มผู้ค้ำ ( ";
                    for (int i = 0; i < guarantorWorkings.Count; i++)
                    {
                        GuarantorWorkingSpecialModel item = guarantorWorkings[i];
                        resultMess += (guarantorWorkings.Count != i + 1 ? $"{item.Message} และ" : $"{item.Message}");
                    }
                    resultMess += $" )";
                    bool CheckConfirmButton = await JS.InvokeAsync<Boolean>("ConfirmButton", resultMess);
                    if (!CheckConfirmButton)
                    {
                        return pass;
                    }
                }
            }
            return true;
        }
        catch (Exception)
        {

            throw;
        }

    }

    private async Task SaveToDbByChangeGuarantorAsync(string? NewGuarantorStaffId)
    {
        try
        {
            decimal? ContractStatusId = null;
            ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(FormOption.ContractId);

            if (main != null)
            {
                ContractStatusId = main.ContractStatusId;
                main.GuarantorStaffId = NewGuarantorStaffId;

                await psuLoan.UpdateContractMain(main);

                await SaveToHistoryAsync(main, ContractStatusId);
            }
        }
        catch (Exception)
        {

            throw;
        }
    }

    private async Task SavePayOffAsync(PaymentTransaction _transaction, PayOffModel payOff)
    {
        //Files/9_0001972/Other
        var DIR = $"{Utility.Files_DIR}\\{ReqCon?.LoanRequestId}_{ReqCon?.DebtorStaffId}\\{Utility.Othrt_DIR}";
        decimal? ContractStatusId = 0;

        try
        {
            ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(ReqCon?.ContractId);

            if (main == null)
            {
                _ = Task.Run(() => { notificationService.ErrorDefult($"ไม่พบข้อมูล ContractId:{ReqCon?.ContractId}"); });
                return;
            }

            if (_transaction.BalanceAmount == 0)
            {
                await SaveFileAndImgService.SaveToFolderImagesAsync(FormOption.PayOff.ReferenceFile, DIR, ReqCon!.LoanRequestId);

                //คนเก่า || คนที่ชำระหมดแล้วแต่ยังค้างอยู่ในระบบ(stutus != 99) => จะทำการปรับให้
                await SaveToDbByPayOffAsync(main, ContractStatusId);

                navigationManager.NavigateTo("/Admin/ManageLoanAgreement/8");
            }
            else if (_transaction.TotalAmount > 0)
            {
                try
                {
                    ContractStatusId = main.ContractStatusId;
                    //var CheckBalance = GetBalanceTotalAsync() - _transaction.TotalAmount
                    var CheckBalance = payOff.BalanceValue;

                    if (IsEqualsLoanNumInstallments)
                    {
                        await SaveFileAndImgService.SaveToFolderImagesAsync(FormOption.PayOff.ReferenceFile, DIR, ReqCon!.LoanRequestId);

                        //คนเก่า || คนที่ชำระหมดแล้วแต่ยังค้างอยู่ในระบบ(stutus != 99) => จะทำการปรับให้
                        await SaveToDbByPayOffAsync(main, ContractStatusId);
                    }
                    else if (CheckBalance <= 0)
                    {
                        //ยอดชำระในการปิดยอดถูกต้อง
                        _transaction.BalanceAmount = CheckBalance;

                        await SaveFileAndImgService.SaveToFolderImagesAsync(FormOption.PayOff.ReferenceFile, DIR, ReqCon!.LoanRequestId);
                        await SaveToDbByPayOffAsync(main, ContractStatusId);
                        await SaveToPaymentTransactionByPayOffAsync(_transaction);
                        await SetDataBySentEmailByPayOffAsync(ReqCon, _transaction);
                    }
                    else if (CheckBalance != 0)
                    {
                        string alert =
                            $"คุณได้โปะยอดเงินกู้จำนวน {string.Format("{0:n2}", _transaction.TotalAmount)} บาท \n" +
                            $"ไม่ได้ปิดยอด \n" +
                            $"(คงเหลือยอดเงินกู้ที้่ต้องชำระ {string.Format("{0:n2}", CheckBalance)} บาท) \n" +
                            $"ยืนยันการดำเนินการใช่หรือไม่ ?";

                        bool CheckConfirmButton = await JS.InvokeAsync<Boolean>("ConfirmButton", alert);

                        if (CheckConfirmButton)
                        {
                            _transaction.BalanceAmount = CheckBalance;

                            await SaveFileAndImgService.SaveToFolderImagesAsync(FormOption.PayOff.ReferenceFile, DIR, ReqCon!.LoanRequestId);
                            await SaveToPaymentTransactionByPayOffAsync(_transaction);
                            await SetDataBySentEmailByPayOffAsync(ReqCon, _transaction);
                        }
                        else
                        {
                            return;
                        }
                    }
                    navigationManager.NavigateTo("/Admin/ManageLoanAgreement/8");
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                await notificationService.WarningDefult("กรุณาตรวจสอบยอดเงินที่กรอกมา");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task SaveToDbByPayOffAsync(ContractMain Contract, decimal? ContractStatusId)
    {
        ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(Contract.ContractId);

        if (main != null)
        {
            main.ContractStatusId = 99;
            main.ContractRemark = $"{FormOption.PayOff.ContractRemark} [{dateService.ChangeDate(FormOption.DateNow, "dd MM yyyy", Utility.DateLanguage_TH)}]";

            await psuLoan.UpdateContractMain(main);

            await SaveToHistoryAsync(main, ContractStatusId);
        }
    }

    private async Task SaveToPaymentTransactionByPayOffAsync(PaymentTransaction pTransaction)
    {
        try
        {
            pTransaction.CreatedBy = StateProvider?.CurrentUser.StaffId;
            pTransaction.CreatedDate = DateTime.Now;

            await psuLoan.AddPaymentTransaction(pTransaction);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task SetDataBySentEmailByPayOffAsync(VLoanRequestContract ReqCon, PaymentTransaction transaction)
    {
        try
        {
            VLoanStaffDetail? StaffDetail = await psuLoan.GetUserDetailAsync(ReqCon.DebtorStaffId);
            var DebtorName = userService.GetFullNameNoTitleName(ReqCon.DebtorStaffId);

            VLoanStaffDetail? GuarantorDetail = await psuLoan.GetUserDetailAsync(ReqCon.ContractGuarantorStaffId);
            var GuarantoName = userService.GetFullNameNoTitleName(ReqCon.ContractGuarantorStaffId);

            ApplyLoanModel loan = new()
            {
                LoanTypeID = ReqCon.LoanTypeId,
                LoanAmount = (ReqCon.ContractLoanAmount != null ? ReqCon.ContractLoanAmount.Value : 0),
                LoanInterest = ReqCon.ContractLoanNumInstallments
            };

            if (StaffDetail != null && GuarantorDetail != null)
            {
                #region ผู้กู้
                if (!string.IsNullOrEmpty(StaffDetail.StaffEmail))
                {
                    var Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);
                    var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);

                    var email = MailService.MailDebtorByAdminImportPayment(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction);
                    MailService.SendEmail(email);
                }
                #endregion

                #region ผู้ค้ำ
                if (!string.IsNullOrEmpty(GuarantorDetail.StaffEmail))
                {
                    var Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);
                    var Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);

                    var email = MailService.MailDebtorByAdminImportPayment(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction);
                    MailService.SendEmail(email);
                }
                #endregion
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task SaveToHistoryAsync(ContractMain? Contract = null,
        decimal? ContractStatusId = null,
        LoanRequest? Request = null,
        decimal? RequestStatusId = null)
    {
        string ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
        if (Contract != null)
        {
            await LogService.GetHisContractMainByContractIDAsync(Contract.ContractId,
                ContractStatusId,
                ModifyBy);
        }

        if (Request != null)
        {
            await LogService.GetHisLoanRequestByRequestIDAsync(Request.LoanRequestId,
               RequestStatusId,
               ModifyBy);
        }
    }

    private int ChangeDateToInstallments(decimal? year, decimal? month)
    {
        int sum_Installments = 0;
        if (year != null || month != null)
        {
            sum_Installments = (int)((year! * 12) + month!);
        }
        return sum_Installments;
    }

    //private async Task<int> GetCountGuarant(string staffId, decimal[] AllowedStatus)
    //{

    //    var checkGuarant = await psuLoan.GetAllGuarantor(staffId, AllowedStatus.ToList());

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
    //            else if (GuarantorDetail.LoanRequestGuaranStaffId == staffId &&
    //                GuarantorDetail.ContractGuarantorStaffId == staffId)
    //            {
    //                ++Count;
    //            }
    //            else if (GuarantorDetail.ContractGuarantorStaffId == staffId)
    //            {
    //                ++Count;
    //            }
    //        }
    //    }
    //    return Count;
    //}

    //private static bool CheckStaffTwoYear(VLoanStaffDetail? staff)
    //{
    //    bool pass = false;
    //    if (staff != null)
    //    {
    //        // คำนวนวันหมดอายุงาน ทำงานมาเกิน 2 ปี
    //        if (staff.StaffAcceptDate != null && staff.StaffEnd != null)
    //        {
    //            #region อายุงานไม่ถึง 2 ปี
    //            if (staff.StaffWorkingYear < 2)
    //            {
    //                return pass;
    //            }
    //            #endregion
    //        }
    //        else
    //        {
    //            #region  วันที่ เริ่ม/สิ้นสุด การทำงานไม่มีข้อมูล
    //            return pass;
    //            #endregion
    //        }
    //        pass = true;
    //    }
    //    return pass;
    //}

    //private bool CheckStaffTypeByGuarantor(string? staffType)
    //{
    //    bool pass = false;
    //    StaffTypeModel SType = new();

    //    if (!string.IsNullOrEmpty(staffType))
    //    {
    //        if (SType.GovernmentOfficer.Contains(staffType))
    //        {
    //            return true;
    //        }
    //        else if (SType.Employee.Contains(staffType))
    //        {
    //            return true;
    //        }
    //        else if (SType.UniversityStaff.Contains(staffType))
    //        {
    //            return true;
    //        }
    //    }
    //    return pass;
    //}
}

public class GuarantorWorkingSpecialModel
{
    public int CaseNo { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum CaseCheckLoanGuarantor
{
    AgreementCount = 2,
    Workless = 4,
}
