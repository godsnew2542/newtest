using ClosedXML.Excel;
using LoanApp.Components.AdminOption;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace LoanApp.Pages.Admin.ExportFile;

public partial class LoanAgreementGenFileImport
{
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #region Inject
    [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

    #endregion

    private ReportLoanForMonth? ReportLoanMonthRef { get; set; }
    private Dictionary<string, CCampus> CampusDict { get; set; } = new();
    private List<VReportTransaction> ReportData { get; set; } = new();

    private DateTime? StartDate { get; set; } // new DateTime(2022, 5, 1)
    private DateTime? EndDate { get; set; } // new DateTime(2022, 5, 30)
    private DateTime? StartDateTemp { get; set; } = null;
    private DateTime? EndDateTemp { get; set; } = null;
    private bool Loading { get; set; } = false;
    private bool isKeepLoanSuccess { get; set; } = false;
    private string? disPlayLoanSuccess { get; set; } = null;

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

    private async Task PerViewData()
    {
        ReportData = new();
        StartDateTemp = null;
        EndDateTemp = null;
        disPlayLoanSuccess = null;

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

        #endregion

        Loading = true;
        await Task.Delay(1);
        StateHasChanged();

        StartDateTemp = StartDate;
        EndDateTemp = EndDate;

        try
        {
            ReportData = await psuLoan.GetListVReportTransactionByExportFileLoanAgreement(StartDate, EndDate, StateProvider?.CurrentUser.CapmSelectNow, isKeepLoanSuccess);

            disPlayLoanSuccess = isKeepLoanSuccess ? "ต้องการข้อมมูลที่สิ้นสุดไปแล้ว" : "ไม่ต้องการข้อมมูลที่สิ้นสุดไปแล้ว";

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
        Loading = true;
        await Task.Delay(1);
        StateHasChanged();

        List<LoanAgreementExportExcel> LoanExportExcels = new();
        List<VLoanStaffDetail> staffData = new();

        try
        {
            if (ReportLoanMonthRef != null)
            {
                var data = ReportLoanMonthRef.ResultDataUI();

                LoanExportExcels.Add(SetPaymentData(data, 3));

                List<string?> staffIdList = data
                    .Where(x => !string.IsNullOrEmpty(x.Transaction.DebtorStaffId))
                    .Select(c => c.Transaction.DebtorStaffId)
                    .Distinct()
                    .ToList();


                staffData = await psuLoan.GetListVLoanStaffDetailById(staffIdList);
            }


            await ExportFileExcel(LoanExportExcels, staffData);

            Loading = false;
            await Task.Delay(1);
            StateHasChanged();

            await notificationService.SuccessDefult("ดาวน์โหลดข้อมูลสำเสร็จ");
        }
        catch (Exception)
        {
            throw;
        }
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
                new()
                {
                    Text = "เลขประจำตัวประชาชน",
                    Bold = false,
                    Border = false,
                    AlignBodyColumn = XLAlignmentHorizontalValues.Center,
                },
            },
            ExcelBodyReportTransactionEdit = datas,
            WorksheetsName = "การหักชำระ"
        };

        return schExcel;
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

    private async Task ExportFileExcel(List<LoanAgreementExportExcel> excels, List<VLoanStaffDetail> staffData)
    {
        string fileName = $"หักชำระเงินกู้รายเดือน_{dateService.ChangeDate(DateTime.Now, "dMMyyyy", Utility.DateLanguage_TH)}.xlsx";
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
                        if (item.ExcelBodyReportTransactionEdit.Any())
                        {
                            var ele = item.ExcelBodyReportTransactionEdit;

                            List<string> numbers = new();
                            for (int i = 1; i <= ele.Count; i++)
                            {
                                numbers.Add(i.ToString());
                            }

                            List<string> contractNo = ele.Select(x => $"{x.Transaction.ContractNo}").ToList();
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
                            ws.Cell(row, 8).InsertData(ele
                                .Select(x => dateService
                                .ChangeDate(x.Transaction.DueDate, "d/MM/yyyy", Utility.DateLanguage_TH))
                                .ToList());
                            ws.Cell(row, 9).InsertData(ele.Select(x => $"{x.Transaction.ContractLoanAmount}").ToList());
                            ws.Cell(row, 10).InsertData(ele.Select(x => $"{x.Transaction.ContractLoanNumInstallments}").ToList());
                            ws.Cell(row, 11).InsertData(ele.Select(x => $"{x.Transaction.InstallmentNo}").ToList());
                            ws.Cell(row, 12).InsertData(ele.Select(x => $"{x.Transaction.PrincipleAmount}").ToList());
                            ws.Cell(row, 13).InsertData(ele.Select(x => $"{x.Transaction.InterestAmont}").ToList());
                            ws.Cell(row, 14).InsertData(ele.Select(x => $"{x.Transaction.TotalAmount}").ToList());
                            ws.Cell(row, 15).InsertData(ele.Select(x => $"{x.Transaction.ValidationTransaction}").ToList());
                            ws.Cell(row, 16).InsertData(ele
                                .Select(x => dateService
                                .ChangeDate(x.Transaction.PaidDate, "d/MM/yyyy", Utility.DateLanguage_TH))
                                .ToList());
                            ws.Cell(row, 17).InsertData(ele
                                .Select(x => staffData
                                .Where(c => c.StaffId == x.Transaction.DebtorStaffId)
                                .Select(y => y.StaffPersId)
                                .FirstOrDefault())
                                .ToList());

                            #region เพิ่มเติม
                            ws.Cell(row, 18).InsertData(ele
                                .Select(x => x.Transaction.CurrentStatusId == 98 || x.Transaction.CurrentStatusId == 99 ? "สิ้นสุดสัญญา" : "")
                                .ToList());
                            ws.Cell(row, 19).InsertData(ele.Select(x => x.IsEditBalanceAmount ? "แก้ไขข้อมมูล" : "").ToList());

                            #endregion

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
}
