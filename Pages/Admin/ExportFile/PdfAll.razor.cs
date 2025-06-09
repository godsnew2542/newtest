using ClosedXML.Excel;
using LoanApp.Components.Document;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LoanApp.Pages.Admin.ExportFile;

public partial class PdfAll
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider stateProvider { get; set; } = null!;

    #endregion

    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

    private InfoModel Info { get; set; } = new();
    private List<ReportAdminModel> ListReportAdmin { get; set; } = new();
    private List<ReportAdminModel> ListReportAdminV2 { get; set; } = new();
    private List<ReportAdminModel> listReportResultDev { get; set; } = new();
    private ReportAdmin RefReportAdmin { get; set; } = new();

    private string ReportAttrachmentHTML { get; set; } = string.Empty;

    private string StorageName { get; } = "ReportAdmin";
    private string Title { get; set; } = string.Empty;
    private bool IsMobile { get; set; } = false;
    private bool IsLoading { get; set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                InfoModel? checkData = await sessionStorage.GetItemAsync<InfoModel>(StorageName);

                if (checkData != null)
                {
                    Info = checkData;
                    string? fiscalString = null;
                    List<decimal> fiscalYears = CounntYear(Convert.ToInt32(Info.End), Convert.ToInt32(Info.Start));

                    List<FiscalReportDataAdminModel> fiscalReportDatas = new();

                    foreach (var fiscalYear in fiscalYears)
                    {
                        DateTime tempDate = new((int)fiscalYear, 1, 1);
                        List<ReportAdminModel> repost = await psuLoan.GetAllDataReportAdminForFiscal(tempDate, Info.CampId);

                        FiscalReportDataAdminModel fiscalReport = new()
                        {
                            FiscalYear = (int)fiscalYear,
                            ReportData = await userService.FindDataInFisicalYear(repost, fiscalYear)
                        };

                        if (fiscalString == null)
                        {
                            fiscalString = $"{fiscalReport.FiscalYear + 543}";
                        }
                        else
                        {
                            fiscalString += $", {fiscalReport.FiscalYear + 543}";
                        }

                        fiscalReportDatas.Add(fiscalReport);
                    }

                    foreach (var item in fiscalReportDatas)
                    {
                        var reportAdmin2 = await SetDataReport2(item, Info.SelectLoanTypeId);

                        ListReportAdminV2.Add(reportAdmin2);
                    }

                    if (ListReportAdmin.Any() || ListReportAdminV2.Any())
                    {
                        CCampus? Campus = EntitiesCentralService.GetCampus(Info.CampId);
                        Title = $"สรุปรายงานเข้าที่ประชุมคณะอนุกรรมการสวัสดิการฯ {Campus?.CampNameThai} ปีงบประมาณ {fiscalString}";
                    }
                }

                IsMobile = await JS.InvokeAsync<bool>("isDevice");

                IsLoading = true;

                StateHasChanged();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }
    }

    private async Task OpenPdfAsync()
    {
        ReportAttrachmentHTML = string.Empty;
        try
        {
            ReportAttrachmentHTML = await HtmlAsync();
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task DownloadPdfAsync()
    {
        try
        {
            int EB = 543;
            var fileName = $"สรุปรายงานประจำปี{Convert.ToInt32(Info.Start) + EB}-{Convert.ToInt32(Info.End) + EB}.pdf";
            var html = await HtmlAsync();
            if (!string.IsNullOrEmpty(html))
            {
                byte[] pdfBuffer = GeneratePDFService.GeneratePDF(html);
                await GeneratePDFService.SaveFilePDFAsync(pdfBuffer, fileName);
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    public async Task<string> HtmlAsync()
    {
        try
        {
            var Html = string.Empty;
            var HeadHTML = await JS.InvokeAsync<string>("headHTML");
            var ScriptHTML = await JS.InvokeAsync<string>("scriptHTML");
            var HtmlText = string.Empty;

            if (RefReportAdmin != null)
            {
                HtmlText = await RefReportAdmin.GetBoByHtmlAsync();

                if (!string.IsNullOrEmpty(HtmlText))
                {
                    Html = $"{HeadHTML} <br/> {HtmlText} <br/> {ScriptHTML}";
                }
            }
            return Html;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private List<decimal> CounntYear(int YEnd, int Ystart)
    {
        int CountBetween = YEnd - Ystart;
        List<decimal> YearBetween = new();
        for (int i = 0; i <= CountBetween; i++)
        {
            int year = Convert.ToInt32(Info.Start) + i;
            YearBetween.Add(Convert.ToDecimal(year));
        }

        return YearBetween;
    }

    private async Task<ReportAdminModel> SetDataReport(decimal? loanTypeId, List<FiscalReportDataAdminModel> models)
    {
        StaffTypeModel sType = new();
        ReportAdminModel reportAdmin = new()
        {
            LoanTypeId = loanTypeId,
            LoanTypeName = userService.GetLoanName((await psuLoan.GetLoanTypeAsync((byte?)loanTypeId))),
            BuddhistCalendar = GetFiscalYearList(models),
            CountGovernmentOfficer = CountStaffTypeByFiscalYear(models, sType.GovernmentOfficer, loanTypeId),
            CountUniversityStaff = CountStaffTypeByFiscalYear(models, sType.UniversityStaff, loanTypeId),
            CountIncomeEmployee = CountStaffTypeByFiscalYear(models, sType.IncomeEmployee, loanTypeId),
            CountEmployee = CountStaffTypeByFiscalYear(models, sType.Employee, loanTypeId),
            CountLoanAmount = CountLoanAmountByFiscalYear(models, loanTypeId)
        };

        reportAdmin.CountLoanAgreement = GetCountLoanAgreement(reportAdmin);

        reportAdmin.SumGovernmentOfficer = reportAdmin.CountGovernmentOfficer.Sum();
        reportAdmin.SumUniversityStaff = reportAdmin.CountUniversityStaff.Sum();
        reportAdmin.SumIncomeEmployee = reportAdmin.CountIncomeEmployee.Sum();
        reportAdmin.SumEmployee = reportAdmin.CountEmployee.Sum();
        reportAdmin.SumLoanAgreement = reportAdmin.CountLoanAgreement.Sum();
        reportAdmin.SumLoanAmount = reportAdmin.CountLoanAmount.Sum();

        return reportAdmin;
    }

    private async Task<ReportAdminModel> SetDataReport2(FiscalReportDataAdminModel data, List<decimal> loanTypeIdList)
    {
        List<ReportAdminModel> temp = new();
        StaffTypeModel sType = new();

        foreach (var loanTypeId in loanTypeIdList)
        {
            List<ReportAdminModel> reportData = data.ReportData.Where(x => x.LoanTypeId == loanTypeId).ToList();
            ReportAdminModel reportAdmin = new()
            {
                LoanTypeId = loanTypeId,
                LoanTypeName = userService.GetLoanName((await psuLoan.GetLoanTypeAsync((byte?)loanTypeId))),
                FiscalYears = data.FiscalYear,
                SumGovernmentOfficer = reportData.Count(x => sType.GovernmentOfficer.Contains(x.StaffType)),
                SumUniversityStaff = reportData.Count(x => sType.UniversityStaff.Contains(x.StaffType)),
                SumIncomeEmployee = reportData.Count(x => sType.IncomeEmployee.Contains(x.StaffType)),
                SumEmployee = reportData.Count(x => sType.Employee.Contains(x.StaffType)),
                SumLoanAgreement = reportData.Count,
                SumLoanAmount = (int)reportData.Where(x => x.LoanAmount != null).Sum(x => x.LoanAmount!.Value),
                TotalInterest = reportData.Where(x => x.TotalInterest != null).Sum(x => x.TotalInterest!.Value),
                TotalBalance = reportData.Where(x => x.TotalBalance != null).Sum(x => x.TotalBalance!.Value),
                TotalPayment = reportData.Where(x => x.TotalPayment != null).Sum(x => x.TotalPayment!.Value),
            };

            if (reportData.Any())
            {
                foreach (var item in reportData)
                {
                    item.LoanTypeName = reportAdmin.LoanTypeName;
                    listReportResultDev.Add(item);
                }
            }

            temp.Add(reportAdmin);
        }

        return new ReportAdminModel()
        {
            ReportAdminList = temp
        };
    }

    private void Back()
    {
        navigationManager.NavigateTo("/Admin/FilterReportAdmin");
    }

    private List<decimal> GetFiscalYearList(List<FiscalReportDataAdminModel> models)
    {
        List<decimal> fiscalYears = new();

        foreach (var item in models)
        {
            fiscalYears.Add(item.FiscalYear);
        }

        return fiscalYears;
    }

    private List<int> CountStaffTypeByFiscalYear(List<FiscalReportDataAdminModel> models, string[] staffType, decimal? loanTypeId)
    {
        List<int> result = new();

        foreach (var item in models)
        {
            var count = item.ReportData
                .Where(c => c.LoanTypeId == loanTypeId)
                .Where(c => staffType.Contains(c.StaffType))
                .Count();

            result.Add(count);
        }

        return result;
    }

    private List<int> GetCountLoanAgreement(ReportAdminModel report)
    {
        List<int> result = new();

        for (int i = 0; i < report.CountGovernmentOfficer.Count; i++)
        {
            var governmentOfficer = report.CountGovernmentOfficer[i];
            var universityStaff = report.CountUniversityStaff[i];
            var incomeEmployee = report.CountIncomeEmployee[i];
            var employee = report.CountEmployee[i];

            int count = governmentOfficer + universityStaff + incomeEmployee + employee;
            result.Add(count);
        }

        return result;
    }

    private List<int> CountLoanAmountByFiscalYear(List<FiscalReportDataAdminModel> models, decimal? loanTypeId)
    {
        List<int> result = new();

        foreach (var item in models)
        {
            decimal? count = item.ReportData
                .Where(c => c.LoanTypeId == loanTypeId)
                .Where(c => c.LoanAmount != null)
                .Sum(x => x.LoanAmount);

            result.Add((count == null ? 0 : (int)count));
        }

        return result;
    }

    private async Task ExportFileByExcel()
    {
        if (!ListReportAdminV2.Any())
        {
            return;
        }

        ExcelPdfAllModel data = setSheetImport(ListReportAdminV2);

        var wb = new XLWorkbook();

        foreach (ExcelBodyPdfAllModel item in data.excelBodys)
        {
            int row = 1;
            var ws = wb.Worksheets.Add(item.WorksheetsName);
            ws.Style.Font.FontSize = 14;
            int tempRow = 0;

            #region Title
            if (item.ExcelTitles.Any())
            {
                for (int titleCount = 0; titleCount < item.ExcelTitles.Count; titleCount++)
                {
                    var column = 0;
                    ExcelTextStyleModel excelTitle = item.ExcelTitles[titleCount];

                    if (!string.IsNullOrEmpty(excelTitle?.Text))
                    {
                        column = titleCount + 1;
                        Utility.setTitleExcel(excelTitle, column, row, ws);
                    }
                    else if (excelTitle!.TextList.Any())
                    {
                        for (int titleIndex = 0; titleIndex < excelTitle.TextList.Count; titleIndex++)
                        {
                            column = titleIndex + 1;
                            ExcelTextStyleModel subTitles = excelTitle.TextList[titleIndex];
                            ws = Utility.setTitleExcel(subTitles, column, row, ws);
                        }
                    }

                    row++;
                }
            }
            #endregion

            #region SetHeader
            if (item.ExcelHeader.Any())
            {
                tempRow = 0;
                for (int i = 0; i < item.ExcelHeader.Count; i++)
                {
                    ExcelTextStyleModel header = item.ExcelHeader[i];
                    var column1 = i + 1;
                    var headerText = !string.IsNullOrEmpty(header.Text) ? header.Text : string.Empty;

                    //if (header.Border)
                    //{
                    //    ws = Utility.SetStyleBorderExcle(row, column1, row, column1, ws);
                    //}
                    if (header.Mergemodel == null)
                    {
                        ws.Cell(row, column1).Value = headerText;
                        ws.Cell(row, column1).Style.Alignment.SetHorizontal(header.AlignBodyColumn);
                        ws.Cell(row, column1).Style.Protection.SetLocked(header.LockColumn);

                        if (!string.IsNullOrEmpty(header.Color))
                        {
                            ws.Cell(row, column1).Style.Fill.BackgroundColor = XLColor.FromHtml(header.Color);
                        }
                    }
                    else
                    {
                        ExcelMergemodel hMerge = header.Mergemodel!;

                        /// row
                        hMerge.FirstCellRow = hMerge.FirstCellRow == 0 ? row : hMerge.FirstCellRow;
                        hMerge.LastCellRow = hMerge.LastCellRow == 0 ? row : hMerge.LastCellRow;

                        ws.Range(hMerge.FirstCellRow, hMerge.FirstCellColumn, hMerge.LastCellRow, hMerge.LastCellColumn)
                            .Merge();

                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Value = headerText;
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Alignment.SetHorizontal(header.AlignBodyColumn);
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Protection.SetLocked(header.LockColumn);

                        if (!string.IsNullOrEmpty(header.Color))
                        {
                            ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(header.Color);
                        }

                        if (tempRow == 0)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                        else if (tempRow < hMerge.LastCellRow)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                    }
                }
                if (tempRow != 0)
                {
                    row = tempRow + 1;
                }
                else
                {
                    row++;
                }
            }
            #endregion

            #region SetBody
            if (item.ExcelBody.Any())
            {
                tempRow = 0;
                var ele = item.ExcelBody;

                List<string> loanTypeName = ele.Select(x => $"{x.LoanTypeName}").ToList();
                List<string> budgetYear = ele.Select(x => $"{(Convert.ToInt32(x.FiscalYears) + 543)}").ToList();
                List<string> sumGovernmentOfficer = ele.Select(x => $"{x.SumGovernmentOfficer}").ToList();
                List<string> sumUniversityStaff = ele.Select(x => $"{x.SumUniversityStaff}").ToList();
                List<string> sumIncomeEmployee = ele.Select(x => $"{x.SumIncomeEmployee}").ToList();
                List<string> sumEmployee = ele.Select(x => $"{x.SumEmployee}").ToList();
                List<string> sumLoanAgreement = ele.Select(x => $"{x.SumLoanAgreement}").ToList();
                List<string> sumLoanAmount = ele.Select(x => $"{(string.Format("{0:n2}", x.SumLoanAmount))}").ToList();
                List<string> totalInterest = ele.Select(x => $"{(string.Format("{0:n2}", x.TotalInterest))}").ToList();
                List<string> totalBalance = ele.Select(x => $"{(string.Format("{0:n2}", x.TotalBalance))}").ToList();
                List<string> totalPayment = ele.Select(x => $"{(string.Format("{0:n2}", x.TotalPayment))}").ToList();

                ws.Cell(row, 1).InsertData(loanTypeName);
                ws.Cell(row, 2).InsertData(budgetYear);
                ws.Cell(row, 3).InsertData(sumGovernmentOfficer);
                ws.Cell(row, 4).InsertData(sumUniversityStaff);
                ws.Cell(row, 5).InsertData(sumIncomeEmployee);
                ws.Cell(row, 6).InsertData(sumEmployee);
                ws.Cell(row, 7).InsertData(sumLoanAgreement);
                ws.Cell(row, 8).InsertData(sumLoanAmount);
                ws.Cell(row, 9).InsertData(totalInterest);
                ws.Cell(row, 10).InsertData(totalPayment);
                ws.Cell(row, 11).InsertData(totalBalance);

                row += ele.Count;
            }
            #endregion

            #region footer
            if (item.ExcelFooters.Any())
            {
                tempRow = 0;
                for (int i = 0; i < item.ExcelFooters.Count; i++)
                {
                    ExcelTextStyleModel footer = item.ExcelFooters[i];

                    var column1 = i + 1;
                    var headerText = !string.IsNullOrEmpty(footer.Text) ? footer.Text : string.Empty;

                    if (footer.Mergemodel == null)
                    {
                        ws.Cell(row, column1).Value = footer.Text;
                        ws.Cell(row, column1).Style.Alignment.SetHorizontal(footer.AlignBodyColumn);
                        ws.Cell(row, column1).Style.Protection.SetLocked(footer.LockColumn);

                        if (!string.IsNullOrEmpty(footer.Color))
                        {
                            ws.Cell(row, column1).Style.Fill.BackgroundColor = XLColor.FromHtml(footer.Color);
                        }
                    }
                    else
                    {
                        ExcelMergemodel hMerge = footer.Mergemodel!;

                        /// row
                        hMerge.FirstCellRow = hMerge.FirstCellRow == 0 ? row : hMerge.FirstCellRow;
                        hMerge.LastCellRow = hMerge.LastCellRow == 0 ? row : hMerge.LastCellRow;

                        ws.Range(hMerge.FirstCellRow, hMerge.FirstCellColumn, hMerge.LastCellRow, hMerge.LastCellColumn)
                            .Merge();

                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Value = headerText;
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Alignment.SetHorizontal(footer.AlignBodyColumn);
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Protection.SetLocked(footer.LockColumn);

                        if (!string.IsNullOrEmpty(footer.Color))
                        {
                            ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(footer.Color);
                        }

                        if (tempRow == 0)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                        else if (tempRow < hMerge.LastCellRow)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                    }
                }
                if (tempRow != 0)
                {
                    row = tempRow + 1;
                }
                else
                {
                    row++;
                }
            }
            #endregion
        }

        MemoryStream xLSStream = new();
        wb.SaveAs(xLSStream);

        byte[] bytes = xLSStream.ToArray();
        xLSStream.Close();

        string fileName = $"สรุปการกู้ยืมรายปีงบประมาณ-{dateService.ChangeDate(DateTime.Now, "yyyy-MM-dd", Utility.DateLanguage_EN)}.xlsx";

        await SaveFileAndImgService.SaveFileAsPath(bytes, fileName, "data:application/vnd.ms-excel;base64,");
    }

    private ExcelPdfAllModel setSheetImport(List<ReportAdminModel> datas)
    {
        ExcelPdfAllModel excelModel = new();

        CCampus? Campus = EntitiesCentralService.GetCampus(Info.CampId);
        for (int i = 0; i < datas.Count; i++)
        {
            var data = datas[i];
            var item = data.ReportAdminList;

            var title = $"สรุปรายงานเข้าที่ประชุมคณะอนุกรรมการสวัสดิการฯ {Campus?.CampNameThai} ปีงบประมาณ {(Convert.ToInt32(item[0].FiscalYears) + 543)}";

            #region Sheet1
            ExcelBodyPdfAllModel excelBodyModel = new()
            {
                ExcelTitles = new List<ExcelTextStyleModel>()
                {
                    new()
                    {
                        Text = title,
                        Mergemodel = new()
                        {
                            FirstCellColumn = 1,
                            LastCellColumn = 11,
                            FirstCellRow = 1,
                            LastCellRow= 1
                        },
                        Border = false,
                    }
                },
                ExcelHeader = new List<ExcelTextStyleModel>()
                {
                    new()
                    {
                        Text = "ประเภทสวัสดิการ",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 1,
                            LastCellColumn = 1,
                            FirstCellRow = 2,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "ปีงบ",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 2,
                            LastCellColumn = 2,
                            FirstCellRow = 2,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "ประเภทบุคลากร",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 3,
                            LastCellColumn = 6,
                            FirstCellRow = 2,
                            LastCellRow= 2
                        }
                    },
                    new()
                    {
                        Text = "ขก",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 3,
                            LastCellColumn = 3,
                            FirstCellRow = 3,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "พ.ม.",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 4,
                            LastCellColumn = 4,
                            FirstCellRow = 3,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "เงินรายได้",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 5,
                            LastCellColumn = 5,
                            FirstCellRow = 3,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "ลูกจ้าง",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 6,
                            LastCellColumn = 6,
                            FirstCellRow = 3,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "รวม",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 7,
                            LastCellColumn = 7,
                            FirstCellRow = 2,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "ยอดเงินกู้รวม",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 8,
                            LastCellColumn = 8,
                            FirstCellRow = 2,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "ยอดดอกเบี้ยเงินกู้",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 9,
                            LastCellColumn = 9,
                            FirstCellRow = 2,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "ยอดชำระเงินกู้แล้ว",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 10,
                            LastCellColumn = 10,
                            FirstCellRow = 2,
                            LastCellRow= 3
                        }
                    },
                    new()
                    {
                        Text = "ยอดเงินกู้คงเหลือ",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 11,
                            LastCellColumn = 11,
                            FirstCellRow = 2,
                            LastCellRow= 3
                        }
                    },
                },
                WorksheetsName = $"ปีงบประมาณ {(Convert.ToInt32(item[0].FiscalYears) + 543)}",
                ExcelBody = item,
                ExcelFooters = new List<ExcelTextStyleModel>()
                {
                    new()
                    {
                        Text = "รวม",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 1,
                            LastCellColumn = 2,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new()
                    {
                        Text = $"{(item.Sum(x => x.SumGovernmentOfficer))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 3,
                            LastCellColumn = 3,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new()
                    {
                        Text = $"{(item.Sum(x => x.SumUniversityStaff))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 4,
                            LastCellColumn = 4,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new()
                    {
                        Text = $"{(item.Sum(x => x.SumIncomeEmployee))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 5,
                            LastCellColumn = 5,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new()
                    {
                        Text = $"{(item.Sum(x => x.SumEmployee))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 6,
                            LastCellColumn = 6,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new()
                    {
                        Text = $"{(item.Sum(x => x.SumLoanAgreement))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 7,
                            LastCellColumn = 7,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new() {
                        Text = $"{(string.Format("{0:n2}",item.Sum(x => x.SumLoanAmount)))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 8,
                            LastCellColumn = 8,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new() {
                        Text = $"{(string.Format("{0:n2}",item.Sum(x => x.TotalInterest)))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 9,
                            LastCellColumn = 9,
                            FirstCellRow = 0,
                            LastCellRow = 0
                        }
                    },
                    new()
                    {
                        Text = $"{(string.Format("{0:n2}",item.Sum(x => x.TotalPayment)))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 10,
                            LastCellColumn = 10,
                            FirstCellRow = 0,
                            LastCellRow= 0
                        }
                    },
                    new()
                    {
                        Text = $"{(string.Format("{0:n2}",item.Sum(x => x.TotalBalance)))}",
                        Mergemodel = new()
                        {
                            FirstCellColumn = 11,
                            LastCellColumn = 11,
                            FirstCellRow = 0,
                            LastCellRow= 0
                        }
                    },
                }
            };

            #endregion
            excelModel.excelBodys.Add(excelBodyModel);
        }

        return excelModel;
    }

    private async Task ExportExcelDataResultDEV()
    {
        Console.WriteLine(listReportResultDev);

        ExcelPdfAllModel data = setSheetImportDEV(listReportResultDev);

        var wb = new XLWorkbook();

        foreach (ExcelBodyPdfAllModel item in data.excelBodys)
        {
            int row = 1;
            var ws = wb.Worksheets.Add(item.WorksheetsName);
            ws.Style.Font.FontSize = 14;
            int tempRow = 0;

            #region Title
            if (item.ExcelTitles.Any())
            {
                for (int titleCount = 0; titleCount < item.ExcelTitles.Count; titleCount++)
                {
                    var column = 0;
                    ExcelTextStyleModel excelTitle = item.ExcelTitles[titleCount];

                    if (!string.IsNullOrEmpty(excelTitle?.Text))
                    {
                        column = titleCount + 1;
                        Utility.setTitleExcel(excelTitle, column, row, ws);
                    }
                    else if (excelTitle!.TextList.Any())
                    {
                        for (int titleIndex = 0; titleIndex < excelTitle.TextList.Count; titleIndex++)
                        {
                            column = titleIndex + 1;
                            ExcelTextStyleModel subTitles = excelTitle.TextList[titleIndex];
                            ws = Utility.setTitleExcel(subTitles, column, row, ws);
                        }
                    }

                    row++;
                }
            }
            #endregion

            #region SetHeader
            if (item.ExcelHeader.Any())
            {
                tempRow = 0;
                for (int i = 0; i < item.ExcelHeader.Count; i++)
                {
                    ExcelTextStyleModel header = item.ExcelHeader[i];
                    var column1 = i + 1;
                    var headerText = !string.IsNullOrEmpty(header.Text) ? header.Text : string.Empty;

                    if (header.Mergemodel == null)
                    {
                        ws.Cell(row, column1).Value = headerText;
                        ws.Cell(row, column1).Style.Alignment.SetHorizontal(header.AlignBodyColumn);
                        ws.Cell(row, column1).Style.Protection.SetLocked(header.LockColumn);

                        if (!string.IsNullOrEmpty(header.Color))
                        {
                            ws.Cell(row, column1).Style.Fill.BackgroundColor = XLColor.FromHtml(header.Color);
                        }
                    }
                    else
                    {
                        ExcelMergemodel hMerge = header.Mergemodel!;

                        /// row
                        hMerge.FirstCellRow = hMerge.FirstCellRow == 0 ? row : hMerge.FirstCellRow;
                        hMerge.LastCellRow = hMerge.LastCellRow == 0 ? row : hMerge.LastCellRow;

                        ws.Range(hMerge.FirstCellRow, hMerge.FirstCellColumn, hMerge.LastCellRow, hMerge.LastCellColumn)
                            .Merge();

                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Value = headerText;
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Alignment.SetHorizontal(header.AlignBodyColumn);
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Protection.SetLocked(header.LockColumn);

                        if (!string.IsNullOrEmpty(header.Color))
                        {
                            ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(header.Color);
                        }

                        if (tempRow == 0)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                        else if (tempRow < hMerge.LastCellRow)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                    }
                }
                if (tempRow != 0)
                {
                    row = tempRow + 1;
                }
                else
                {
                    row++;
                }
            }
            #endregion

            #region SetBody
            if (item.ExcelBody.Any())
            {
                tempRow = 0;
                var ele = item.ExcelBody;

                List<string> loanTypeName = ele.Select(x => $"{x.LoanTypeName}").ToList();
                List<string> staffTypeName = ele.Select(x => $"{x.StaffTypeName}").ToList();
                List<string> facNameThai = ele.Select(x => $"{x.FacNameThai}").ToList();
                List<string> staffId = ele.Select(x => $"{x.StaffId}").ToList();
                List<string> loanAmount = ele.Select(x => $"{x.LoanAmount}").ToList();
                List<string> totalInterest = ele.Select(x => $"{x.TotalInterest}").ToList();
                List<string> contractDate = ele.Select(x => $"{(dateService.ChangeDate(x.ContractDate, "d MMMM yyyy", Utility.DateLanguage_TH))}").ToList();
                List<string> paidDate = ele.Select(x => $"{(dateService.ChangeDate(x.PaidDate, "d MMMM yyyy", Utility.DateLanguage_TH))}").ToList();
                List<string> totalBalance = ele.Select(x => $"{(string.Format("{0:n2}", x.TotalBalance))}").ToList();
                List<string> totalPayment = ele.Select(x => $"{(string.Format("{0:n2}", x.TotalPayment))}").ToList();
                List<string> contractNo = ele.Select(x => x.ContractNo).ToList();

                ws.Cell(row, 1).InsertData(loanTypeName);
                ws.Cell(row, 2).InsertData(staffTypeName);
                ws.Cell(row, 3).InsertData(facNameThai);
                ws.Cell(row, 4).InsertData(staffId);
                ws.Cell(row, 5).InsertData(loanAmount);
                ws.Cell(row, 6).InsertData(totalInterest);
                ws.Cell(row, 7).InsertData(contractDate);
                ws.Cell(row, 8).InsertData(paidDate);
                ws.Cell(row, 9).InsertData(totalPayment);
                ws.Cell(row, 10).InsertData(totalBalance);
                ws.Cell(row, 11).InsertData(contractNo);

                row += ele.Count;
            }
            #endregion

            #region footer
            if (item.ExcelFooters.Any())
            {
                tempRow = 0;
                for (int i = 0; i < item.ExcelFooters.Count; i++)
                {
                    ExcelTextStyleModel footer = item.ExcelFooters[i];

                    var column1 = i + 1;
                    var headerText = !string.IsNullOrEmpty(footer.Text) ? footer.Text : string.Empty;

                    if (footer.Mergemodel == null)
                    {
                        ws.Cell(row, column1).Value = footer.Text;
                        ws.Cell(row, column1).Style.Alignment.SetHorizontal(footer.AlignBodyColumn);
                        ws.Cell(row, column1).Style.Protection.SetLocked(footer.LockColumn);

                        if (!string.IsNullOrEmpty(footer.Color))
                        {
                            ws.Cell(row, column1).Style.Fill.BackgroundColor = XLColor.FromHtml(footer.Color);
                        }
                    }
                    else
                    {
                        ExcelMergemodel hMerge = footer.Mergemodel!;

                        /// row
                        hMerge.FirstCellRow = hMerge.FirstCellRow == 0 ? row : hMerge.FirstCellRow;
                        hMerge.LastCellRow = hMerge.LastCellRow == 0 ? row : hMerge.LastCellRow;

                        ws.Range(hMerge.FirstCellRow, hMerge.FirstCellColumn, hMerge.LastCellRow, hMerge.LastCellColumn)
                            .Merge();

                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Value = headerText;
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Alignment.SetHorizontal(footer.AlignBodyColumn);
                        ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Protection.SetLocked(footer.LockColumn);

                        if (!string.IsNullOrEmpty(footer.Color))
                        {
                            ws.Cell(hMerge.FirstCellRow, hMerge.FirstCellColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(footer.Color);
                        }

                        if (tempRow == 0)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                        else if (tempRow < hMerge.LastCellRow)
                        {
                            tempRow = hMerge.LastCellRow;
                        }
                    }
                }
                if (tempRow != 0)
                {
                    row = tempRow + 1;
                }
                else
                {
                    row++;
                }
            }
            #endregion
        }

        MemoryStream xLSStream = new();
        wb.SaveAs(xLSStream);

        byte[] bytes = xLSStream.ToArray();
        xLSStream.Close();

        string fileName = $"ข้อมูลปีงบประมาณ-{dateService.ChangeDate(DateTime.Now, "yyyy-MM-dd", Utility.DateLanguage_EN)}.xlsx";

        await SaveFileAndImgService.SaveFileAsPath(bytes, fileName, "data:application/vnd.ms-excel;base64,");
    }

    private ExcelPdfAllModel setSheetImportDEV(List<ReportAdminModel> datas)
    {
        ExcelPdfAllModel excelModel = new();

        CCampus? Campus = EntitiesCentralService.GetCampus(Info.CampId);

        List<decimal> fiscalYears = datas
            .Where(x => x.FiscalYear != null)
            .Select(x => x.FiscalYear!.Value)
            .Distinct()
            .ToList();

        if (!fiscalYears.Any())
        {
            return excelModel;
        }

        foreach (var item in fiscalYears)
        {
            List<ReportAdminModel> data = datas
                .Where(x => x.FiscalYear == item)
                .ToList();

            var title = $"ข้อมูล {Campus?.CampNameThai} ปีงบประมาณ {data[0].FiscalYear}";

            #region Sheet1
            ExcelBodyPdfAllModel excelBodyModel = new()
            {
                ExcelTitles = new List<ExcelTextStyleModel>()
                {
                    new()
                    {
                        Text = title,
                        Mergemodel = new()
                        {
                            FirstCellColumn = 1,
                            LastCellColumn = 10,
                            FirstCellRow = 1,
                            LastCellRow= 1
                        },
                        Border = false,
                    }
                },
                ExcelHeader = new List<ExcelTextStyleModel>()
                {
                    new(){ Text = "ประเภทสวัสดิการ"},
                    new(){ Text = "ประเภทบุคลากร",},
                    new(){ Text = "ส่วนงาน",},
                    new(){ Text = "รหัสบุคลากร",},
                    new(){ Text = "จำนวนเงินกู้",},
                    new(){ Text = "จำนวนดอกเบี้ยเงินกู้",},
                    new(){ Text = "วันที่ทำสัญญา",},
                    new(){ Text = "วันที่โอนเงิน",},
                    new()
                    {
                        Text = "ยอดชำระเงินกู้แล้ว",
                    },
                    new()
                    {
                        Text = "ยอดเงินกู้คงเหลือ",
                    },
                    new()
                    {
                        Text = "เลขที่สัญญา",
                    },
                },
                WorksheetsName = $"ปีงบประมาณ {data[0].FiscalYear}",
                ExcelBody = data,
            };

            #endregion
            excelModel.excelBodys.Add(excelBodyModel);
        }

        return excelModel;
    }
}

public class InfoModel
{
    public decimal Start { get; set; } = 0;
    public decimal End { get; set; } = 0;
    public List<decimal> SelectLoanTypeId { get; set; } = new();
    public string? CampId { get; set; } = null;
}

public class FiscalReportDataAdminModel
{
    /// <summary>
    /// ค.ศ.
    /// </summary>
    public int FiscalYear { get; set; } = 0;
    public List<ReportAdminModel> ReportData { get; set; } = new();
}

public class ExcelPdfAllModel
{
    public List<ExcelBodyPdfAllModel> excelBodys { get; set; } = new();
    public string? FileName { get; set; } = null;
    public bool ProtectSheet { get; set; } = false;
    public string? SheetPassword { get; set; } = "1234";
}

public class ExcelBodyPdfAllModel
{
    public string WorksheetsName { get; set; } = string.Empty;

    /// <summary>
    /// หัวข้อใน excel
    /// </summary>
    public List<ExcelTextStyleModel> ExcelTitles { get; set; } = new();

    /// <summary>
    /// ข้อมูลในตราง header
    /// </summary>
    public List<ExcelTextStyleModel> ExcelHeader { get; set; } = new();

    /// <summary>
    /// ข้อมูลในตราง body แบบ model List string
    /// </summary>
    public List<ReportAdminModel> ExcelBody { get; set; } = new();

    public List<ExcelTextStyleModel> ExcelFooters { get; set; } = new();
}
