using ClosedXML.Excel;
using LoanApp.Components.Test;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace LoanApp.Components.Dev
{
    /// <summary>
    /// สำหรับนำเข้าข้อมูล Table INSTALLMENT_DETAIL ในรูปแบบ EXLSX
    /// </summary>
    public partial class ImportNewInstallmentDetail
    {
        #region Inject
        [Inject] Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        #endregion

        private UploadModel? ResultFile { get; set; } = null;

        private bool Isloading { get; set; } = false;

        private void Callback(UploadModel model)
        {
            ResultFile = model;
            StateHasChanged();
        }

        private void FileCallback(UploadModel model)
        {
            ResultFile = null;
            StateHasChanged();
        }

        /// <summary>
        /// รูปแบบไฟล์ เป็น xlsx
        /// [col 1] CONTRACT_ID	||
        /// [col 2] PAID_DATE 
        /// [col 3] CONTRACT_LOAN_AMOUNT ||	
        /// [col 4] CONTRACT_LOAN_INTEREST || 
        /// [col 5]CONTRACT_LOAN_NUM_INSTALLMENTS
        /// </summary>
        /// <returns></returns>
        private async Task GenNewInstallmentDetail()
        {
            Isloading = true;
            await Task.Delay(1);
            StateHasChanged();

            List<DevNewInstallmentDetailModel> newInstallmentDetail = new();

            if (ResultFile != null)
            {
                using (var workBook = new XLWorkbook(ResultFile.Url))
                {
                    var workSheet = workBook.Worksheet(1);
                    var firstRowUsed = workSheet.FirstRowUsed();
                    var firstPossibleAddress = workSheet.Row(firstRowUsed.RowNumber()).FirstCell().Address;
                    var lastPossibleAddress = workSheet.LastCellUsed().Address;
                    var range = workSheet.Range(firstPossibleAddress, lastPossibleAddress).AsRange();

                    List<string[]> rows = range.RowsUsed()
                        .Skip(1)
                        .Select(row => row.Cells().Select(cell => cell.Value.ToString())
                        .ToArray())
                        .ToList();

                    if (rows.Any())
                    {
                        foreach (var row in rows)
                        {
                            DevNewInstallmentDetailModel? data = SetNewData(row);

                            if (data != null)
                            {
                                newInstallmentDetail.Add(data);
                            }
                        }
                    }
                }
            }

            if (newInstallmentDetail.Any())
            {
                try
                {
                    newInstallmentDetail = newInstallmentDetail.Where(c => c.ContractId != null).ToList();

                    List<decimal> test = newInstallmentDetail
                        .Select(x => x.ContractId!.Value)
                        .ToList();

                    var reCheck = await psuLoan.CheckInstallmentDetailByContractId(test);

                    if (reCheck.Any())
                    {
                        newInstallmentDetail = newInstallmentDetail
                            .Where(c => !reCheck.Contains(c.ContractId!.Value))
                            .ToList();
                    }

                    PaymentListComponent paymentListComponentPage = new();

                    foreach (var item in newInstallmentDetail)
                    {
                        List<InstallmentDetail> installmentDetails = paymentListComponentPage.SetInstallmentDetail(item.PaidDate!.Value, (int)item.LoanNumInstallments!.Value, item.LoanAmount!.Value, item.LoanInterest, TransactionService, item.ContractId);

                        await psuLoan.AddMutilateDataInstallmentDetail(installmentDetails);
                    }
                }
                catch (Exception ex)
                {
                    _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
                    
                    Isloading = false;
                    await Task.Delay(1);
                    StateHasChanged();
                    return;
                }
            }

            _ = Task.Run(() => { notificationService.SuccessDefult("เพิ่มข้อมูลสำเร็จ"); });
            ResultFile = null;

            Isloading = false;
            await Task.Delay(1);
            StateHasChanged();
        }

        private DevNewInstallmentDetailModel? SetNewData(string[] row)
        {
            try
            {
                DateTimeFormatInfo En = new CultureInfo(Utility.DateLanguage_EN, false).DateTimeFormat;

                DevNewInstallmentDetailModel data = new()
                {
                    ContractId = !string.IsNullOrEmpty(row[0]) ? Convert.ToDecimal(row[0].ToString()) : null,
                    PaidDate = !string.IsNullOrEmpty(row[1]) ? Convert.ToDateTime(row[1].ToString(), En) : null,
                    LoanAmount = !string.IsNullOrEmpty(row[2]) ? Convert.ToDecimal(row[2].ToString()) : null,
                    LoanInterest = !string.IsNullOrEmpty(row[3]) ? Convert.ToDecimal(row[3].ToString()) : null,
                    LoanNumInstallments = !string.IsNullOrEmpty(row[4]) ? Convert.ToDecimal(row[4].ToString()) : null,
                };

                return data;
            }
            catch (Exception ex)
            {
                _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
                return null;
            }

        }

        private async Task tst2()
        {
            //await psuLoan.DeleteDev();

            await notificationService.SuccessDefult("👌👌👌👌");
        }
    }

    /// <summary>
    /// ตัวอย่างไฟล์ที่จะนำเข้า อยู่ใน project fileName: ตัวอย่างไฟล์ที่จะนำเข้า_InstallmentDetail.xlsx
    /// </summary>
    public class DevNewInstallmentDetailModel
    {
        /// <summary>
        /// id
        /// </summary>
        public decimal? ContractId { get; set; }

        /// <summary>
        /// วันที่ได้รับเงิน
        /// </summary>
        public DateTime? PaidDate { get; set; }

        /// <summary>
        /// ยอดเงินกู้
        /// </summary>
        public decimal? LoanAmount { get; set; }

        /// <summary>
        /// ดอกเบี้ย
        /// </summary>
        public decimal? LoanInterest { get; set; }

        /// <summary>
        /// จำนวนงวด
        /// </summary>
        public decimal? LoanNumInstallments { get; set; }
    }
}
