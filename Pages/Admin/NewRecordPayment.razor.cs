using AntDesign;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Linq.Dynamic.Core;

namespace LoanApp.Pages.Admin
{
    public partial class NewRecordPayment
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }
        [CascadingParameter] private MainLayout Layout { get; set; } = null!;

        #endregion

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private ITransactionService transactionService { get; set; } = null!;
        [Inject] private ModalService ModalService { get; set; } = null!;

        #endregion

        private List<LoanType> LoanTypeList { get; set; } = new();
        private List<VLoanRequestContract> ListContract { get; set; } = new();
        private IOrderedQueryable<VLoanRequestContract> Query { get; set; } = null!;
        private List<PaymentTransaction> SelectRequest { get; set; } = new();
        private List<PaymentTransaction> TransactionListHis { get; set; } = new();

        private List<decimal> StatusId { get; } = new() { 0m, 1m, 2m, 3m, 4m, 5m, 9m, 11m, 98m, 99m, 100m };
        private List<decimal> SelectRequestId { get; set; } = new();

        private string StorageName { get; } = "ImportPaymentCSV";
        private string? SearchVal { get; set; } = null;
        private bool IsAgreementSuccess { get; set; } = false;
        private DateTime? DateValueCheck { get; set; } = null;
        private bool PaymentConfirm { get; set; } = false;
        private bool VisibleTransaction { get; set; } = false;
        private decimal TypeID { get; set; } = 0m;
        private bool IsEditBalanceAmount { get; set; } = false;
        private bool IsMobile { get; set; } = false;
        private bool IsRefaceTale { get; set; } = false;


        /// <summary>
        /// รองรับทีละคน ไม่รองรับหลายคน
        /// </summary>
        private decimal? BalanceAmountTemp { get; set; } = null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    IsMobile = await userService.CheckDevice();

                    LoanTypeList = await psuLoan.GetAllLoanType();
                    LoanTypeList.Insert(0, new LoanType()
                    {
                        LoanTypeId = 0,
                        LoanTypeName = "ทุกประเภท"
                    });

                    LoanPaymentResult();
                    await DataTable();

                    pageLogeing = false;
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    pageLogeing = false;
                    StateHasChanged();
                    await Error.ProcessError(ex);
                }
            }
        }

        private void LoanPaymentResult()
        {
            string? adminCampId = StateProvider?.CurrentUser.CapmSelectNow;

            try
            {
                Query = _context.VLoanRequestContracts
                   .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                   .Where(c => string.IsNullOrEmpty(adminCampId) || c.DebtorCampusId == adminCampId)
                   .OrderBy(x => x.DebtorNameTh);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task SetCurrentDataUploadCSVAsync(UploadModel value)
        {
            UploadModel ModelUploadCSV = new();

            ModelUploadCSV.Id = value.Id;
            ModelUploadCSV.Name = value.Name;
            ModelUploadCSV.Url = value.Url;
            ModelUploadCSV.TempImgName = value.TempImgName;
            ModelUploadCSV.AttachmentTypeId = value.AttachmentTypeId;

            if (!string.IsNullOrEmpty(ModelUploadCSV.Url))
            {
                await sessionStorage.SetItemAsync(StorageName, ModelUploadCSV);
                //navigationManager.NavigateTo($"/Admin/ImportPayment")
                navigationManager.NavigateTo($"/Admin/ImportPaymentV2");
            }
        }

        private async Task OnSearch(string? val)
        {
            try
            {
                if (string.IsNullOrEmpty(val))
                {
                    IsRefaceTale = true;
                    StateHasChanged();
                    await Task.Delay(1);

                    ListContract = await Query.ToListAsync();

                    IsRefaceTale = false;
                    StateHasChanged();
                    return;
                }
                else if ((val.Trim()).Length < Utility.SearchMinlength)
                {
                    await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา เลขที่สัญญา/ชื่อ-สกุล อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
                    return;
                }

                IsRefaceTale = true;
                StateHasChanged();
                await Task.Delay(1);

                await DataTable(val.Trim(), TypeID);

                IsRefaceTale = false;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                await notificationService.Error(notificationService.ExceptionLog(ex));
            }
        }

        private async Task DataTable(string? searchName = null, decimal typeId = 0)
        {
            ListContract = new();

            try
            {
                ListContract = await Query
                .Where(c => string.IsNullOrEmpty(searchName) ||
                ((c.DebtorNameTh!.Contains(searchName) ||
                c.DebtorSnameTh!.Contains(searchName) ||
                c.DebtorNameEng!.ToLower().Contains(searchName.ToLower()) ||
                c.DebtorSnameEng!.ToLower().Contains(searchName.ToLower()) ||
                (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower())) ||
                c.ContractNo!.Contains(searchName)))
                .Where(c => typeId == 0 || c.LoanTypeId == typeId)
                .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<decimal?> GetNumInstallment(decimal? contractId)
        {
            try
            {
                PaymentTransaction? payment = await psuLoan.GetLastPaymentTransactionByContractIdAsync(contractId);

                if (payment != null)
                {
                    return ((decimal)(payment.InstallmentNo != null ? payment.InstallmentNo + 1 : 1));
                }
                else
                {
                    return 1;
                }
            }
            catch (Exception ex)
            {
                await notificationService.Error(notificationService.ExceptionLog(ex));
                return null;
            }
        }

        private async Task SelectData(VLoanRequestContract data)
        {
            DateValueCheck = null;

            IsEditBalanceAmount = false;
            PaymentConfirm = false;
            IsAgreementSuccess = false;
            SelectRequestId = new();
            SelectRequest = new();
            TransactionListHis = new();

            try
            {
                PaymentTransaction? paymentTransaction = await psuLoan.GetLastPaymentTransactionByContractIdAsync(data.ContractId);

                decimal? installmentNo_now = 0;
                decimal? loanTotalAmount = null;

                if (paymentTransaction != null) /// มีการเคยจ่ายเงิน
                {
                    if (paymentTransaction.InstallmentNo != data.ContractLoanNumInstallments)
                    {
                        installmentNo_now = paymentTransaction!.InstallmentNo;
                        loanTotalAmount = paymentTransaction!.BalanceAmount;
                    }
                    else
                    {
                        _ = Task.Run(() => { notificationService.WarningDefult("จำนวนงวดเกินกว่าที่จะสามารถจ่ายได้"); });

                        IsAgreementSuccess = true;
                        return;
                    }

                    PaymentConfirm = true;
                }
                else
                {
                    installmentNo_now = 0;

                    loanTotalAmount = data.ContractLoanAmount != null ? data.ContractLoanAmount : data.LoanRequestLoanAmount;
                }

                PaymentTransaction payment = SetTransaction(data, installmentNo_now, 1);
                InstallmentDetail? iDetail = await psuLoan.GetInstallmentDetailByContractId(data.ContractId, payment.InstallmentNo);

                if (iDetail != null)
                {
                    payment.PrincipleAmount = iDetail.PrincipleAmount;
                    payment.InterestAmont = iDetail.InterestAmont;
                    payment.TotalAmount = iDetail.PrincipleAmount + iDetail.InterestAmont;
                }

                payment.BalanceAmount = loanTotalAmount;
                SelectRequest.Add(payment);
                PaymentConfirm = true;

                SelectRequestId.Add(data.LoanRequestId);
            }
            catch (Exception ex)
            {
                await notificationService.Error(notificationService.ExceptionLog(ex));
            }
        }

        private PaymentTransaction SetTransaction(VLoanRequestContract data, decimal? PaymentNumInstallments, decimal Number)
        {
            PaymentTransaction payment = new()
            {
                ContractId = data.ContractId,
                InstallmentNo = PaymentNumInstallments + Number,
                PrincipleAmount = 0,
                InterestAmont = 0,
                TotalAmount = 0,
                ContractNo = data.ContractNo,
                BalanceAmount = data.ContractLoanTotalAmount
            };

            return payment;
        }

        private async Task HandleOk()
        {
            try
            {
                var checkVal = SelectRequest
                    .Where(c => c.TotalAmount == null || c.TotalAmount == 0)
                    .FirstOrDefault();

                if (checkVal != null)
                {
                    _ = Task.Run(() => { notificationService.WarningDefult("ไม่สามารถทำรายการได้ เนื่องจากพบรายการที่ ยอดรวมหักเท่ากับ 0 บาท"); });
                    return;
                }

                if (DateValueCheck == null)
                {
                    _ = Task.Run(() => { notificationService.WarningDefult("กรุณาระบุ วันที่ชำระเงิน"); });
                    return;
                }

                if (BalanceAmountTemp == null)
                {
                    _ = Task.Run(() => { notificationService.WarningDefult("กรุณาตรวจสอบ ยอดเงินต้นคงเหลือ"); });
                    return;
                }
                else if (BalanceAmountTemp <= 0)
                {
                    bool isCancel = await ModalService.ConfirmAsync(new ConfirmOptions()
                    {
                        Title = "ยืนยันการบันทึกข้อมูลชำระเงินและปิด",
                        Content = $"ยืนยันการบันทึกข้อมูลชำระเงิน",
                        //CancelButtonProps =
                        //{
                        //    ChildContent = CancelButtonRender,
                        //    Type = ButtonType.Link,
                        //},
                        //OkButtonProps =
                        //{
                        //    ChildContent = OkButtonRender,
                        //    Type = ButtonType.Link,
                        //}
                    });

                    if (!isCancel)
                    {
                        return;
                    }
                }

                _loading = true;
                await Task.Delay(1);
                StateHasChanged();

                bool reloadPage = await ConfirmPageAsync(SelectRequest, DateValueCheck!.Value);

                if (reloadPage)
                {
                    LoanPaymentResult();
                    await DataTable();

                    _loading = false;
                    await Task.Delay(1);
                    StateHasChanged();

                    PaymentConfirm = false;
                    StateHasChanged();

                    _ = Task.Run(() => notificationService.SuccessDefult("บันทึกชำระเงิน สำเร็จ"));

                    var url = navigationManager.Uri.Split(navigationManager.BaseUri);
                    if (url.Length == 2)
                    {
                        navigationManager.NavigateTo(url[1], true);
                    }
                }
                else
                {
                    _loading = false;
                    await Task.Delay(1);
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                _loading = false;
                PaymentConfirm = false;
                _ = Task.Run(() => notificationService.Error(notificationService.ExceptionLog(ex)));
            }
        }

        private void HandleCancel()
        {
            PaymentConfirm = false;
        }

        private void GetDate(DateTime? date)
        {
            DateValueCheck = date;
        }

        /// <summary>
        /// รวมชำระ
        /// </summary>
        /// <param name="PrincipleAmount">เงินต้น</param>
        /// <param name="InterestAmont">ดอกเบี้ย</param>
        /// <returns></returns>
        private decimal? ResultTotalAmount(decimal? PrincipleAmount, decimal? InterestAmont)
        {
            decimal? result = PrincipleAmount + InterestAmont;
            return result == null ? 0 : result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listTransaction"></param>
        /// <param name="paidDate"></param>
        /// <returns>true = reload page || false = not reload page</returns>
        private async Task<bool> ConfirmPageAsync(List<PaymentTransaction> listTransaction, DateTime paidDate)
        {
            bool isSuccess = false;
            try
            {
                foreach (var transaction in listTransaction)
                {
                    PaymentTransaction? payment = await psuLoan.GetPaymentTransactionByInstallmentNo(transaction.InstallmentNo, transaction.ContractId);

                    if (payment == null)
                    {
                        var getReq = await psuLoan.GetVLoanRequestContractByContractId(transaction.ContractId);
                        if (getReq != null)
                        {
                            await SaveToDBAsync(paidDate, transaction, getReq);
                            isSuccess = true;
                        }
                    }
                    else
                    {
                        ContractMain? contract = await psuLoan.GeContractMainByContractIdAsync(transaction.ContractId);

                        var mess = $"พบข้อมูลงวดที่ {transaction.InstallmentNo} ของคุณ{userService.GetFullNameNoTitleName(contract?.DebtorStaffId)} แล้วจึงไม่สามารถเพิ่มข้อมูลดังกล่าวได้";
                        _ = Task.Run(() => { notificationService.Warning(mess, "แจ้งเตือน", false); });
                        isSuccess = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
            }
            return isSuccess;
        }

        private async Task SaveToDBAsync(DateTime PaidDate, PaymentTransaction transaction, VLoanRequestContract reqCon)
        {
            if (reqCon.ContractId == null)
            {
                return;
            }
            try
            {
                ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(reqCon.ContractId);

                /// bugData (ImportPayment) การเช็คการ สิ้นสุดสัญญา เข็คจาก BalanceAmount == 0
                /// bugData (PaymentTable) รอสอบถาม การเช็คการ สิ้นสุดสัญญา โดยจะเช็คจาก ??? แต่ตแนนี้ใส่ เข็คจาก งวด ไปก่อน
                //decimal? BalanceAmount = transaction.BalanceAmount - transaction.TotalAmount;
                decimal? BalanceAmount = BalanceAmountTemp;
                bool IsLast = false;

                #region เช็คจากงวด
                if (main != null)
                {
                    if (main.LoanNumInstallments == transaction.InstallmentNo)
                    {
                        decimal? ContractStatusId = main.ContractStatusId;
                        main.ContractStatusId = 99;
                        IsLast = true;

                        await psuLoan.UpdateContractMain(main);
                        await SaveToHistoryAsync(main, ContractStatusId);
                    }
                    // Ex 10 (จำนวนงวดที่ข้อมา) < 11 (จำนวนงวดที่ กรอกมาจากหน้าที่จะจ่าย)
                    else if (main.LoanNumInstallments < transaction.InstallmentNo)
                    {
                        string alert = $"กรุณาตรวจสอบงวดที่จะทำการจ่ายเงิน \n" +
                            $"ที่กรอกมาจากหน้าที่จ่ายคืองวด = {transaction.InstallmentNo} \n" +
                            $"ตอนข้อกู้ {main.LoanNumInstallments} งวด \n" +
                            $"ดังนั้นจึงทำรายการไม่ได้";
                        await JS.InvokeVoidAsync("displayTickerAlert", alert);
                        return;
                    }
                }
                #endregion

                transaction.BalanceAmount = BalanceAmount;
                transaction.PayDate = PaidDate;
                transaction.CreatedBy = StateProvider?.CurrentUser.StaffId;
                transaction.CreatedDate = DateTime.Now;

                await psuLoan.AddPaymentTransaction(transaction);

                await SetDataBySentEmailAsync(reqCon, transaction, IsLast);
            }
            catch (Exception ex)
            {
                await notificationService.Error(notificationService.ExceptionLog(ex));
            }
        }

        private async Task SaveToHistoryAsync(ContractMain contract, decimal? contractStatusId)
        {
            decimal? LoanStatusId = contractStatusId;
            string ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

            await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, LoanStatusId, ModifyBy);
        }

        private async Task SetDataBySentEmailAsync(VLoanRequestContract reqCon, PaymentTransaction transaction, bool isLast)
        {
            try
            {
                var StaffDetail = await psuLoan.GetUserDetailAsync(reqCon.DebtorStaffId);
                var DebtorName = userService.GetFullNameNoTitleName(reqCon.DebtorStaffId);

                var GuarantorDetail = await psuLoan.GetUserDetailAsync(reqCon.ContractGuarantorStaffId);
                var GuarantoName = userService.GetFullNameNoTitleName(reqCon.ContractGuarantorStaffId);

                ApplyLoanModel loan = new()
                {
                    LoanTypeID = reqCon.LoanTypeId,
                    LoanAmount = reqCon.ContractLoanAmount != null ? reqCon.ContractLoanAmount.Value : 0
                };

                #region ผู้กู้
                if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
                {
                    var Email = string.Empty;
                    var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                    Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                    var email = MailService.MailDebtorByAdminImportPayment(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction,
                        isLast);
                    MailService.SendEmail(email);
                }
                #endregion

                #region ผู้ค้ำ
                if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
                {
                    var Email = string.Empty;
                    var Name = userService.GetFullName(GuarantorDetail.StaffId);
                    Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                    var email = MailService.MailDebtorByAdminImportPayment(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction,
                        isLast);
                    MailService.SendEmail(email);
                }
                #endregion
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async void OnSelectedItemChangedHandler(LoanType value)
        {
            await DataTable(SearchVal, value.LoanTypeId);
        }

        private async Task TemplateDowload()
        {
            string fileName = "ตัวอย่างไฟล์_บันทึกชำระเงินกู้ยืม.xlsx";
            string rootUrl = SaveFileAndImgService.GetFullPhysicalFilePathDir();
            string fileTemplate = $"{rootUrl}\\{Utility.TEMPLATE_DIR}\\{fileName}";

            bool isnoFile = File.Exists(fileTemplate);

            if (isnoFile)
            {
                try
                {
                    using (var stream = new FileStream(fileTemplate, FileMode.Open))
                    {
                        MemoryStream ms = new();
                        await stream.CopyToAsync(ms);
                        byte[] bytes = ms.ToArray();
                        ms.Close();
                        stream.Close();

                        await SaveFileAndImgService.SaveFileAsPath(bytes, Path.GetFileName(fileTemplate));
                    }
                }
                catch (IOException io)
                {
                    await notificationService.Error(notificationService.ExceptionLog(io));
                }
                catch (Exception ex)
                {
                    await notificationService.Error(notificationService.ExceptionLog(ex));
                }
            }
            else
            {
                await notificationService.WarningDefult("รอเปิดใช้งาน");
            }
        }

        private async Task OpenTransactionHistory(PaymentTransaction transaction)
        {
            try
            {
                TransactionListHis = await psuLoan.GetAllPaymentTransactionByContractNo(transaction.ContractNo);

                if (!TransactionListHis.Any())
                {
                    _ = Task.Run(() => { notificationService.WarningDefult("ไม่พบประวัติการชำระเงิน"); });
                    return;
                }

                if (!Layout.collapseNavMenu)
                {
                    Layout.ToggleNavMenu((IsMobile ? false : true));
                }

                VisibleTransaction = true;
            }
            catch (Exception ex)
            {
                await notificationService.Error(notificationService.ExceptionLog(ex));
            }
        }

        private async Task CancelTransactionHistory()
        {
            try
            {
                if (Layout.collapseNavMenu)
                {
                    Layout.ToggleNavMenu(false);
                }

                VisibleTransaction = false;
            }
            catch (Exception ex)
            {
                await notificationService.Error(notificationService.ExceptionLog(ex));
            }
        }

    }
}
