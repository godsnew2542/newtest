using ClosedXML.Excel;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.Services.LoanDb;
using LoanApp.Services.Services;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using LoanApp.Shared;

namespace LoanApp.Components.Admin;

public partial class ImportPaymentComponent
{
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    [Parameter] public UploadModel? ModelUploadCSV { get; set; } = null;

    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private Services.IServices.IImportPaymentService importPaymentService { get; set; } = null!;

    private StorageImportPaymentCSV RecordPayment { get; set; } = new();

    private int ReadCountNoExcel { get; set; } = 0;
    /// <summary>
    /// เกิดข้อผิดพลาดในการอ่านไฟล์หรือไหม [defult ไม่ = false]
    /// ค่าที่ check
    /// 1. สัญญาเงินกู้ != null || ""
    /// 2. จำนวน column >= 14
    /// </summary>
    private bool FileImportError { get; set; } = false;


    //protected override async Task OnAfterRenderAsync(bool firstRender)
    //{
    //    if (firstRender && ModelUploadCSV != null)
    //    {
    //        var FileName = ModelUploadCSV.TempImgName!.Split(".");
    //        RecordPayment.FileName = FileName[0];

    //        List<ImportRecordPaymentModel> importRecords = ReadExcel(ModelUploadCSV.Url);

    //        if (importRecords.Any() && !FileImportError)
    //        {
    //            await CheckDataImportRecordPaymentAsync(importRecords);
    //        }
    //    }
    //}

    //private List<ImportRecordPaymentModel> ReadExcel(string? path)
    //{
    //    if (string.IsNullOrEmpty(path))
    //    {
    //        return new List<ImportRecordPaymentModel>();
    //    }

    //    try
    //    {
    //        List<ImportRecordPaymentModel> importRecords = new();

    //        using (var workBook = new XLWorkbook(path))
    //        {
    //            var workSheet = workBook.Worksheet(1);
    //            var firstRowUsed = workSheet.FirstRowUsed();
    //            var firstPossibleAddress = workSheet.Row(firstRowUsed.RowNumber()).FirstCell().Address;
    //            var lastPossibleAddress = workSheet.LastCellUsed().Address;
    //            var range = workSheet.Range(firstPossibleAddress, lastPossibleAddress).AsRange();

    //            List<string[]> excelRows = range.RowsUsed()
    //                 .Skip(1)
    //                 .Select(row => row.Cells().Select(cell => cell.Value.ToString())
    //                 .ToArray())
    //                 .ToList();
    //            try
    //            {
    //                foreach (var item in excelRows.Select((value, i) => new { i, value }))
    //                {
    //                    var value = item.value;
    //                    var index = item.i;
    //                    ReadCountNoExcel = (index + 1);

    //                    if (value == null || value.Length < 14)
    //                    {
    //                        FileImportError = true;
    //                        break;
    //                    }
    //                    else if (string.IsNullOrEmpty(value[1]))
    //                    {
    //                        FileImportError = true;
    //                        break;
    //                    }

    //                    ImportRecordPaymentModel ExtractData = ExtractDataFromExcel(value);
    //                    importRecords.Add(ExtractData);
    //                }
    //            }
    //            catch (Exception)
    //            {
    //                throw;
    //            }
    //        }

    //        return importRecords;
    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }
    //}

    //private ImportRecordPaymentModel ExtractDataFromExcel(string[] row)
    //{
    //    try
    //    {
    //        DateTimeFormatInfo TH = new CultureInfo(Utility.DateLanguage_TH, false).DateTimeFormat;
    //        ImportRecordPaymentModel ImportPayment = new();

    //        #region คก.
    //        var PsuProject = row[0];
    //        ImportPayment.PsuProject = (PsuProject != null &&
    //            !string.IsNullOrEmpty(PsuProject.ToString()) &&
    //            !string.IsNullOrWhiteSpace(PsuProject.ToString()) ?
    //            PsuProject.ToString() : string.Empty);

    //        #endregion

    //        #region สัญญาเงินกู้
    //        var ContractNo = row[1];
    //        ImportPayment.ContractNo = (ContractNo != null &&
    //            !string.IsNullOrEmpty(ContractNo.ToString()) &&
    //            !string.IsNullOrWhiteSpace(ContractNo.ToString()) ?
    //            ContractNo.ToString() : string.Empty);

    //        #endregion

    //        #region รหัสบุคลากร => {รหัสเงินเดือน}
    //        var StaffSalaryId = row[2];
    //        ImportPayment.StaffSalaryId = (StaffSalaryId != null &&
    //            !string.IsNullOrEmpty(StaffSalaryId.ToString()) &&
    //            !string.IsNullOrWhiteSpace(StaffSalaryId.ToString()) ?
    //            StaffSalaryId.ToString() : string.Empty);

    //        #endregion

    //        #region ชื่อ
    //        var Fname = row[3];
    //        ImportPayment.Fname = (Fname != null &&
    //            !string.IsNullOrEmpty(Fname.ToString()) &&
    //            !string.IsNullOrWhiteSpace(Fname.ToString()) ?
    //            Fname.ToString() : string.Empty);

    //        #endregion

    //        #region สกุล
    //        var Lname = row[4];
    //        ImportPayment.Lname = (Lname != null &&
    //            !string.IsNullOrEmpty(Lname.ToString()) &&
    //            !string.IsNullOrWhiteSpace(Lname.ToString()) ?
    //            Lname.ToString() : string.Empty);

    //        #endregion

    //        #region ประเภทยืม
    //        var LoanParentName = row[5];
    //        ImportPayment.LoanParentName = (LoanParentName != null &&
    //            !string.IsNullOrEmpty(LoanParentName.ToString()) &&
    //            !string.IsNullOrWhiteSpace(LoanParentName.ToString()) ?
    //            LoanParentName.ToString() : string.Empty);

    //        #endregion

    //        #region ยอดคงเหลือ == "0"  สิ้นสุดสัญญา
    //        var BalanceAmount = row[6];
    //        if (!string.IsNullOrEmpty(BalanceAmount))
    //        {
    //            if (BalanceAmount.ToString() == "0")
    //            {
    //                ImportPayment.BalanceAmount = 0m;
    //            }
    //            else if (!string.IsNullOrWhiteSpace(BalanceAmount))
    //            {
    //                ImportPayment.BalanceAmount = Convert.ToDecimal(BalanceAmount.ToString());
    //            }
    //        }
    //        else
    //        {
    //            ImportPayment.BalanceAmount = null;
    //        }

    //        #endregion

    //        #region วันที่รายงาน
    //        var PayDate = row[7];
    //        ImportPayment.PayDate = (PayDate != null &&
    //            !string.IsNullOrEmpty(PayDate.ToString()) &&
    //            !string.IsNullOrWhiteSpace(PayDate.ToString()) ?
    //            Convert.ToDateTime(PayDate.ToString(), TH) : null);

    //        #endregion

    //        #region จำนวนเงินยืม
    //        var LoanAmount = row[8];
    //        ImportPayment.LoanAmount = (LoanAmount != null &&
    //            !string.IsNullOrEmpty(LoanAmount.ToString()) &&
    //            !string.IsNullOrWhiteSpace(LoanAmount.ToString()) ?
    //            Convert.ToDecimal(LoanAmount.ToString()) : null);

    //        #endregion

    //        #region จำนวนงวด
    //        var LoanNumInstallments = row[9];
    //        ImportPayment.LoanNumInstallments = (LoanNumInstallments != null &&
    //            !string.IsNullOrEmpty(LoanNumInstallments.ToString()) &&
    //            !string.IsNullOrWhiteSpace(LoanNumInstallments.ToString()) ?
    //            Convert.ToDecimal(LoanNumInstallments.ToString()) : null);

    //        #endregion

    //        #region งวดที่จ่าย
    //        var InstallmentNo = row[10];
    //        ImportPayment.InstallmentNo = (InstallmentNo != null &&
    //            !string.IsNullOrEmpty(InstallmentNo.ToString()) &&
    //            !string.IsNullOrWhiteSpace(InstallmentNo.ToString()) ?
    //            Convert.ToDecimal(InstallmentNo.ToString()) : null);

    //        #endregion

    //        #region เงินต้น
    //        var PrincipleAmount = row[11];
    //        ImportPayment.PrincipleAmount = (PrincipleAmount != null &&
    //            !string.IsNullOrEmpty(PrincipleAmount.ToString()) &&
    //            !string.IsNullOrWhiteSpace(PrincipleAmount.ToString()) ?
    //            Convert.ToDecimal(PrincipleAmount.ToString()) : null);

    //        #endregion

    //        #region ดอกเบี้ย
    //        var InterestAmont = row[12];
    //        ImportPayment.InterestAmont = (InterestAmont != null &&
    //            !string.IsNullOrEmpty(InterestAmont.ToString()) &&
    //            !string.IsNullOrWhiteSpace(InterestAmont.ToString()) ?
    //            Convert.ToDecimal(InterestAmont.ToString()) : null);

    //        #endregion

    //        #region รวมหัก
    //        var TotalAmount = row[13];
    //        ImportPayment.TotalAmount = (TotalAmount != null &&
    //            !string.IsNullOrEmpty(TotalAmount.ToString()) &&
    //            !string.IsNullOrWhiteSpace(TotalAmount.ToString()) ?
    //            Convert.ToDecimal(TotalAmount.ToString()) : null);

    //        #endregion

    //        return ImportPayment;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.Message);
    //        throw;
    //    }
    //}

    //private async Task CheckDataImportRecordPaymentAsync(List<ImportRecordPaymentModel> importRecords)
    //{
    //    string ImportBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

    //    try
    //    {
    //        List<string> contractNoFromFile = importRecords
    //            .Where(c => !string.IsNullOrEmpty(c.ContractNo))
    //            .Select(c => c.ContractNo!)
    //            .ToList();

    //        List<ContractMain> contractMains = await psuLoan.GetListContractMainByListContractNo(contractNoFromFile);
    //        List<string?> contractNoFromDB = contractMains.Select(c => c.ContractNo).ToList();

    //        #region ไม่พบใน DB 
    //        RecordPayment.PaymentFail = importRecords
    //            .Where(c => !contractNoFromDB!.Contains(c.ContractNo))
    //            .ToList();
    //        #endregion

    //        if (contractMains.Any())
    //        {
    //            #region เช็คว่าใน DB เป็นสิ้นสุดสัญาหรือยัง
    //            var isLoanSuccess = contractMains
    //                .Where(c => c.ContractStatusId == 99)
    //                .ToList();

    //            List<string?> contractNoFromIsLoanSuccess = isLoanSuccess
    //                .Select(c => c.ContractNo)
    //                .ToList();

    //            if (isLoanSuccess.Any())
    //            {
    //                var data = importRecords
    //                    .Where(c => contractNoFromIsLoanSuccess!.Contains(c.ContractNo))
    //                    .ToList();

    //                data.ForEach(x => x.Remark = "สัญญาสิ้นสุดแล้ว");

    //                foreach (var item in data)
    //                {
    //                    RecordPayment.PaymentFail.Add(item);
    //                }
    //            }

    //            #endregion

    //            var iPayments = importRecords
    //                .Where(c => !contractNoFromIsLoanSuccess.Contains(c.ContractNo))
    //                .Where(c => contractNoFromDB.Contains(c.ContractNo))
    //                .ToList();

    //            if (iPayments.Any())
    //            {
    //                foreach (var importPayment in iPayments)
    //                {
    //                    #region เช็คว่าเคยบันทึกลง db หรือไหม เช็คจาก payDate เฉพาะ import file
    //                    var isHaveDataFromDb = await psuLoan.CheckTransactionFromPayDate(importPayment.ContractNo, importPayment.PayDate);
    //                    #endregion

    //                    if (isHaveDataFromDb == false)
    //                    {
    //                        PaymentTransaction? isPaymentTransaction = await importPaymentService.CheckPaymentTransactionAgainAsync(importPayment.ContractNo!, importPayment.InstallmentNo, importPayment.PayDate);

    //                        ImportPaidLoan? isImportPaidLoan = await importPaymentService.CheckImportPaidLoanAgainAsync(importPayment.ContractNo!, importPayment.InstallmentNo, importPayment.PayDate);

    //                        importPayment.IsPaymentTransaction = (isPaymentTransaction == null ? false : true);
    //                        importPayment.IsImportPaidLoan = (isImportPaidLoan == null ? false : true);

    //                        if (importPayment.IsPaymentTransaction)
    //                        {
    //                            importPayment.IsAlready = importPayment.IsPaymentTransaction;
    //                            RecordPayment.PaymentFail.Add(importPayment);
    //                        }
    //                        else
    //                        {
    //                            ContractMain? cMain = contractMains
    //                                .Where(c => c.ContractNo == importPayment.ContractNo)
    //                                .FirstOrDefault();

    //                            if (importPayment.BalanceAmount == 0)
    //                            {
    //                                importPayment.IsFinalInstallmentByBalanceAmount = true;
    //                            }

    //                            if (cMain != null)
    //                            {
    //                                importPayment.saveImportPayment = await CheckFullDataFromPaymentSuccessAsync(importPayment,
    //                                    ImportBy,
    //                                    cMain);
    //                            }

    //                            RecordPayment.PaymentSuccess.Add(importPayment);
    //                        }
    //                    }
    //                    else
    //                    {
    //                        importPayment.IsAlready = true;
    //                        RecordPayment.PaymentFail.Add(importPayment);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }
    //}

    ///// <summary>
    ///// เช็คว่าเคยมี งวดนี้หรือไหม แค่งวด ไม่ได้ลงถึงวัน
    ///// </summary>
    ///// <param name="ele"></param>
    ///// <param name="ImportBy"></param>
    ///// <param name="contractMain"></param>
    ///// <returns></returns>
    //private async Task<SaveImportPaymentServiceModel> CheckFullDataFromPaymentSuccessAsync(ImportRecordPaymentModel ele, string ImportBy, ContractMain contractMain)
    //{
    //    SaveImportPaymentServiceModel saveImport = new();

    //    var BalanceAmount = ele.BalanceAmount;
    //    try
    //    {
    //        if (!ele.IsImportPaidLoan)
    //        {
    //            saveImport.ImportPaid =
    //                importPaymentService.SetTableImportPaidLoan(ele, BalanceAmount, ImportBy);
    //        }

    //        if (!ele.IsPaymentTransaction)
    //        {
    //            SaveImportPaymentServiceModel SetPaymentTransaction =
    //                await importPaymentService.SetTablePaymentTransactionAsync(ele, BalanceAmount, contractMain);

    //            saveImport.IsSaveTablePaymentTransaction = true;
    //            saveImport.Transaction = SetPaymentTransaction.Transaction;

    //            // ?? น่าจะ check ว่าเคยบันทึกงวดนี้ไปแล้วยัง
    //            // เช่นไปปิดยอดหน้าเคอร์เตอร์ แล้วกองคลังส่งมา ที่เป็นงวดซ้ำ จะทำการ +1 งวด
    //            if (SetPaymentTransaction.ChangeData != null)
    //            {
    //                if (saveImport.ChangeData == null)
    //                {
    //                    saveImport.ChangeData = new();
    //                }

    //                saveImport.ChangeData.NewInstallmentNo = SetPaymentTransaction.ChangeData.NewInstallmentNo;

    //                if (saveImport.ImportPaid != null)
    //                {
    //                    saveImport.ImportPaid.ReferenceId1 = saveImport.ChangeData.NewInstallmentNo?.Remark;
    //                }
    //            }
    //        }
    //        else
    //        {
    //            // กรณีพบใน Table PaymentTransactions => Get val ล่าสุดมาเลย
    //            saveImport.Transaction = await importPaymentService.GetPaymentTransactionByDataAsync(ele);
    //        }

    //        ContractMain? Contract = null;
    //        VLoanRequestContract? reqCon = null;

    //        if (PaymentServiceModel.Transaction != null)
    //        {
    //            Contract = await importPaymentService
    //                .GetContractMainByContractIdAsync(PaymentServiceModel.Transaction.ContractId);

    //            if (Contract != null)
    //            {
    //                reqCon = userService.GetVLoanRequestContract(Contract.LoanRequestId);
    //            }
    //        }

    //        // TODO (ImportPayment) การเช็คการ สิ้นสุดสัญญา เข็คจาก BalanceAmount == 0
    //        if (ele.IsFinalInstallmentByBalanceAmount)
    //        {
    //            PaymentServiceModel.HisContractMain =
    //                await importPaymentService.SetHisContractMainAsync(ele, BalanceAmount, StateProvider?.CurrentUser.StaffId);
    //            saveImport.LoanIsLast = true;
    //        }
    //        else
    //        {
    //            var paymentTransaction = await importPaymentService.GetLastInstallmentNoFromPaymentTransaction(ele);

    //            if (paymentTransaction != null)
    //            {
    //                var resultPay = paymentTransaction.BalanceAmount - ele.TotalAmount;

    //                // อาจจะ มีค่าน้อยกว่า  0
    //                if (resultPay <= 0)
    //                {
    //                    PaymentServiceModel.HisContractMain =
    //                    await importPaymentService.SetHisContractMainAsync(ele, resultPay, StateProvider?.CurrentUser.StaffId);
    //                    saveImport.LoanIsLast = true;

    //                    if (saveImport.ChangeData == null)
    //                    {
    //                        saveImport.ChangeData = new();
    //                    }

    //                    saveImport.ChangeData.NewBalanceAmount = new()
    //                    {
    //                        NewData = resultPay,
    //                        OldData = BalanceAmount,
    //                        Remark = $"From {BalanceAmount} To {resultPay}"
    //                    };

    //                    if (saveImport.ImportPaid != null &&
    //                        saveImport.ChangeData.NewBalanceAmount != null)
    //                    {
    //                        saveImport.ImportPaid.ReferenceId2 = saveImport.ChangeData.NewBalanceAmount.Remark;
    //                    }
    //                }
    //            }

    //        }

    //        return saveImport;
    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }
    //}
}
