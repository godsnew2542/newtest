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
    public partial class ExportFileByManager
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;

        private IOrderedQueryable<VLoanRequestContract> loanRequestContractsTemp { get; set; } = null!;
        private List<VLoanRequestContract> newLoanRequestContracts { get; set; } = new();
        private List<SetNewRecordFirstPaymentModel> ListRecord { get; set; } = new();
        private SearchModel Search { get; set; } = new();

        private decimal[] StatusId { get; set; } = new[] { 2m, 4m };
        private DateTime? PaymentTime { get; set; } = null;
        private string SearchView { get; set; } = string.Empty;
        private int GovernmentOfficer { get; set; } = 0;
        private int UniversityStaff { get; set; } = 0;
        private int Employee { get; set; } = 0;
        private int IncomeEmployee { get; set; } = 0;
        private int _pageIndex { get; set; } = 1;



        protected async override Task OnInitializedAsync()
        {
            try
            {
                string? AdminCampId = StateProvider?.CurrentUser.CapmSelectNow;
                loanRequestContractsTemp = _context.VLoanRequestContracts
                    .Where(c => StatusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => string.IsNullOrEmpty(AdminCampId) || c.DebtorCampusId == AdminCampId)
                    .OrderBy(c => c.AdminRecordDate);

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

        private bool GetCheckContractId(VLoanRequestContract requestContract)
        {
            return ListRecord.Any(c => c.ContractId == requestContract.ContractId);
        }

        private async Task DataTableV2(string? searchName, DateTime? date)
        {
            newLoanRequestContracts = new();

            try
            {
                newLoanRequestContracts = await loanRequestContractsTemp
                    .Where(c => string.IsNullOrEmpty(searchName) ||
                    c.DebtorNameTh!.Contains(searchName) ||
                    c.DebtorSnameTh!.Contains(searchName) ||
                    c.DebtorNameEng!.ToLower().Contains(searchName.ToLower()) ||
                    c.DebtorSnameEng!.ToLower().Contains(searchName.ToLower()) ||
                    (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                    (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower()))
                    .Where(c => date == null ||
                    c.AdminRecordDate!.Value.Date == date.Value.Date && c.AdminRecordDate.Value.Month == date.Value.Month && c.AdminRecordDate.Value.Year == date.Value.Year)
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
                else if (val.Trim().Length < Utility.SearchMinlength)
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
            var FileName = "รายการคำขอที่ผ่านการตรวจสอบ";
            GovernmentOfficer = 0;
            UniversityStaff = 0;
            Employee = 0;
            IncomeEmployee = 0;

            try
            {
                if (data.Any())
                {
                    int row = 1;
                    var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Sheet1");

                    //Merge the cell
                    int lastRow = 2;

                    #region Table Header
                    ws.Cell(row, 1).Value = "ลำดับที่";
                    ws.Range(row, 1, lastRow, 1).Merge();

                    ws.Cell(row, 2).Value = "คำนำหน้านาม";
                    ws.Range(row, 2, lastRow, 2).Merge();

                    ws.Cell(row, 3).Value = "ชื่อ";
                    ws.Range(row, 3, lastRow, 3).Merge();

                    ws.Cell(row, 4).Value = "สกุล";
                    ws.Range(row, 4, lastRow, 4).Merge();

                    ws.Cell(row, 5).Value = "สังกัดส่วนงาน/หน่วยงาน";
                    ws.Range(row, 5, lastRow, 5).Merge();

                    ws.Cell(row, 6).Value = "ตำแหน่ง";
                    ws.Range(row, 6, lastRow, 6).Merge();

                    ws.Cell(row, 7).Value = "รหัสเงินเดือน";
                    ws.Range(row, 7, lastRow, 7).Merge();

                    ws.Cell(row, 8).Value = "ประเภทบุคลากร";
                    ws.Range(row, 8, row, 11).Merge();

                    ws.Cell(row, 12).Value = "วงเงินยืม";
                    ws.Range(row, 12, lastRow, 12).Merge();

                    ws.Cell(row, 13).Value = "ประเภทกู้ยืม";
                    ws.Range(row, 13, lastRow, 13).Merge();

                    #region new line Table Header Details
                    row++;
                    ws.Cell(row, 8).Value = "ข้าราชการ";
                    ws.Cell(row, 9).Value = "ลูกจ้างประจำ";
                    ws.Cell(row, 10).Value = "พนง.งบประมาณ";
                    ws.Cell(row, 11).Value = "พนง.เงินรายได้";
                    #endregion
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
                            //var loan = userService.GetLoanType((byte?)reqCon?.LoanTypeID);

                            string? StaffSalaryId = await _context.ContractMains
                            .Where(c => c.ContractId == reqCon.ContractId)
                            .Select(x => x.StaffSalaryId)
                            .FirstOrDefaultAsync();

                            var _GovernmentOfficer = AddPeopleByStaffType(Detail?.StaffType, 1);
                            var _Employee = AddPeopleByStaffType(Detail?.StaffType, 2);
                            var _UniversityStaff = AddPeopleByStaffType(Detail?.StaffType, 3);
                            var _IncomeEmployee = AddPeopleByStaffType(Detail?.StaffType, 4);

                            ws.Cell(row, 1).Value = i + 1;

                            ws.Cell(row, 2).Value = Detail?.TitleNameThai;

                            ws.Cell(row, 3).Value = Detail?.StaffNameThai;

                            ws.Cell(row, 4).Value = Detail?.StaffSnameThai;

                            ws.Cell(row, 5).Value = Detail?.DeptNameThai;

                            ws.Cell(row, 6).Value = Detail?.PosNameThai;

                            ws.Cell(row, 7).Value = StaffSalaryId;

                            ws.Cell(row, 8).Value = !string.IsNullOrEmpty(_GovernmentOfficer) ? _GovernmentOfficer : "0";

                            ws.Cell(row, 9).Value = !string.IsNullOrEmpty(_Employee) ? _Employee : "0";

                            ws.Cell(row, 10).Value = !string.IsNullOrEmpty(_UniversityStaff) ? _UniversityStaff : "0";

                            ws.Cell(row, 11).Value = !string.IsNullOrEmpty(_IncomeEmployee) ? _IncomeEmployee : "0";

                            ws.Cell(row, 12).Value = (double)(reqCon?.ContractLoanAmount != null ? reqCon.ContractLoanAmount : 0);

                            //ws.Cell(row, 13).Value = loan.LoanParentName;
                            ws.Cell(row, 13).Value = reqCon?.LoanTypeName;

                        }
                    }
                    //for (int i = 0; i < data.Count; i++)
                    //{
                    //    var info = data[i];
                    //    row++;

                    //    ContractMain? main = await _context.ContractMains
                    //        .Where(c => c.ContractId == info.ContractId)
                    //        .FirstOrDefaultAsync();
                    //    if (main != null)
                    //    {
                    //        var loan = userService.GetLoanType((byte?)main?.LoanTypeID);

                    //        ContractStatus? Cstatus = await _context.ContractStatuses
                    //            .Where(c => c.ContractStatusId == main.ContractStatusId)
                    //            .FirstOrDefaultAsync();

                    //        VLoanStaffDetail? Detail = userService.GetUserDetail(main?.DebtorStaffId);

                    //        if (Detail != null)
                    //        {
                    //            var _GovernmentOfficer = AddPeopleByStaffType(Detail.StaffType, 1);
                    //            var _Employee = AddPeopleByStaffType(Detail.StaffType, 2);
                    //            var _UniversityStaff = AddPeopleByStaffType(Detail.StaffType, 3);
                    //            var _IncomeEmployee = AddPeopleByStaffType(Detail.StaffType, 4);

                    //            ws.Cell(row, 1).Value = i + 1;

                    //            ws.Cell(row, 2).Value = Detail.TitleNameThai;

                    //            ws.Cell(row, 3).Value = Detail.StaffNameThai;

                    //            ws.Cell(row, 4).Value = Detail.StaffSnameThai;

                    //            ws.Cell(row, 5).Value = Detail.DeptNameThai;

                    //            ws.Cell(row, 6).Value = Detail.PosNameThai;

                    //            ws.Cell(row, 7).Value = main.StaffSalaryId;

                    //            ws.Cell(row, 8).Value = (!string.IsNullOrEmpty(_GovernmentOfficer) ? _GovernmentOfficer : "0");

                    //            ws.Cell(row, 9).Value = (!string.IsNullOrEmpty(_Employee) ? _Employee : "0");

                    //            ws.Cell(row, 10).Value = (!string.IsNullOrEmpty(_UniversityStaff) ? _UniversityStaff : "0");

                    //            ws.Cell(row, 11).Value = (!string.IsNullOrEmpty(_IncomeEmployee) ? _IncomeEmployee : "0");

                    //            ws.Cell(row, 12).Value = (double)main.LoanAmount;

                    //            ws.Cell(row, 13).Value = loan.LoanParentName;
                    //        }
                    //    }
                    //}
                    #endregion

                    #region Table Footer
                    row++;
                    ws.Cell(row, 7).Value = "รวมจำนวนเงินกู้";
                    ws.Cell(row, 8).Value = GovernmentOfficer;
                    ws.Cell(row, 9).Value = Employee;
                    ws.Cell(row, 10).Value = UniversityStaff;
                    ws.Cell(row, 11).Value = IncomeEmployee;
                    #endregion

                    MemoryStream XLSStream = new();
                    wb.SaveAs(XLSStream);

                    byte[] bytes = XLSStream.ToArray();
                    XLSStream.Close();

                    string ExportBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
                    DateTime idate = DateTime.Now;
                    await SaveToDbAsync(data, ExportBy, idate);

                    //var path = "data:application/vnd.ms-excel;base64," + Convert.ToBase64String(bytes)
                    //await JS.InvokeVoidAsync("jsSaveAsFileFromPath", FileName, path)

                    await SaveFileAndImgService.SaveFileAsPath(bytes, FileName, "data:application/vnd.ms-excel;base64,");
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task SaveToDbAsync(List<SetNewRecordFirstPaymentModel> data, string ExportBy, DateTime idate)
        {
            try
            {
                List<LoanExportToCommittee> toCommittees = new();

                for (int i = 0; i < data.Count; i++)
                {
                    var result = data[i];

                    LoanExportToCommittee Export = new()
                    {
                        ContractId = result.ContractId,
                        ExportBy = ExportBy,
                        ExportDate = idate,
                        ContractNo = result.LoanRequestContract?.ContractNo,
                        ContractApproveDate = result.LoanRequestContract?.ContractApproveDate
                    };

                    toCommittees.Add(Export);
                }

                await psuLoan.AddMutilateDataLoanExportToCommittee(toCommittees);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string AddPeopleByStaffType(string? StaffType, int key)
        {
            string Result = string.Empty;
            StaffTypeModel SType = new();

            if (SType.GovernmentOfficer.Contains(StaffType) && key == 1)
            {
                Result = "1";
                GovernmentOfficer++;
            }
            else if (SType.Employee.Contains(StaffType) && key == 2)
            {
                Result = "1";
                Employee++;
            }
            else if (SType.UniversityStaff.Contains(StaffType) && key == 3)
            {
                Result = "1";
                UniversityStaff++;
            }
            else if (SType.IncomeEmployee.Contains(StaffType) && key == 4)
            {
                Result = "1";
                IncomeEmployee++;
            }
            return Result;
        }
    }
}
