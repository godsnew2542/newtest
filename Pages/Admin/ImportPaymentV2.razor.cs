using ClosedXML.Excel;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Globalization;

namespace LoanApp.Pages.Admin;

public partial class ImportPaymentV2
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #endregion

    [Parameter] public string? EncryptData { get; set; } = null;

    #region Inject
    [Inject] private Services.IServices.IImportPaymentService importPaymentService { get; set; } = null!;
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;

    #endregion

    private StorageImportPaymentCSV RecordPayment { get; set; } = new();
    private SaveImportPaymentServiceModel PaymentServiceModel { get; set; } = new();
    private List<ImportRecordPaymentModel> PaymentFailTemp { get; set; } = new();
    private List<ImportRecordPaymentModel> PaymentSuccessTemp { get; set; } = new();
    private List<PaymentTransaction> HisPaymentTransactions { get; set; } = new();
    private UploadModel? modelUploadCSV { get; set; } = null;

    private string StorageName { get; } = "ImportPaymentCSV";
    private bool IsLoading { get; set; } = true;
    private int ReadCountNoExcel { get; set; } = 0;
    bool _switchValue { get; set; } = false;
    bool LoadingPaymentFail { get; set; } = false;

    /// <summary>
    /// เกิดข้อผิดพลาดในการอ่านไฟล์หรือไหม [defult ไม่ = false]
    /// ค่าที่ check
    /// 1. สัญญาเงินกู้ != null || ""
    /// 2. จำนวน column >= 14
    /// </summary>
    private bool FileImportError { get; set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            List<ImportRecordPaymentModel> importRecords = new();
            try
            {
                SaveFileAndImgService.AutoDeleteFileInFolderTemp();

                UploadModel checkData = await sessionStorage.GetItemAsync<UploadModel>(StorageName);

                if (checkData != null)
                {
                    modelUploadCSV = checkData;
                    var FileName = modelUploadCSV.TempImgName!.Split(".");
                    RecordPayment.FileName = FileName[0];

                    importRecords = ReadExcel(modelUploadCSV.Url);
                }

                if (importRecords.Any() && !FileImportError)
                {
                    await CheckDataImportRecordPaymentAsync(importRecords);
                }

                PaymentFailTemp = RecordPayment.PaymentFail;
                PaymentSuccessTemp = RecordPayment.PaymentSuccess;
                IsLoading = false;


                if (!string.IsNullOrEmpty(EncryptData))
                {
                    await decryptQuery(EncryptData);
                }

                await autoCheckPaymentTransactionData(RecordPayment.PaymentFail);

                StateHasChanged();
            }
            catch (Exception ex)
            {
                IsLoading = false;
                StateHasChanged();
                Console.WriteLine(ex.Message);
                await Error.ProcessError(ex, $"Data was not correct at row  {ReadCountNoExcel} ,Please check!");
                throw;
            }
        }
    }

    private async Task decryptQuery(string encryptVal)
    {
        string? queryStr = Utility.DecryptQueryString(encryptVal);

        if (queryStr == null)
        {
            return;
        }

        string[] splitStr = queryStr.Split("&");

        if (splitStr.Length == 0)
        {
            return;
        }

        foreach (var item in splitStr)
        {
            string[] splitData = item.Split("=");

            if (splitData.Length == 2)
            {
                string val = splitData[1];

                if (splitData[0].ToLower() == "switchcheckpaymenttransactiondata")
                {
                    bool data = bool.Parse(val);

                    if (data)
                    {
                        await ClickSwitch();
                    }
                }
            }
        }
    }

    private List<ImportRecordPaymentModel> ReadExcel(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new List<ImportRecordPaymentModel>();
        }

        try
        {
            List<ImportRecordPaymentModel> importRecords = new();

            using (var workBook = new XLWorkbook(path))
            {
                var workSheet = workBook.Worksheet(1);
                var firstRowUsed = workSheet.FirstRowUsed();
                var firstPossibleAddress = workSheet.Row(firstRowUsed.RowNumber()).FirstCell().Address;
                var lastPossibleAddress = workSheet.LastCellUsed().Address;
                var range = workSheet.Range(firstPossibleAddress, lastPossibleAddress).AsRange();

                List<string[]> excelRows = range.RowsUsed()
                     .Skip(1)
                     .Select(row => row.Cells().Select(cell => cell.Value.ToString())
                     .ToArray())
                     .ToList();
                try
                {
                    foreach (var item in excelRows.Select((value, i) => new { i, value }))
                    {
                        var value = item.value;
                        var index = item.i;
                        ReadCountNoExcel = (index + 1);

                        if (value == null || value.Length < 14)
                        {
                            FileImportError = true;
                            break;
                        }
                        else if (string.IsNullOrEmpty(value[1]))
                        {
                            FileImportError = true;
                            break;
                        }

                        ImportRecordPaymentModel ExtractData = ExtractDataFromExcel(value);
                        importRecords.Add(ExtractData);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return importRecords;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task CheckDataImportRecordPaymentAsync(List<ImportRecordPaymentModel> importRecords)
    {
        string ImportBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

        try
        {
            List<string> contractNoFromFile = importRecords.Select(c => c.ContractNo!).ToList();
            List<ContractMain> contractMains = await psuLoan.GetListContractMainByListContractNo(contractNoFromFile);
            List<string?> contractNoFromDB = contractMains.Select(c => c.ContractNo).ToList();

            #region ไม่พบใน DB 
            RecordPayment.PaymentFail = importRecords
                .Where(c => !contractNoFromDB!.Contains(c.ContractNo))
                .ToList();
            #endregion 

            if (contractMains.Any())
            {
                #region เช็คว่าใน DB เป็นสิ้นสุดสัญาหรือยัง
                var isLoanSuccess = contractMains
                    .Where(c => c.ContractStatusId == 99)
                    .ToList();

                List<string?> contractNoFromIsLoanSuccess = isLoanSuccess
                    .Select(c => c.ContractNo)
                    .ToList();

                if (isLoanSuccess.Any())
                {
                    var data = importRecords
                        .Where(c => contractNoFromIsLoanSuccess!.Contains(c.ContractNo))
                        .ToList();

                    data.ForEach(x => x.Remark = "สัญญาสิ้นสุดแล้ว");

                    foreach (var item in data)
                    {
                        RecordPayment.PaymentFail.Add(item);
                    }
                }

                #endregion

                var iPayments = importRecords
                    .Where(c => !contractNoFromIsLoanSuccess.Contains(c.ContractNo))
                    .Where(c => contractNoFromDB.Contains(c.ContractNo))
                    .ToList();

                if (iPayments.Any())
                {
                    foreach (var importPayment in iPayments)
                    {
                        #region เช็คว่าเคยบันทึกลง db หรือไหม เช็คจาก payDate เฉพาะ import file
                        var isHaveDataFromDb = await psuLoan.CheckTransactionFromPayDate(importPayment.ContractNo, importPayment.PayDate);
                        #endregion

                        if (isHaveDataFromDb == false)
                        {
                            PaymentTransaction? isPaymentTransaction = await importPaymentService.CheckPaymentTransactionAgainAsync(importPayment.ContractNo!, importPayment.InstallmentNo, importPayment.PayDate);

                            ImportPaidLoan? isImportPaidLoan = await importPaymentService.CheckImportPaidLoanAgainAsync(importPayment.ContractNo!, importPayment.InstallmentNo, importPayment.PayDate);

                            importPayment.IsPaymentTransaction = (isPaymentTransaction == null ? false : true);
                            importPayment.IsImportPaidLoan = (isImportPaidLoan == null ? false : true);

                            if (importPayment.IsPaymentTransaction)
                            {
                                importPayment.IsAlready = importPayment.IsPaymentTransaction;
                                RecordPayment.PaymentFail.Add(importPayment);
                            }
                            else
                            {
                                ContractMain? cMain = contractMains
                                    .Where(c => c.ContractNo == importPayment.ContractNo)
                                    .FirstOrDefault();

                                if (importPayment.BalanceAmount == 0)
                                {
                                    importPayment.IsFinalInstallmentByBalanceAmount = true;
                                }

                                if (cMain != null)
                                {
                                    importPayment.saveImportPayment = await CheckFullDataFromPaymentSuccessAsync(importPayment,
                                        ImportBy,
                                        cMain);
                                }

                                RecordPayment.PaymentSuccess.Add(importPayment);
                            }
                        }
                        else
                        {
                            importPayment.IsAlready = true;
                            RecordPayment.PaymentFail.Add(importPayment);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// เช็คว่าเคยมี งวดนี้หรือไหม แค่งวด ไม่ได้ลงถึงวัน
    /// </summary>
    /// <param name="ele"></param>
    /// <param name="ImportBy"></param>
    /// <param name="contractMain"></param>
    /// <returns></returns>
    private async Task<SaveImportPaymentServiceModel> CheckFullDataFromPaymentSuccessAsync(ImportRecordPaymentModel ele, string ImportBy, ContractMain contractMain)
    {
        SaveImportPaymentServiceModel saveImport = new();

        var BalanceAmount = ele.BalanceAmount;
        try
        {
            if (!ele.IsImportPaidLoan)
            {
                saveImport.ImportPaid =
                    importPaymentService.SetTableImportPaidLoan(ele, BalanceAmount, ImportBy);
            }

            if (!ele.IsPaymentTransaction)
            {
                SaveImportPaymentServiceModel SetPaymentTransaction =
                    await importPaymentService.SetTablePaymentTransactionAsync(ele, BalanceAmount, contractMain);

                saveImport.IsSaveTablePaymentTransaction = true;
                saveImport.Transaction = SetPaymentTransaction.Transaction;

                // ?? น่าจะ check ว่าเคยบันทึกงวดนี้ไปแล้วยัง
                // เช่นไปปิดยอดหน้าเคอร์เตอร์ แล้วกองคลังส่งมา ที่เป็นงวดซ้ำ จะทำการ +1 งวด
                if (SetPaymentTransaction.ChangeData != null)
                {
                    if (saveImport.ChangeData == null)
                    {
                        saveImport.ChangeData = new();
                    }

                    saveImport.ChangeData.NewInstallmentNo = SetPaymentTransaction.ChangeData.NewInstallmentNo;

                    if (saveImport.ImportPaid != null)
                    {
                        saveImport.ImportPaid.ReferenceId1 = saveImport.ChangeData.NewInstallmentNo?.Remark;
                    }
                }
            }
            else
            {
                // กรณีพบใน Table PaymentTransactions => Get val ล่าสุดมาเลย
                saveImport.Transaction = await importPaymentService.GetPaymentTransactionByDataAsync(ele);
            }

            ContractMain? Contract = null;
            VLoanRequestContract? reqCon = null;

            if (PaymentServiceModel.Transaction != null)
            {
                Contract = await importPaymentService
                    .GetContractMainByContractIdAsync(PaymentServiceModel.Transaction.ContractId);

                if (Contract != null)
                {
                    reqCon = userService.GetVLoanRequestContract(Contract.LoanRequestId);
                }
            }

            // TODO (ImportPayment) การเช็คการ สิ้นสุดสัญญา เข็คจาก BalanceAmount == 0
            if (ele.IsFinalInstallmentByBalanceAmount)
            {
                PaymentServiceModel.HisContractMain =
                    await importPaymentService.SetHisContractMainAsync(ele, BalanceAmount, StateProvider?.CurrentUser.StaffId);
                saveImport.LoanIsLast = true;
            }
            else
            {
                var paymentTransaction = await importPaymentService.GetLastInstallmentNoFromPaymentTransaction(ele);

                if (paymentTransaction != null)
                {
                    var resultPay = paymentTransaction.BalanceAmount - ele.TotalAmount;

                    // อาจจะ มีค่าน้อยกว่า  0
                    if (resultPay <= 0)
                    {
                        PaymentServiceModel.HisContractMain =
                        await importPaymentService.SetHisContractMainAsync(ele, resultPay, StateProvider?.CurrentUser.StaffId);
                        saveImport.LoanIsLast = true;

                        if (saveImport.ChangeData == null)
                        {
                            saveImport.ChangeData = new();
                        }

                        saveImport.ChangeData.NewBalanceAmount = new()
                        {
                            NewData = resultPay,
                            OldData = BalanceAmount,
                            Remark = $"From {BalanceAmount} To {resultPay}"
                        };

                        if (saveImport.ImportPaid != null &&
                            saveImport.ChangeData.NewBalanceAmount != null)
                        {
                            saveImport.ImportPaid.ReferenceId2 = saveImport.ChangeData.NewBalanceAmount.Remark;
                        }
                    }
                }

            }

            return saveImport;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private ImportRecordPaymentModel ExtractDataFromExcel(string[] row)
    {
        try
        {
            DateTimeFormatInfo TH = new CultureInfo(Utility.DateLanguage_TH, false).DateTimeFormat;
            ImportRecordPaymentModel ImportPayment = new();

            #region คก.
            var PsuProject = row[0];
            ImportPayment.PsuProject = (PsuProject != null &&
                !string.IsNullOrEmpty(PsuProject.ToString()) &&
                !string.IsNullOrWhiteSpace(PsuProject.ToString()) ?
                PsuProject.ToString() : string.Empty);

            #endregion

            #region สัญญาเงินกู้
            var ContractNo = row[1];
            ImportPayment.ContractNo = (ContractNo != null &&
                !string.IsNullOrEmpty(ContractNo.ToString()) &&
                !string.IsNullOrWhiteSpace(ContractNo.ToString()) ?
                ContractNo.ToString() : string.Empty);

            #endregion

            #region รหัสบุคลากร => {รหัสเงินเดือน}
            var StaffSalaryId = row[2];
            ImportPayment.StaffSalaryId = (StaffSalaryId != null &&
                !string.IsNullOrEmpty(StaffSalaryId.ToString()) &&
                !string.IsNullOrWhiteSpace(StaffSalaryId.ToString()) ?
                StaffSalaryId.ToString() : string.Empty);

            #endregion

            #region ชื่อ
            var Fname = row[3];
            ImportPayment.Fname = (Fname != null &&
                !string.IsNullOrEmpty(Fname.ToString()) &&
                !string.IsNullOrWhiteSpace(Fname.ToString()) ?
                Fname.ToString() : string.Empty);

            #endregion

            #region สกุล
            var Lname = row[4];
            ImportPayment.Lname = (Lname != null &&
                !string.IsNullOrEmpty(Lname.ToString()) &&
                !string.IsNullOrWhiteSpace(Lname.ToString()) ?
                Lname.ToString() : string.Empty);

            #endregion

            #region ประเภทยืม
            var LoanParentName = row[5];
            ImportPayment.LoanParentName = (LoanParentName != null &&
                !string.IsNullOrEmpty(LoanParentName.ToString()) &&
                !string.IsNullOrWhiteSpace(LoanParentName.ToString()) ?
                LoanParentName.ToString() : string.Empty);

            #endregion

            #region ยอดคงเหลือ == "0"  สิ้นสุดสัญญา
            var BalanceAmount = row[6];
            if (!string.IsNullOrEmpty(BalanceAmount))
            {
                if (BalanceAmount.ToString() == "0")
                {
                    ImportPayment.BalanceAmount = 0m;
                }
                else if (!string.IsNullOrWhiteSpace(BalanceAmount))
                {
                    ImportPayment.BalanceAmount = Convert.ToDecimal(BalanceAmount.ToString());
                }
            }
            else
            {
                ImportPayment.BalanceAmount = null;
            }

            #endregion

            #region วันที่รายงาน
            var PayDate = row[7];
            ImportPayment.PayDate = (PayDate != null &&
                !string.IsNullOrEmpty(PayDate.ToString()) &&
                !string.IsNullOrWhiteSpace(PayDate.ToString()) ?
                Convert.ToDateTime(PayDate.ToString(), TH) : null);

            #endregion

            #region จำนวนเงินยืม
            var LoanAmount = row[8];
            ImportPayment.LoanAmount = (LoanAmount != null &&
                !string.IsNullOrEmpty(LoanAmount.ToString()) &&
                !string.IsNullOrWhiteSpace(LoanAmount.ToString()) ?
                Convert.ToDecimal(LoanAmount.ToString()) : null);

            #endregion

            #region จำนวนงวด
            var LoanNumInstallments = row[9];
            ImportPayment.LoanNumInstallments = (LoanNumInstallments != null &&
                !string.IsNullOrEmpty(LoanNumInstallments.ToString()) &&
                !string.IsNullOrWhiteSpace(LoanNumInstallments.ToString()) ?
                Convert.ToDecimal(LoanNumInstallments.ToString()) : null);

            #endregion

            #region งวดที่จ่าย
            var InstallmentNo = row[10];
            ImportPayment.InstallmentNo = (InstallmentNo != null &&
                !string.IsNullOrEmpty(InstallmentNo.ToString()) &&
                !string.IsNullOrWhiteSpace(InstallmentNo.ToString()) ?
                Convert.ToDecimal(InstallmentNo.ToString()) : null);

            #endregion

            #region เงินต้น
            var PrincipleAmount = row[11];
            ImportPayment.PrincipleAmount = (PrincipleAmount != null &&
                !string.IsNullOrEmpty(PrincipleAmount.ToString()) &&
                !string.IsNullOrWhiteSpace(PrincipleAmount.ToString()) ?
                Convert.ToDecimal(PrincipleAmount.ToString()) : null);

            #endregion

            #region ดอกเบี้ย
            var InterestAmont = row[12];
            ImportPayment.InterestAmont = (InterestAmont != null &&
                !string.IsNullOrEmpty(InterestAmont.ToString()) &&
                !string.IsNullOrWhiteSpace(InterestAmont.ToString()) ?
                Convert.ToDecimal(InterestAmont.ToString()) : null);

            #endregion

            #region รวมหัก
            var TotalAmount = row[13];
            ImportPayment.TotalAmount = (TotalAmount != null &&
                !string.IsNullOrEmpty(TotalAmount.ToString()) &&
                !string.IsNullOrWhiteSpace(TotalAmount.ToString()) ?
                Convert.ToDecimal(TotalAmount.ToString()) : null);

            #endregion

            return ImportPayment;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private async Task ConfirmDataAsync(StorageImportPaymentCSV data)
    {
        IsLoading = true;
        try
        {
            await Task.Delay(1);
            await SaveToDbV2Async(data.PaymentSuccess);
            await ExportToExcelAsync(data.PaymentFail);

            IsLoading = false;
            _ = Task.Run(() => notificationService.SuccessDefult("นำเข้าข้อมูลชำระเงินสำเร็จ"));

            navigationManager.NavigateTo("/Admin/ImportPaymentV2", true);
            //back()
        }
        catch (Exception ex)
        {
            IsLoading = false;
            await Error.ProcessError(ex, $"{ReadCountNoExcel} ,Please check!");

        }
    }

    private async Task SaveToDbV2Async(List<ImportRecordPaymentModel> paymentSuccess)
    {
        if (paymentSuccess.Any())
        {
            foreach (var ele in paymentSuccess)
            {
                if (ele.saveImportPayment != null)
                {
                    SaveImportPaymentServiceModel saveImport = ele.saveImportPayment;

                    try
                    {
                        #region Add ข้อมูลเพิ่มเติมใน PaymentTransaction
                        if (saveImport.Transaction != null)
                        {
                            saveImport.Transaction.CreatedBy = StateProvider?.CurrentUser.StaffId;
                            saveImport.Transaction.CreatedDate = DateTime.Now;
                        }

                        #endregion


                        /// Save Data
                        var SaveData = await importPaymentService.AddDataByImportPaymentAsync(saveImport, StateProvider?.CurrentUser.StaffId);
                        //var SaveData = true;

                        if (SaveData)
                        {
                            VLoanRequestContract? reqCon = await psuLoan.GetVLoanRequestContractByContractId(saveImport.Transaction?.ContractId);

                            // TODO sent mail
                            if (reqCon != null && saveImport.Transaction != null)
                            {
                                await SetDataBySentEmail(reqCon, saveImport.Transaction, saveImport.LoanIsLast);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception ex2 = new Exception($"มีข้อผิดพลาด เลขที่สัญญาที่{ele.ContractNo} ", ex);
                        throw ex2;
                    }
                }
            }
        }
    }

    private async Task SetDataBySentEmail(VLoanRequestContract ReqCon, PaymentTransaction transaction, bool IsLast)
    {
        try
        {
            if (transaction != null && transaction.ContractId != null)
            {
                var StaffDetail = await psuLoan.GetUserDetailAsync(ReqCon.DebtorStaffId);
                var DebtorName = string.Empty;

                if (StaffDetail != null)
                {
                    DebtorName = $"{StaffDetail.StaffNameThai} {StaffDetail.StaffSnameThai}";
                }

                var GuarantorDetail = await psuLoan.GetUserDetailAsync(ReqCon.ContractGuarantorStaffId);
                var GuarantoName = string.Empty;

                if (GuarantorDetail != null)
                {
                    GuarantoName = $"{GuarantorDetail.StaffNameThai} {GuarantorDetail.StaffSnameThai}";
                }

                ApplyLoanModel loan = new()
                {
                    LoanTypeID = ReqCon.LoanTypeId,
                    LoanAmount = (ReqCon.ContractLoanAmount == null ? 0 : ReqCon.ContractLoanAmount.Value),
                    LoanInterest = ReqCon.ContractLoanNumInstallments
                };

                #region ผู้กู้
                if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
                {
                    var Email = string.Empty;
                    var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                    Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                    var email = MailService.MailDebtorByAdminImportPayment(
                        Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction,
                        IsLast);
                    MailService.SendEmail(email);
                }
                #endregion

                #region ผู้ค้ำ
                if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
                {
                    var Email = string.Empty;
                    var Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);
                    Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                    var email = MailService.MailDebtorByAdminImportPayment(
                        Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction,
                        IsLast);
                    MailService.SendEmail(email);
                }
                #endregion
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task ExportToExcelAsync(List<ImportRecordPaymentModel> PaymentFail)
    {
        byte[] bytes;
        try
        {
            if (PaymentFail.Any())
            {
                // new Version
                var wb = new XLWorkbook();
                int row = 1;
                var ws = wb.Worksheets.Add("Sheet1");

                #region Table Header
                ws.Cell(row, 1).Value = "คก.";
                ws.Cell(row, 2).Value = "สัญญาเงินกู้";
                ws.Cell(row, 3).Value = "รหัสเงินเดือน";
                ws.Cell(row, 4).Value = "ชื่อ";
                ws.Cell(row, 5).Value = "สกุล";
                ws.Cell(row, 6).Value = "ประเภทยืม";
                ws.Cell(row, 7).Value = "ยอดคงเหลือ";
                ws.Cell(row, 8).Value = "วันที่รายงาน";
                ws.Cell(row, 9).Value = "จำนวนเงินยืม";
                ws.Cell(row, 10).Value = "จำนวนงวด";
                ws.Cell(row, 11).Value = "งวดที่จ่าย";
                ws.Cell(row, 12).Value = "เงินต้น";
                ws.Cell(row, 13).Value = "ดอกเบี้ย";
                ws.Cell(row, 14).Value = "รวมหัก";
                ws.Cell(row, 15).Value = "หมายเหตุ";
                #endregion

                #region Table Boby
                if (PaymentFail.Any())
                {
                    foreach (var info in PaymentFail)
                    {
                        row++;
                        ws.Cell(row, 1).Value = info.PsuProject;
                        ws.Cell(row, 2).Value = info.ContractNo;
                        ws.Cell(row, 3).Value = info.StaffSalaryId;
                        ws.Cell(row, 4).Value = info.Fname;
                        ws.Cell(row, 5).Value = info.Lname;
                        ws.Cell(row, 6).Value = info.LoanParentName;
                        ws.Cell(row, 7).Value = (info.BalanceAmount == null ? 0 : (double)info.BalanceAmount);
                        ws.Cell(row, 8).Value = dateService.ChangeDate(dateService.ConvertToDateTime(info.PayDate), "dd-MM-yyyy", Utility.DateLanguage_TH);
                        ws.Cell(row, 9).Value = (info.LoanAmount == null ? 0 : (double)info.LoanAmount);
                        ws.Cell(row, 10).Value = (info.LoanNumInstallments == null ? 0 : (double)info.LoanNumInstallments);
                        ws.Cell(row, 11).Value = (info.InstallmentNo == null ? 0 : (double)info.InstallmentNo);
                        ws.Cell(row, 12).Value = (info.PrincipleAmount == null ? 0 : (double)info.PrincipleAmount);
                        ws.Cell(row, 13).Value = (info.InterestAmont == null ? 0 : (double)info.InterestAmont);
                        ws.Cell(row, 14).Value = (info.TotalAmount == null ? 0 : (double)info.TotalAmount);
                        ws.Cell(row, 15).Value = ($"{(info.IsAlready ? "รายการนี้ถูกนำเข้าแล้ว" : "พบข้อผิดพลาด")}" + $"{(!string.IsNullOrEmpty(info.Remark) ? info.Remark : null)}");
                    }
                }
                #endregion

                MemoryStream XLSStream = new();
                wb.SaveAs(XLSStream);

                bytes = XLSStream.ToArray();
                XLSStream.Close();
                //var path = "data:application/vnd.ms-excel;base64," + Convert.ToBase64String(bytes);
                //await JS.InvokeVoidAsync("jsSaveAsFileFromPath", RecordPayment.FileName, path);

                await SaveFileAndImgService.SaveFileAsPath(bytes, RecordPayment.FileName, "data:application/vnd.ms-excel;base64,");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async void OnFailSearch(string? val)
    {
        if (string.IsNullOrEmpty(val))
        {
            PaymentFailTemp = RecordPayment.PaymentFail;
            return;
        }
        else if ((val.Trim()).Length < Utility.SearchMinlength)
        {
            PaymentFailTemp = RecordPayment.PaymentFail;
            await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา เลขที่สัญญา/ชื่อ-สกุล อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
            return;
        }

        val = val.Trim();
        //PaymentFailTemp = RecordPayment.PaymentFail
        //    .Where(c => (c.ContractNo!.Contains(val)) ||
        //    (c.Fname!.ToLower().Contains(val.ToLower()) ||
        //    c.Lname!.ToLower().Contains(val.ToLower()) ||
        //    $"{c.Fname.ToLower()} {c.Lname.ToLower()}".Contains(val.ToLower())))
        //    .ToList()

        PaymentFailTemp = await OnFilterPaymentFail(val, _switchValue);

        StateHasChanged();
    }

    private async void OnSuccessSearch(string? val)
    {
        if (string.IsNullOrEmpty(val))
        {
            PaymentSuccessTemp = RecordPayment.PaymentSuccess;
            return;
        }
        else if (val.Length < 3)
        {
            PaymentSuccessTemp = RecordPayment.PaymentSuccess;
            await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา เลขที่สัญญา/ชื่อ-สกุล อย่างน้อย 3 ตัวอักษร");
            return;
        }

        PaymentSuccessTemp = RecordPayment.PaymentSuccess
            .Where(c => (c.ContractNo!.Contains(val)) ||
            (c.Fname!.ToLower().Contains(val.ToLower()) ||
            c.Lname!.ToLower().Contains(val.ToLower()) ||
            $"{c.Fname.ToLower()} {c.Lname.ToLower()}".Contains(val.ToLower())))
            .ToList();

        StateHasChanged();
    }

    private async Task DtlPayment(ImportRecordPaymentModel importRecord)
    {
        HisPaymentTransactions = new();
        try
        {

            HisPaymentTransactions = await psuLoan.GetAllPaymentTransactionByContractNo(importRecord.ContractNo);

            _visible = true;
        }
        catch (Exception ex)
        {
            await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
        }
    }

    private async Task<string?> GetDelHisTransactions(List<PaymentTransaction> payments)
    {
        string? result = null;

        try
        {
            PaymentTransaction payment = payments[0];

            VLoanRequestContract? requestContract = await psuLoan.GetVLoanRequestContractByContractId(payment.ContractId);

            if (requestContract != null)
            {
                FullNameModel? fullName = userService.GetNameForDebtor(requestContract);

                if (fullName != null)
                {
                    result = $"{fullName.FullNameTh} ";
                }

                result += $"ประเภทกู้ยืม: {requestContract?.LoanTypeName} " +
                    $"จำนวน: {requestContract?.ContractLoanNumInstallments} งวด " +
                    $"ยอดเงินกู้: " +
                    $"{string.Format("{0:n2}", (requestContract?.ContractLoanAmount != null ? requestContract?.ContractLoanAmount : 0))} บาท";
            }
        }
        catch (Exception ex)
        {
            await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
        }

        return result;
    }

    private void HandleOk(MouseEventArgs e)
    {
        _visible = false;
    }

    private void HandleCancel(MouseEventArgs e)
    {
        _visible = false;
    }

    private async Task<bool?> CheckPaymentTransactionData(ImportRecordPaymentModel data)
    {
        try
        {
            return await psuLoan.CheckTransactionFromPayDate(data.ContractNo, data.PayDate);
        }
        catch (Exception ex)
        {
            await notificationService.Error(notificationService.ExceptionLog(ex));
        }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns>true = มีเงินคงเหลือ ที่ยังไม่เป็น 0</returns>
    private async Task<bool> CheckPaymentTransactionBalanceAmount(ImportRecordPaymentModel data)
    {
        try
        {
            var transactionList = await psuLoan.GetAllPaymentTransactionByContractNo(data.ContractNo);

            if (transactionList.Any())
            {
                var transaction = transactionList
                    .Where(c => c.InstallmentNo == (transactionList.Max(x => x.InstallmentNo)))
                    .OrderBy(x => x.BalanceAmount)
                    .First();

                if (transaction.BalanceAmount > 0 && await CheckPaymentTransactionData(data) == false)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            await notificationService.Error(notificationService.ExceptionLog(ex));
        }
        return false;
    }

    private async Task AddPaymentTransactionData(ImportRecordPaymentModel data)
    {
        try
        {
            ContractMain? main = await psuLoan.GeContractMainByContractNo(data.ContractNo);

            if (main != null)
            {
                List<PaymentTransaction> val = await psuLoan.GetAllPaymentTransactionByContractId(main.ContractId);

                decimal? installmentNo = val.Max(x => x.InstallmentNo);
                installmentNo = installmentNo == null ? 1 : installmentNo.Value + 1;

                if (installmentNo > main.LoanNumInstallments!.Value)
                {
                    await notificationService.WarningDefult("ไม่สามารถทำรายการได้เนื่อจจาก จำนวนงวดเกิดจากที่ขอกู้");
                    return;
                }

                PaymentTransaction transaction = new()
                {
                    ContractId = main.ContractId,
                    InstallmentNo = installmentNo,
                    PayDate = data.PayDate,
                    PrincipleAmount = data.PrincipleAmount,
                    InterestAmont = data.InterestAmont,
                    TotalAmount = data.TotalAmount,
                    ContractNo = data.ContractNo,
                    CreatedBy = StateProvider?.CurrentUser.StaffId,
                    CreatedDate = DateTime.Now,
                    BalanceAmount = data.BalanceAmount,
                };

                await psuLoan.AddPaymentTransaction(transaction);

                var parameter = $"switchcheckpaymenttransactiondata={_switchValue}";

                navigationManager.NavigateTo($"/Admin/ImportPaymentV2/{Utility.EncryptQueryString(parameter)}", true);
            }
        }
        catch (Exception ex)
        {
            await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
        }
    }

    private async Task ClickSwitch()
    {
        LoadingPaymentFail = true;
        await Task.Delay(1);
        StateHasChanged();

        _switchValue = !_switchValue;
        string? text = !string.IsNullOrEmpty(FailSearch) ? FailSearch.Trim() : null;
        PaymentFailTemp = await OnFilterPaymentFail(text, _switchValue);

        LoadingPaymentFail = false;
        await Task.Delay(1);
        StateHasChanged();
    }

    private async Task<List<ImportRecordPaymentModel>> OnFilterPaymentFail(string? text, bool switchVal)
    {
        List<ImportRecordPaymentModel> imports = new();

        var p = RecordPayment.PaymentFail
            .Where(c => string.IsNullOrEmpty(text) ||
            ((c.ContractNo!.Contains(text)) || (c.Fname!.ToLower().Contains(text.ToLower()) || c.Lname!.ToLower().Contains(text.ToLower()) || $"{c.Fname.ToLower()} {c.Lname.ToLower()}".Contains(text.ToLower()))
            ))
            .ToList();

        if (switchVal)
        {
            foreach (var item in p)
            {
                bool? pp = await CheckPaymentTransactionData(item);

                if (pp == false)
                {
                    bool cPaymentTransaction = await CheckPaymentTransactionBalanceAmount(item);

                    if (cPaymentTransaction)
                    {
                        imports.Add(item);
                    }
                }
            }
        }
        else
        {
            imports = p;
        }
        return imports;
    }


    private void back()
    {
        navigationManager.NavigateTo("/Admin/NewRecordPayment");
    }

    /// <summary>
    /// Check data ที่ยังไม่ปรับสถานะ เป็น 99 Auto
    /// </summary>
    /// <param name="failData"></param>
    /// <returns></returns>
    private async Task autoCheckPaymentTransactionData(List<ImportRecordPaymentModel> failData)
    {
        List<ImportRecordPaymentModel> imports = new();

        try
        {
            foreach (var item in failData)
            {
                bool? pp = await CheckPaymentTransactionData(item);

                if (pp == false)
                {
                    bool cPaymentTransaction = await CheckPaymentTransactionBalanceAmount(item);

                    if (cPaymentTransaction)
                    {
                        imports.Add(item);
                    }
                }
            }

            if (imports.Any())
            {
                List<string> contractNos = imports
                    .Where(c => !string.IsNullOrEmpty(c.ContractNo))
                    .Select(c => c.ContractNo!)
                    .ToList();

                List<ContractMain> mains = await psuLoan.GetListContractMainByListContractNo(contractNos);

                if (mains.Any())
                {
                    List<PaymentTransaction> paymentTransactions = new();
                    foreach (var main in mains)
                    {
                        ImportRecordPaymentModel? data = imports.Where(c => c.ContractNo == main.ContractNo).FirstOrDefault();

                        if (data != null)
                        {
                            List<PaymentTransaction> val = await psuLoan.GetAllPaymentTransactionByContractId(main.ContractId);

                            decimal? installmentNo = val.Max(x => x.InstallmentNo);
                            installmentNo = installmentNo == null ? 1 : installmentNo.Value + 1;

                            if (installmentNo > main.LoanNumInstallments!.Value) { }
                            else
                            {
                                PaymentTransaction transaction = new()
                                {
                                    ContractId = main.ContractId,
                                    InstallmentNo = installmentNo,
                                    PayDate = data.PayDate,
                                    PrincipleAmount = data.PrincipleAmount,
                                    InterestAmont = data.InterestAmont,
                                    TotalAmount = data.TotalAmount,
                                    ContractNo = data.ContractNo,
                                    CreatedBy = StateProvider?.CurrentUser.StaffId,
                                    CreatedDate = DateTime.Now,
                                    BalanceAmount = data.BalanceAmount,
                                };

                                paymentTransactions.Add(transaction);
                            }
                        }
                    }

                    if (paymentTransactions.Any())
                    {
                        await psuLoan.AddRangePaymentTransaction(paymentTransactions);
                    }
                }
            }

        }
        catch (Exception ex)
        {
            await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
        }
    }
}