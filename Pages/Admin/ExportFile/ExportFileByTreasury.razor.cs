using ClosedXML.Excel;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.Admin.ExportFile
{
    public partial class ExportFileByTreasury
    {
        [CascadingParameter] public Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;

        /// <summary>
        /// IOrderedQueryable
        /// </summary>
        private IOrderedQueryable<VLoanRequestContract> loanRequestContractsTemp { get; set; } = null!;
        private List<VLoanRequestContract> newLoanRequestContracts { get; set; } = new();

        private List<SetNewRecordFirstPaymentModel> ListRecord { get; set; } = new();
        private SearchModel Search { get; set; } = new();

        private DateTime? PaymentTime { get; set; } = null;
        private decimal[] StatusId { get; } = new[] { 9m };
        private string? SearchView { get; set; } = null;
        private int _pageIndex { get; set; } = 1;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                string? AdminCampId = StateProvider?.CurrentUser.CapmSelectNow;

                loanRequestContractsTemp = _context.VLoanRequestContracts
                    .Where(c => StatusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => string.IsNullOrEmpty(AdminCampId) || c.DebtorCampusId == AdminCampId)
                    .OrderBy(c => c.ContractDate);

                await DataTableV2(Search.Title, PaymentTime);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private void CheckboxClickedV2(decimal? ContractId, bool value, VLoanRequestContract requestContract)
        {
            if (ListRecord.Any())
            {
                var myTodo = ListRecord.Find(x => x.ContractId == ContractId);

                if (myTodo != null)
                {
                    ListRecord.Remove(myTodo);
                }
                else
                {
                    ListRecord.Add(Utility.AddListRecord(ContractId, value, requestContract));
                }
            }
            else
            {
                ListRecord.Add(Utility.AddListRecord(ContractId, value, requestContract));
            }
        }

        private async Task DataTableV2(string? searchName, DateTime? date)
        {
            newLoanRequestContracts = new();

            try
            {
                newLoanRequestContracts = await loanRequestContractsTemp
                    .Where(c => string.IsNullOrEmpty(searchName) ||
                    (c.DebtorNameTh!.Contains(searchName) ||
                    c.DebtorSnameTh!.Contains(searchName) ||
                    (c.DebtorNameEng!).ToLower().Contains(searchName.ToLower()) ||
                    (c.DebtorSnameEng!).ToLower().Contains(searchName.ToLower()) ||
                    (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                    (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower())))
                    .Where(c => date == null ||
                    c.ContractDate!.Value.Date == date.Value.Date &&
                    c.ContractDate.Value.Month == date.Value.Month &&
                    c.ContractDate.Value.Year == date.Value.Year)
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task NewOnChangeAsync(DateTime? value)
        {
            PaymentTime = value;
            await OnSearch(Search.Title, PaymentTime);
        }

        private void SetOrClearCheckedAsync(bool data)
        {
            ListRecord = new();
            if (data)
            {
                if (newLoanRequestContracts.Any())
                {
                    foreach (var item in newLoanRequestContracts)
                    {
                        CheckboxClickedV2(item.ContractId, data, item);
                    }
                }
            }
        }

        private async Task ExportToExcel2Async(List<SetNewRecordFirstPaymentModel> data)
        {
            var FileName = "รายการทำสัญญาเสร็จสิิ้น";
            decimal? SumLoanAmount = 0m;
            try
            {
                if (data.Count != 0)
                {
                    int row = 1;
                    var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Sheet1");

                    #region Table Header
                    ws.Cell(row, 1).Value = "ลำดับที่";

                    ws.Cell(row, 2).Value = "คำนำหน้านาม";

                    ws.Cell(row, 3).Value = "ชื่อ";

                    ws.Cell(row, 4).Value = "สกุล";

                    ws.Cell(row, 5).Value = "รหัสเงินเดือน";

                    ws.Cell(row, 6).Value = "วงเงินยืม";

                    ws.Cell(row, 7).Value = "ผ่อนชำระ (งวด)";

                    ws.Cell(row, 8).Value = "เลขที่บัญชี";

                    ws.Cell(row, 9).Value = "ชื่อธนาคาร";

                    ws.Cell(row, 10).Value = "สาขา";

                    ws.Cell(row, 11).Value = "ผู้ค้ำประกัน";

                    ws.Cell(row, 12).Value = "ประเภทกู้ยืม";

                    #endregion

                    #region Table Boby
                    for (int i = 0; i < data.Count; i++)
                    {
                        var info = data[i];
                        row++;

                        VLoanRequestContract? reqCon = info.LoanRequestContract;
                        VLoanStaffDetail? Detail = await psuLoan.GetUserDetailAsync(reqCon?.DebtorStaffId);

                        if (reqCon != null)
                        {
                            //string? StaffSalaryId = await _context.ContractMains
                            //.Where(c => c.ContractId == reqCon.ContractId)
                            //.Select(x => x.StaffSalaryId)
                            //.FirstOrDefaultAsync();

                            string? StaffSalaryId = (await psuLoan.GeContractMainByContractIdAsync(reqCon.ContractId))?.StaffSalaryId;

                            LoanStaffDetail? bookBank = await psuLoan.GetLoanStaffDetailByStaffId(reqCon.DebtorStaffId);

                            SumLoanAmount += (reqCon?.ContractLoanAmount != null ? reqCon?.ContractLoanAmount : 0);


                            ws.Cell(row, 1).Value = i + 1;

                            ws.Cell(row, 2).Value = Detail?.TitleNameThai;

                            ws.Cell(row, 3).Value = Detail?.StaffNameThai;

                            ws.Cell(row, 4).Value = Detail?.StaffSnameThai;

                            ws.Cell(row, 5).Value = StaffSalaryId;

                            ws.Cell(row, 6).Value = (double)(reqCon?.ContractLoanAmount != null ? reqCon.ContractLoanAmount : 0);

                            ws.Cell(row, 7).Value = (double)(reqCon?.ContractLoanNumInstallments != null ? reqCon.ContractLoanNumInstallments : 0);

                            ws.Cell(row, 8).Value = bookBank?.BookBankNo;

                            ws.Cell(row, 9).Value = bookBank?.BookBankName;

                            ws.Cell(row, 10).Value = bookBank?.BookBankBranch;

                            ws.Cell(row, 11).Value = userService.GetNameForGuarantor(reqCon)?.FullNameTh;

                            ws.Cell(row, 12).Value = reqCon?.LoanTypeName;

                        }
                    }

                    #endregion

                    #region Table Footer
                    row++;

                    ws.Cell(row, 5).Value = "รวมทั้งสิ้น";

                    ws.Cell(row, 6).Value = (SumLoanAmount != null ?
                        (double)SumLoanAmount : 0);

                    #endregion

                    MemoryStream XLSStream = new();
                    wb.SaveAs(XLSStream);

                    byte[] bytes = XLSStream.ToArray();
                    XLSStream.Close();

                    string ExportBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
                    DateTime idate = DateTime.Now;
                    await SaveToDbAsync(data, ExportBy, idate);

                    await SaveFileAndImgService.SaveFileAsPath(bytes, FileName, "data:application/vnd.ms-excel;base64,");

                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task SaveToDbAsync(List<SetNewRecordFirstPaymentModel> data,
            string ExportBy,
            DateTime idate)
        {
            try
            {
                List<LoanExportToTreasury> ExportToTreasuries = new();
                for (int i = 0; i < data.Count; i++)
                {
                    var result = data[i];

                    LoanExportToTreasury Export = new()
                    {
                        ContractId = result.ContractId,
                        ExportBy = ExportBy,
                        ExportDate = idate,
                        ContractNo = result.LoanRequestContract?.ContractNo,
                        ContractDate = result.LoanRequestContract?.ContractDate
                    };

                    ExportToTreasuries.Add(Export);
                }

                await psuLoan.AddMutilateDataLoanExportToTreasury(ExportToTreasuries);
            }
            catch (Exception)
            {
                throw;
            }

        }

        private bool GetCheckContractId(VLoanRequestContract requestContract)
        {
            return ListRecord.Any(c => c.ContractId == requestContract.ContractId);
        }

        private async Task OnSearch(string? val, DateTime? date)
        {
            try
            {
                Search.Title = string.Empty;

                if (string.IsNullOrEmpty(val))
                {
                    await DataTableV2(null, date);
                    return;
                }
                else if ((val.Trim()).Length < Utility.SearchMinlength)
                {
                    await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
                    return;
                }

                _pageIndex = 1;
                Search.Title = val.Trim();
                await DataTableV2(Search.Title, date);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

    }
}
