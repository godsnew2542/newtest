using AntDesign;
using ClosedXML.Excel;
using LoanApp.Components.AdminOption;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Pages.Admin.ExportFile
{
    public partial class ExportFileLoanAgreement
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #region Inject
        [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private ModalService ModalService { get; set; } = null!;

        #endregion

        private List<VReportTransaction> ReportData { get; set; } = new();
        private Dictionary<string, CCampus> CampusDict { get; set; } = new();
        private List<string> CollapseDefault { get; set; } = new();

        
        private DateTime? StartDate { get; set; } // new DateTime(2022, 5, 1)
        private DateTime? EndDate { get; set; } // new DateTime(2022, 5, 30)
        private DateTime? StartDateTemp { get; set; } = null;
        private DateTime? EndDateTemp { get; set; } = null;
        private int _pageIndex { get; set; } = 1;
        private bool Loading { get; set; } = false;

        private ReportLoanTypeDataByStaff? ReportLoanTypeDataRef { get; set; }
        private ReportTypeDataByStaff? ReportTypeDataRef { get; set; }
        private ReportLoanForMonth? ReportLoanMonthRef { get; set; }
        private DebtorRegisterForMonth? DebtorRegisterForMonthRef { get; set; }

        private CheckboxOption<string>[] ckeckAllOptions { get; set; } = new CheckboxOption<string>[]
        {
            new()
            {
                Label="ตามประเภทสวัสดิการ",
                Value="LoanDataByPeople",
                Checked=true
            },
            new()
            {
                Label="ตามประเภทบุคลากร",
                Value="TypeDataByPeople",
                Checked=true
            },
            //new()
            //{
            //    Label="รายละเอียดการหักชำระสวัสดิการเงินกู้",
            //    Value="PaymentDataByPeople" ,
            //    Checked=true
            //},
            new()
            {
                Label="ทะเบียนลูกหนี้",
                Value="DebtorRegister" ,
                Checked=false
            },
        };

        //bool indeterminate => ckeckAllOptions.Count(o => o.Checked) > 0 && ckeckAllOptions.Count(o => o.Checked) < ckeckAllOptions.Count()

        //bool checkAll => ckeckAllOptions.All(o => o.Checked)

        protected override void OnInitialized()
        {
            SetStartDate(DateTime.Now);
            SetEndDate(DateTime.Now);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    CampusDict = await EntitiesCentralService.GetCampusDict();
                }
                catch (Exception ex)
                {
                    _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
                }
            }
        }

        private void SetStartDate(DateTime? e)
        {
            if (e != null)
            {
                StartDate = new DateTime(e.Value.Year, e!.Value.Month, 1);
                return;
            }

            StartDate = null;
        }

        private void SetEndDate(DateTime? e)
        {
            if (e != null)
            {
                int lastDay = DateTime.DaysInMonth(e!.Value.Year, e!.Value.Month);

                EndDate = new DateTime(e!.Value.Year, e!.Value.Month, lastDay);
                return;
            }

            EndDate = null;
        }

        //private void CheckAllChanged()
        //{
        //    bool allChecked = checkAll;
        //    ckeckAllOptions.ForEach(o => o.Checked = !allChecked);
        //}

        private void CheckboxGroupChanged(string[]? checkbox)
        {

        }

        private async Task PerViewData()
        {
            ReportData = new();
            CollapseDefault = new();
            StartDateTemp = null;
            EndDateTemp = null;

            await Task.Delay(1);
            StateHasChanged();

            #region Chcek data Validation
            if (StartDate == null || EndDate == null)
            {
                _ = Task.Run(() => { notificationService.WarningDefult("กรุณาเลือก เดือนเริ่มต้นหรือ เดือนสิ้นสุด"); });
                return;
            }

            if (EndDate <= StartDate)
            {
                _ = Task.Run(() => { notificationService.WarningDefult("กรุณาตรวจสอบ เดือนเริ่มต้นและ เดือนสิ้นสุด"); });
                return;
            }

            if (!ckeckAllOptions.Any(c => c.Checked))
            {
                _ = Task.Run(() => { notificationService.WarningDefult($"กรุณาเลือก รายงานที่ต้องการออก"); });
                return;
            }

            #endregion

            Loading = true;
            await Task.Delay(1);
            StateHasChanged();

            StartDateTemp = StartDate;
            EndDateTemp = EndDate;
            CollapseDefault = ckeckAllOptions
                .Where(c => c.Checked)
                .Select(x => x.Value)
                .ToList();

            try
            {

                ReportData = await psuLoan.GetListVReportTransactionByExportFileLoanAgreement(StartDate, EndDate, StateProvider?.CurrentUser.CapmSelectNow);

                _pageIndex = 1;
                Loading = false;
            }
            catch (Exception ex)
            {
                Loading = false;
                _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
            }
        }

        private async Task DowloadData()
        {
            var cc = CollapseDefault.Any(x => x == "DebtorRegister");
            if (cc)
            {
                bool isCancel = await ModalService.ConfirmAsync(new ConfirmOptions()
                {
                    Title = "ยืนยันการดาวน์โหลด ทะเบียนลูกหนี้ หรือไม่",
                    Content = $"การดาวน์โหลดนี้ใช้ระยะเวลานาน 1 ข้อมูล ใช่เวลาปรมาณ 10 วินาที [ขณะนี้ ทดสอบอยู่ที่ 20 ข้อมูล]",
                    CancelButtonProps =
                    {
                        ChildContent = CancelButtonRender,
                        Type = ButtonType.Link,
                    },
                    OkButtonProps =
                    {
                        ChildContent = OkButtonRender,
                        Type = ButtonType.Link,
                    }
                });

                if (isCancel)
                {
                    return;
                }
            }

            Loading = true;
            await Task.Delay(1);
            StateHasChanged();

            List<LoanAgreementExportExcel> LoanExportExcels = new();

            foreach (var collapse in CollapseDefault)
            {
                if (collapse == "PaymentDataByPeople" && ReportLoanMonthRef != null)
                {
                    var data = ReportLoanMonthRef.ResultDataUI();

                    LoanExportExcels.Add(SetPaymentData(data, 3));
                }

                if (collapse == "LoanDataByPeople" && ReportLoanTypeDataRef != null)
                {
                    var data = ReportLoanTypeDataRef.ResultDataUI();

                    LoanExportExcels.Add(SetLoanData(data, 1));
                }

                if (collapse == "TypeDataByPeople" && ReportTypeDataRef != null)
                {
                    var data = ReportTypeDataRef.ResultDataUI();

                    LoanExportExcels.Add(SetTypeData(data, 2));
                }

                if (collapse == "DebtorRegister" && DebtorRegisterForMonthRef != null)
                {
                    var data = DebtorRegisterForMonthRef.ResultDataUI();

                    LoanExportExcels.Add(SetDebtorRegister(data, 4));
                }
            }

            await ExportFileExcel(LoanExportExcels);

            Loading = false;
            await Task.Delay(1);
            StateHasChanged();

            await notificationService.SuccessDefult("ดาวน์โหลดข้อมูลสำเสร็จ");
        }

        private async Task ExportFileExcel(List<LoanAgreementExportExcel> excels)
        {
            string fileName = $"สรุปการหักชำระเงินกู้รายเดือน_{dateService.ChangeDate(DateTime.Now, "dMMyyyy", Utility.DateLanguage_TH)}";
            if (excels.Any())
            {
                MemoryStream XLSStream = new();
                try
                {
                    using (var wb = new XLWorkbook())
                    {
                        foreach (var item in excels)
                        {
                            int row = 1;
                            var ws = wb.Worksheets.Add(item.WorksheetsName);
                            ws.Style.Font.FontSize = 13;

                            int startFormulas = 0;
                            int endFormulas = 0;
                            decimal sumCount = 0; /// รายการ

                            #region Title
                            if (item.ExcelTitle.Any())
                            {
                                foreach (var valData in item.ExcelTitle)
                                {
                                    List<ExcelTextStyleModel> bodyData = valData.TextList;

                                    for (int i = 0; i < bodyData.Count; i++)
                                    {
                                        var excelTitle = bodyData[i];
                                        var column = i + 1;

                                        if (excelTitle.Mergemodel != null)
                                        {
                                            ws.Range(excelTitle.Mergemodel.FirstCellRow,
                                                excelTitle.Mergemodel.FirstCellColumn,
                                                excelTitle.Mergemodel.LastCellRow,
                                                excelTitle.Mergemodel.LastCellColumn)
                                                .Merge();
                                        }

                                        ws.Cell(row, column).Value = excelTitle.Text;

                                        ws.Cell(row, column).Style.Alignment.SetHorizontal(excelTitle.AlignBodyColumn);

                                        if (excelTitle.Bold)
                                        {
                                            ws.Cell(row, column).Style.Font.Bold = excelTitle.Bold;
                                        }
                                    }

                                    row++;
                                    ws.Rows($"{row}").AdjustToContents();
                                }
                            }
                            #endregion

                            #region SetHaeder
                            if (item.ExcelHeader.Any())
                            {
                                SetLayOutExcelBody(ws, row, item.ExcelHeader);

                                row++;
                            }
                            #endregion

                            #region SetBody
                            startFormulas = row;
                            //if (item.ExcelBodyReportTransaction.Any())
                            if (item.ExcelBodyReportTransactionEdit.Any())
                                {
                                //var ele = item.ExcelBodyReportTransaction;
                                var ele = item.ExcelBodyReportTransactionEdit;

                                List<string> numbers = new();
                                for (int i = 1; i <= ele.Count; i++)
                                {
                                    numbers.Add(i.ToString());
                                }

                                //List<string> fullNameList = ele.Select(x => $"{x.DebtorNameTh} {x.DebtorSnameTh}").ToList();
                                //List<string> staffType = ele.Select(x => $"{x.StaffTypeName}").ToList();
                                //List<string> contractNo = ele.Select(x => $"{x.ContractNo}").ToList();
                                //List<decimal> loanAmount = ele.Select(x => x.ContractLoanAmount ?? 0).ToList();
                                //List<string> installmentNo = ele.Select(x => $"{x.InstallmentNo}/{x.ContractLoanNumInstallments}").ToList();
                                //List<decimal> principleAmount = ele.Select(x => x.PrincipleAmount ?? 0).ToList();
                                //List<decimal> interestAmont = ele.Select(x => x.InterestAmont ?? 0).ToList();
                                //List<decimal> totalAmount = ele.Select(x => x.TotalAmount ?? 0).ToList();

                                List<string> fullNameList = ele.Select(x => $"{x.Transaction.DebtorNameTh} {x.Transaction.DebtorSnameTh}").ToList();
                                List<string> staffType = ele.Select(x => $"{x.Transaction.StaffTypeName}").ToList();
                                List<string> contractNo = ele.Select(x => $"{x.Transaction.ContractNo}").ToList();
                                List<decimal> loanAmount = ele.Select(x => x.Transaction.ContractLoanAmount ?? 0).ToList();
                                List<string> installmentNo = ele.Select(x => $"{x.Transaction.InstallmentNo}/{x.Transaction.ContractLoanNumInstallments}").ToList();
                                List<decimal> principleAmount = ele.Select(x => x.Transaction.PrincipleAmount ?? 0).ToList();
                                List<decimal> interestAmont = ele.Select(x => x.Transaction.InterestAmont ?? 0).ToList();
                                List<decimal> totalAmount = ele.Select(x => x.Transaction.TotalAmount ?? 0).ToList();

                                var styleAmount = wb.Style;
                                styleAmount.Font.FontSize = 13;
                                styleAmount.NumberFormat.Format = "#,##0.00";
                                styleAmount.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                                ws.Cell(row, 1).InsertData("");
                                ws.Cell(row, 2).InsertData(contractNo);
                                ws.Cell(row, 3).InsertData(ele.Select(x => $"{x.Transaction.DebtorStaffId}").ToList());
                                ws.Cell(row, 4).InsertData(ele.Select(x => $"{x.Transaction.DebtorNameTh}").ToList());
                                ws.Cell(row, 5).InsertData(ele.Select(x => $"{x.Transaction.DebtorSnameTh}").ToList());
                                ws.Cell(row, 6).InsertData(ele.Select(x => $"{x.Transaction.LoanTypeName}").ToList());
                                ws.Cell(row, 7).InsertData(ele.Select(x => x.Transaction.BalanceAmount).ToList());
                                ws.Cell(row, 8).InsertData(ele.Select(x => dateService.ChangeDate(x.Transaction.DueDate, "d/MM/yyyy", Utility.DateLanguage_TH)).ToList());
                                ws.Cell(row, 9).InsertData(ele.Select(x => $"{x.Transaction.ContractLoanAmount}").ToList());
                                ws.Cell(row, 10).InsertData(ele.Select(x => $"{x.Transaction.ContractLoanNumInstallments}").ToList());
                                ws.Cell(row, 11).InsertData(ele.Select(x => $"{x.Transaction.InstallmentNo}").ToList());
                                ws.Cell(row, 12).InsertData(ele.Select(x => $"{x.Transaction.PrincipleAmount}").ToList());
                                ws.Cell(row, 13).InsertData(ele.Select(x => $"{x.Transaction.InterestAmont}").ToList());
                                ws.Cell(row, 14).InsertData(ele.Select(x => $"{x.Transaction.TotalAmount}").ToList());
                                ws.Cell(row, 15).InsertData(ele.Select(x => $"{x.Transaction.ValidationTransaction}").ToList());
                                ws.Cell(row, 16).InsertData(ele.Select(x => dateService.ChangeDate(x.Transaction.PaidDate, "d/MM/yyyy", Utility.DateLanguage_TH)).ToList());
                                ws.Cell(row, 17).InsertData(ele.Select(x => x.Transaction.CurrentStatusId == 98 || x.Transaction.CurrentStatusId == 99 ? "สิ้นสุดสัญญา" : "").ToList());
                                ws.Cell(row, 18).InsertData(ele.Select(x => x.IsEditBalanceAmount ? "แก้ไขข้อมมูล" : "").ToList());

                                row += ele.Count;
                            }
                            else if (item.ExcelBodyDebtorRegister.Any())
                            {
                                var tt = item.ExcelBodyDebtorRegister
                                    .Take(20)
                                    .ToList();
                                //for (int i = 0; i < item.ExcelBodyDebtorRegister.Count; i++)
                                //var maxCount = item.ExcelBodyDebtorRegister.Count;
                                var maxCount = tt.Count;

                                Console.WriteLine($"data จริง : {item.ExcelBodyDebtorRegister.Count}");

                                for (int i = 0; i < maxCount; i++)
                                {
                                    //var ele = item.ExcelBodyDebtorRegister[i];
                                    var ele = tt[i];
                                    int index = (i + 1);
                                    var dataSet_1 = GetBodyExcelDebtorRegister(ele.ReportTransaction, index);

                                    SetLayOutExcelBody(ws, row, dataSet_1);

                                    row++;

                                    Console.WriteLine($"data: {index}/{maxCount}");
                                    if (ele.TransactionList.Any(x => x.ValidationTransaction == "Y"))
                                    {
                                        var resultTransactions = ele.TransactionList
                                            .Where(x => x.ValidationTransaction == "Y")
                                            .ToList();

                                        foreach (var transaction in resultTransactions)
                                        {
                                            var dataSet_2 = GetBodyExcelDebtorRegisterTransaction(transaction);

                                            SetLayOutExcelBody(ws, row, dataSet_2);

                                            row++;
                                        }
                                    }

                                }
                            }
                            else if (item.ExcelBodyTypeDataByStaff.Any())
                            {
                                var ele = item.ExcelBodyTypeDataByStaff;

                                sumCount = ele.Select(x => x.Count).ToList().Sum(c => c);

                                List<string> dataName = new();
                                List<string> count = ele.Select(x => $"{x.Count}").ToList();
                                List<decimal> principleAmount = ele.Select(x => x.SumPrincipleAmount ?? 0).ToList();
                                List<decimal> interestAmont = ele.Select(x => x.SumInterestAmont ?? 0).ToList();
                                List<decimal> totalAmount = ele.Select(x => x.SumTotalAmount ?? 0).ToList();

                                switch (item.SheetType)
                                {
                                    case 1:
                                        dataName = ele.Select(x => x.LoanTypeName).ToList();
                                        break;

                                    case 2:
                                        dataName = ele.Select(x => x.StaffTypeName).ToList();
                                        break;
                                }

                                var styleAmount = wb.Style;
                                styleAmount.Font.FontSize = 13;
                                styleAmount.NumberFormat.Format = "#,##0.00";
                                styleAmount.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                                ws.Cell(row, 1).InsertData(dataName);
                                ws.Cell(row, 2).InsertData(count);
                                ws.Cell(row, 3).InsertData(principleAmount).Style = styleAmount;
                                ws.Cell(row, 4).InsertData(interestAmont).Style = styleAmount;
                                ws.Cell(row, 5).InsertData(totalAmount).Style = styleAmount;

                                row += ele.Count;
                            }

                            endFormulas = row - 1;
                            #endregion

                            #region Footer
                            switch (item.SheetType)
                            {
                                case 1: //สรุปประเภทสวัสดิการ
                                    List<ExcelTextStyleModel> bodyData1 = new()
                                    {
                                        new()
                                        {
                                            Text = "รวมจำนวนเงินทุกประเภทสวัสดิการ",
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                                        },
                                        new()
                                        {
                                            Text = $"{sumCount}",
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                                        },
                                        new()
                                        {
                                            Formulas = new FormulasModel()
                                            {
                                                Formulas = $"SUM(C{startFormulas}:C{endFormulas})",
                                                NumberFormat = "#,##0.00",
                                            },
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                        },
                                        new()
                                        {
                                            Formulas = new FormulasModel()
                                            {
                                                Formulas = $"SUM(D{startFormulas}:D{endFormulas})",
                                                NumberFormat = "#,##0.00",
                                            },
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                        },
                                        new()
                                        {
                                            Formulas = new FormulasModel()
                                            {
                                                Formulas = $"SUM(E{startFormulas}:E{endFormulas})",
                                                NumberFormat = "#,##0.00",
                                            },
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                        },
                                    };
                                    SetLayOutExcelBody(ws, row, bodyData1);

                                    row++;
                                    ws.Rows($"{row}").AdjustToContents();
                                    break;

                                case 2: //สรุปประเภทบุคลากร
                                    List<ExcelTextStyleModel> bodyData2 = new()
                                    {
                                        new()
                                        {
                                            Text = "รวมจำนวนเงินทุกประเภทบุคลากร",
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                                        },
                                        new()
                                        {
                                            Text = $"{sumCount}",
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                                        },
                                        new()
                                        {
                                            Formulas = new FormulasModel()
                                            {
                                                Formulas = $"SUM(C{startFormulas}:C{endFormulas})",
                                                NumberFormat = "#,##0.00",
                                            },
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                        },
                                        new()
                                        {
                                            Formulas = new FormulasModel()
                                            {
                                                Formulas = $"SUM(D{startFormulas}:D{endFormulas})",
                                                NumberFormat = "#,##0.00",
                                            },
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                        },
                                        new()
                                        {
                                            Formulas = new FormulasModel()
                                            {
                                                Formulas = $"SUM(E{startFormulas}:E{endFormulas})",
                                                NumberFormat = "#,##0.00",
                                            },
                                            Bold = true,
                                            AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                        },
                                    };
                                    SetLayOutExcelBody(ws, row, bodyData2);

                                    row++;
                                    ws.Rows($"{row}").AdjustToContents();
                                    break;

                                case 3: //รายละเอียดการหักชำระสวัสดิการเงินกู้บุคลากรมหาวิทยาลัยสงขลานครินทร์
                                    //List<ExcelTextStyleModel> bodyData3 = new()
                                    //{
                                    //    new()
                                    //    {
                                    //        Text = "",
                                    //        FixColumn = 6,
                                    //        Mergemodel = new()
                                    //        {
                                    //            FirstCellColumn = 1,
                                    //            LastCellColumn = 6,
                                    //            FirstCellRow = row,
                                    //            LastCellRow = row,
                                    //        },
                                    //    },
                                    //    new()
                                    //    {
                                    //        Formulas = new FormulasModel()
                                    //        {
                                    //            Formulas = $"SUM(G{startFormulas}:G{endFormulas})",
                                    //            NumberFormat = "#,##0.00",
                                    //        },
                                    //        Bold = true,
                                    //        AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                    //        FixColumn = 7,
                                    //    },
                                    //    new()
                                    //    {
                                    //        Formulas = new FormulasModel()
                                    //        {
                                    //            Formulas = $"SUM(H{startFormulas}:H{endFormulas})",
                                    //            NumberFormat = "#,##0.00",
                                    //        },
                                    //        Bold = true,
                                    //        AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                    //        FixColumn = 8,
                                    //    },
                                    //    new()
                                    //    {
                                    //        Formulas = new FormulasModel()
                                    //        {
                                    //            Formulas = $"SUM(I{startFormulas}:I{endFormulas})",
                                    //            NumberFormat = "#,##0.00",
                                    //        },
                                    //        Bold = true,
                                    //        AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                                    //        FixColumn = 9,
                                    //    },
                                    //};
                                    //SetLayOutExcelBody(ws, row, bodyData3);

                                    //row++;
                                    //ws.Rows($"{row}").AdjustToContents();
                                    break;
                            }
                            #endregion
                        }

                        wb.SaveAs(XLSStream);
                    }

                    byte[] bytes = XLSStream.ToArray();
                    XLSStream.Close();

                    await SaveFileAndImgService.SaveFileAsPath(bytes, fileName, "data:application/vnd.ms-excel;base64,");
                }
                catch (Exception ex)
                {
                    XLSStream.Close();
                    Loading = false;
                    await Task.Delay(1);
                    _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });

                    StateHasChanged();
                }
            }
        }

        private void SetLayOutExcelBody(IXLWorksheet ws, int row, List<ExcelTextStyleModel> bodyData)
        {
            for (int x = 0; x < bodyData.Count; x++)
            {
                var body = bodyData[x];
                var column = body.FixColumn ?? x + 1;

                if (body.Mergemodel != null)
                {
                    ws.Range(body.Mergemodel.FirstCellRow,
                        body.Mergemodel.FirstCellColumn,
                        body.Mergemodel.LastCellRow,
                        body.Mergemodel.LastCellColumn)
                        .Merge();
                }

                if (body.Formulas != null && !string.IsNullOrEmpty(body.Formulas.Formulas))
                {
                    var cell = ws.Cell(row, column);
                    cell.FormulaA1 = body.Formulas.Formulas;

                    if (!string.IsNullOrEmpty(body.Formulas.NumberFormat))
                    {
                        cell.Style.NumberFormat.Format = body.Formulas.NumberFormat;
                    }
                }
                else
                {
                    ws.Cell(row, column).Value = body.Text;
                }

                if (body.Mergemodel != null && body.Border)
                {
                    ws = Utility.SetStyleBorderExcle(body.Mergemodel.FirstCellRow, body.Mergemodel.FirstCellColumn, body.Mergemodel.LastCellRow, body.Mergemodel.LastCellColumn, ws);
                }
                else if (body.Border)
                {
                    ws = Utility.SetStyleBorderExcle(row, column, row, column, ws);
                }

                if (body.Mergemodel != null && body.Bold)
                {
                    ws.Range(body.Mergemodel.FirstCellRow, body.Mergemodel.FirstCellColumn, body.Mergemodel.LastCellRow, body.Mergemodel.LastCellColumn).Style.Font.Bold = body.Bold;
                }
                else if (body.Bold)
                {
                    ws.Range(row, column, row, column).Style.Font.Bold = body.Bold;
                }

                ws.Cell(row, column).Style.Alignment.SetHorizontal(body.AlignBodyColumn);

                if (body.WrapText)
                {
                    ws.Style.Alignment.WrapText = body.WrapText;
                }

                ws.Style.Alignment.SetVertical(body.AlignmentVertical);
                ws.Columns().AdjustToContents();
            }
        }

        private List<ExcelTextStyleModel> GetBodyExcelDebtorRegisterTransaction(VReportTransaction ele)
        {
            List<ExcelTextStyleModel> textStyleModels = new()
            {
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new(){ Text = $"",},
                new()
                {
                    Text = $"{ele.InstallmentNo}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{dateService.ChangeDate(ele.DueDate, "d/MM/yyyy", Utility.DateLanguage_TH)}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{ele.ReferenceId1}",
                    WrapText = true,
                    //AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", ele.PrincipleAmount))}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{string.Format("{0:n2}", ele.InterestAmont)}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new(){ Text = $"", },
            };

            return textStyleModels;
        }

        private List<ExcelTextStyleModel> GetBodyExcelDebtorRegister(VReportTransaction ele, int index)
        {
            List<ExcelTextStyleModel> textStyleModels = new()
            {
                new()
                {
                    Text = $"{index}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{dateService.ChangeDate(ele.DueDate, "d/MM/yyyy", Utility.DateLanguage_TH)}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{ele.DebtorNameTh} {ele.DebtorSnameTh}",
                    WrapText = true,
                    //AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{ele.ContractNo}",
                    WrapText = true,
                    //AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{ele.LoanTypeName}",
                    WrapText = true,
                    //AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{ele.DebtorFacNameThai}",
                    WrapText = true,
                    //AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{String.Format("{0:n2}", ele.ContractLoanAmount)}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{ele.ContractLoanNumInstallments}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{String.Format("{0:n2}", ele.ContractLoanInterest)}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{String.Format("{0:n2}", ele.ContractLoanInstallment)}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new(){ Text = $"", },
                new(){ Text = $"", },
                new(){ Text = $"", },
                new(){ Text = $"", },
                new(){ Text = $"", },
                new()
                {
                    Text = $"{String.Format("{0:n2}", ele.BalanceAmount)}",
                    WrapText = true,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
            };

            return textStyleModels;
        }

        private List<ExcelTextStyleModel> GetBodyExcelLoanData(TypeDataByStaffModel ele, int index, TypeDataByStaffModel val)
        {
            List<ExcelTextStyleModel> textStyleModels = new()
            {
                new()
                {
                    Text = $"{index}. {ele.LoanTypeName}",
                    WrapText = true,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n0}", val.Count))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", val.SumPrincipleAmount))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", val.SumInterestAmont))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", val.SumTotalAmount))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
            };

            return textStyleModels;
        }

        private List<ExcelTextStyleModel> GetBodyExcelTypeData(TypeDataByStaffModel ele, int index, TypeDataByStaffModel val)
        {
            List<ExcelTextStyleModel> textStyleModels = new()
            {
                new()
                {
                    Text = $"{index}. {ele.StaffTypeName}",
                    WrapText = true,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n0}", val.Count))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", val.SumPrincipleAmount))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", val.SumInterestAmont))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", val.SumTotalAmount))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
            };

            return textStyleModels;
        }

        private List<ExcelTextStyleModel> GetBodyExcelPaymentData(VReportTransaction ele, int index)
        {
            List<ExcelTextStyleModel> textStyleModels = new()
            {
                new()
                {
                    Text = $"{index}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{ele.DebtorNameTh} {ele.DebtorSnameTh}",
                    WrapText=true,
                },
                new()
                {
                    Text = $"{ele.StaffTypeName}",
                    WrapText=true,
                },
                new()
                {
                    Text = $"{ele.ContractNo}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", ele.ContractLoanAmount))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{ele.InstallmentNo}/{ele.ContractLoanNumInstallments}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", ele.PrincipleAmount))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", ele.InterestAmont))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
                new()
                {
                    Text = $"{(string.Format("{0:n2}", ele.TotalAmount))}",
                    AlignBodyColumn = XLAlignmentHorizontalValues.Right,
                },
            };

            return textStyleModels;
        }

        /// <summary>
        /// สรุปประเภทสวัสดิการ
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="sheetType"></param>
        /// <returns></returns>
        private LoanAgreementExportExcel SetLoanData(List<TypeDataByStaffModel> datas, int sheetType)
        {
            //List<ExcelTextStyleModel> textStyleModels = new();

            //for (int i = 0; i < datas.Count; i++)
            //{
            //    var ele = datas[i];
            //    int no = i + 1;
            //    TypeDataByStaffModel? val = datas.Find(c => c.LoanTypeId == ele.LoanTypeId);

            //    if (val != null)
            //    {
            //        ExcelTextStyleModel excelText = new()
            //        {
            //            TextList = GetBodyExcelLoanData(ele, no, val)
            //        };

            //        textStyleModels.Add(excelText);
            //    }
            //}

            LoanAgreementExportExcel schExcel = new()
            {
                SheetType = sheetType,
                ExcelTitle = new()
                {
                    new()
                    {
                        TextList = new()
                        {
                            new()
                            {
                                Mergemodel = new()
                                {
                                    FirstCellColumn = 1,
                                    LastCellColumn = 5,
                                    FirstCellRow = 1,
                                    LastCellRow = 1,
                                },
                                Text = $"สรุปประเภทสวัสดิการ " +
                                $"เดือน {dateService.ChangeDate(StartDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} " +
                                $"ถึง {dateService.ChangeDate(EndDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} ",
                                AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                            }
                        }
                    },
                },
                ExcelHeader = new()
                {
                    new()
                    {
                        Text = "สรุปประเภทสวัสดิการ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "รายการ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "เงินต้น",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ดอกเบี้ย",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "รวมจำนวนเงิน",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                },
                //ExcelBody = textStyleModels,
                ExcelBodyTypeDataByStaff = datas,
                WorksheetsName = "ประเภทสวัสดิการ"
            };

            return schExcel;
        }

        /// <summary>
        /// สรุปประเภทบุคลากร
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="sheetType"></param>
        /// <returns></returns>
        private LoanAgreementExportExcel SetTypeData(List<TypeDataByStaffModel> datas, int sheetType)
        {
            //List<ExcelTextStyleModel> textStyleModels = new();

            //for (int i = 0; i < datas.Count; i++)
            //{
            //    var ele = datas[i];
            //    int no = i + 1;
            //    TypeDataByStaffModel? val = datas.Find(c => c.StaffType == ele.StaffType);

            //    if (val != null)
            //    {
            //        ExcelTextStyleModel excelText = new()
            //        {
            //            TextList = GetBodyExcelTypeData(ele, no, val)
            //        };

            //        textStyleModels.Add(excelText);
            //    }
            //}

            LoanAgreementExportExcel schExcel = new()
            {
                SheetType = sheetType,
                ExcelTitle = new()
                {
                    new()
                    {
                        TextList = new()
                        {
                            new()
                            {
                                Mergemodel = new()
                                {
                                    FirstCellColumn = 1,
                                    LastCellColumn = 5,
                                    FirstCellRow = 1,
                                    LastCellRow = 1,
                                },
                                Text = $"สรุปประเภทบุคลากร " +
                                $"เดือน {dateService.ChangeDate(StartDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} " +
                                $"ถึง {dateService.ChangeDate(EndDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} ",
                                AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                            }
                        }
                    },
                },
                ExcelHeader = new()
                {
                    new()
                    {
                        Text = "สรุปประเภทบุคลากร",
                         Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "รายการ",
                         Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "เงินต้น",
                         Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ดอกเบี้ย",
                         Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "รวมจำนวนเงิน",
                         Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                },
                //ExcelBody = textStyleModels,
                ExcelBodyTypeDataByStaff = datas,
                WorksheetsName = "ประเภทบุคลากร"
            };

            return schExcel;
        }

        /// <summary>
        /// รายละเอียดการหักชำระสวัสดิการเงินกู้บุคลากรมหาวิทยาลัยสงขลานครินทร์
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="sheetType"></param>
        /// <returns></returns>
        private LoanAgreementExportExcel SetPaymentData(List<VReportTransactionEditModel> datas, int sheetType)
        {
            string? campId = string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow) || StateProvider?.CurrentUser.CapmSelectNow == "00" ?
                null : StateProvider!.CurrentUser.CapmSelectNow;

            string? cameNameTh = !string.IsNullOrEmpty(campId) ? CampusDict[campId].CampNameThai : null;

            LoanAgreementExportExcel schExcel = new()
            {
                SheetType = sheetType,
                ExcelTitle = new()
                {
                    new()
                    {
                        TextList = new()
                        {
                            new()
                            {
                                Mergemodel = new()
                                {
                                    FirstCellColumn = 1,
                                    //LastCellColumn = 9,
                                    LastCellColumn = 14,
                                    FirstCellRow = 1,
                                    LastCellRow = 1,
                                },
                                Text = $"รายละเอียดการหักชำระสวัสดิการเงินกู้บุคลากรมหาวิทยาลัยสงขลานครินทร์ {(!string.IsNullOrEmpty(cameNameTh) ? cameNameTh + " " : "")}",
                                AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                            }
                        }
                    },
                    new()
                    {
                        TextList = new()
                        {
                            new()
                            {
                                Mergemodel = new()
                                {
                                    FirstCellColumn = 1,
                                    //LastCellColumn = 9,
                                    LastCellColumn = 14,
                                    FirstCellRow = 2,
                                    LastCellRow = 2,
                                },
                                Text = $"เดือน {dateService.ChangeDate(StartDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} " +
                                $"ถึง {dateService.ChangeDate(EndDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} ",
                                AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                            }
                        }
                    },
                },
                ExcelHeader = new()
                {
                    new()
                    {
                        Text = "คก.",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "สัญญาเงินกู้",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "รหัสบุคลากร",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ชื่อ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "สกุล",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ประเภทยืม",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ยอดคงเหลือ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "วันที่รายงาน",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "จำนวนเงินยืม",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "จำนวนงวด",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "งวดที่จ่าย",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "เงินต้น",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ดอกเบี้ย",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "รวมหัก",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "สถานะการจ่ายเงิน Y = จ่ายแล้ว",
                        Bold = false,
                        Border = false,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "วันที่กองคลัง โอนเงิน",
                        Bold = false,
                        Border = false,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    //new()
                    //{
                    //    Text = "ลำดับ",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "ชื่อ-นามสกุล",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "ประเภทบุคลากร",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "เลขที่สัญญา",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "จำนวนเงินกู้",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "จำนวนงวดผ่อน",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "ชำระเงินต้น",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "ดอกเบี้ยจ่าย",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                    //new()
                    //{
                    //    Text = "รวมจำนวนเงิน",
                    //    Bold = true,
                    //    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    //},
                },
                //ExcelBodyReportTransaction = datas,
                ExcelBodyReportTransactionEdit = datas,
                WorksheetsName = "การหักชำระ"
            };

            return schExcel;
        }

        /// <summary>
        /// ทะเบียนลูกหนี้
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sheetType"></param>
        /// <returns></returns>
        private LoanAgreementExportExcel SetDebtorRegister(List<DebtorRegisterModel> datas, int sheetType)
        {
            string? campId = string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow) || StateProvider?.CurrentUser.CapmSelectNow == "00" ?
                null : StateProvider!.CurrentUser.CapmSelectNow;

            string? cameNameTh = !string.IsNullOrEmpty(campId) ? CampusDict[campId].CampNameThai : null;

            LoanAgreementExportExcel schExcel = new()
            {
                SheetType = sheetType,
                ExcelTitle = new()
                {
                    new()
                    {
                        TextList = new()
                        {
                            new()
                            {
                                Mergemodel = new()
                                {
                                    FirstCellColumn = 1,
                                    LastCellColumn = 16,
                                    FirstCellRow = 1,
                                    LastCellRow = 1,
                                },
                                Text = $"มหาวิทยาลัย สงขลานครินทร์ {(!string.IsNullOrEmpty(cameNameTh) ? cameNameTh + " " : "")}",
                                AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                            }
                        }
                    },
                    new()
                    {
                        TextList = new()
                        {
                            new()
                            {
                                Mergemodel = new()
                                {
                                    FirstCellColumn = 1,
                                    LastCellColumn = 16,
                                    FirstCellRow = 3,
                                    LastCellRow = 3,
                                },
                                Text = $"ลูกหนี้เงินกู้สวัสดิการ",
                                AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                            }
                        }
                    },
                    new()
                    {
                        TextList = new()
                        {
                            new()
                            {
                                Mergemodel = new()
                                {
                                    FirstCellColumn = 1,
                                    LastCellColumn = 16,
                                    FirstCellRow = 2,
                                    LastCellRow = 2,
                                },
                                Text = $"เดือน {dateService.ChangeDate(StartDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} " +
                                $"ถึง {dateService.ChangeDate(EndDateTemp, "MMMM yyyy", Utility.DateLanguage_TH)} ",
                                AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                            }
                        }
                    },
                },
                ExcelHeader = new()
                {
                    new()
                    {
                        Text = "ลำดับ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "วันที่จ่ายเงิน",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ชื่อผู้กู้",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "เลขที่สัญญา",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ประเภทสวัสดิการเงินกู้",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "หน่วยงาน",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "จำนวนเงินกู้",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "จำนวนงวด",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ดอกเบี้ย(%)",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ชำระงวดละ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ชำระงวดที่",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "วันที่รับชำระ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "เลขที่ใบเสร็จรับเงิน",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "จำนวนเงิน",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "ดอกเบี้ย",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                    new()
                    {
                        Text = "คงเหลือ",
                        Bold = true,
                        AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                    },
                },
                ExcelBodyDebtorRegister = datas,
                WorksheetsName = "ลูกหนี้"
            };

            return schExcel;
        }
    }

    public class LoanAgreementExportExcel
    {
        /// <summary>
        /// 1 = ข้อมูล กู้เงินสวัสดิการของบุคลากร || 2 = ข้อมูล ประเภทของบุคลากร || 3 = รายละเอียดการหักชำระสวัสดิการเงินกู้บุคลากร
        /// </summary>
        public int SheetType { get; set; } = 0;
        public string WorksheetsName { get; set; } = string.Empty;
        /// <summary>
        /// หัวข้อใน excel
        /// </summary>
        public List<ExcelTextStyleModel> ExcelTitle { get; set; } = new();
        /// <summary>
        /// ข้อมูลในตราง header
        /// </summary>
        public List<ExcelTextStyleModel> ExcelHeader { get; set; } = new();

        //public List<ExcelTextStyleModel> ExcelBody { get; set; } = new()
        public List<TypeDataByStaffModel> ExcelBodyTypeDataByStaff { get; set; } = new();
        //public List<VReportTransaction> ExcelBodyReportTransaction { get; set; } = new()
        public List<VReportTransactionEditModel> ExcelBodyReportTransactionEdit { get; set; } = new();
        public List<DebtorRegisterModel> ExcelBodyDebtorRegister { get; set; } = new();

        public decimal? FormGroupType { get; set; } = null;
    }
}
