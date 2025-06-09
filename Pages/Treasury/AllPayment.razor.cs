using AntDesign.TableModels;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using NPOI.HSSF.Record;

namespace LoanApp.Pages.Treasury;

public partial class AllPayment
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #endregion

    [Parameter] public decimal StatusID { get; set; } = 0;

    #region Inject
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
    [Inject] private LoanApp.IServices.IUtilityServer utilityServer { get; set; } = null!;

    #endregion

    private IOrderedQueryable<VLoanRequestContract> loanRequestContractsTemp { get; set; } = null!;
    private List<VLoanRequestContract> newLoanRequestContracts { get; set; } = new();
    private List<LoanType> LoanTypeList { get; set; } = new();
    private SearchModel Search { get; set; } = new();
    private List<VLoanRequestContract> ListRecord { get; set; } = new();
    private List<ContractStatus> Status { get; set; } = new();

    private decimal[] AllowedStatus { get; } = new[] { 6m, 7m, 8m, 80m, 81m, 82m, 99m, 98m, 200m };
    private string? SearchView { get; set; } = null;
    private decimal StaId { get; set; } = 0;
    private decimal TypeID { get; set; } = 0;
    private int _pageIndex { get; set; } = 1;

    //protected override async Task OnAfterRenderAsync(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //        try
    //        {
    //            if (!string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow))
    //            {
    //                string adminCapmId = StateProvider.CurrentUser.CapmSelectNow;

    //                loanRequestContractsTemp = _context.VLoanRequestContracts
    //                .Where(c => AllowedStatus.Contains(c.CurrentStatusId!.Value))
    //                .Where(c => (c.ContractDate == null) || ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
    //                .Where(c => c.DebtorCampusId == adminCapmId)
    //                .OrderBy(c => c.CurrentStatusId);
    //            }

    //            if (StatusID != 0)
    //            {
    //                StaId = StatusID;
    //                await DataTableV2(Search.Title, StaId, TypeID);
    //            }
    //            else
    //            {
    //                await DataTableV2(Search.Title, StaId, TypeID);
    //            }

    //            LoanTypeList = await psuLoan.GetAllLoanType();

    //            LoanTypeList.Insert(0, new LoanType()
    //            {
    //                LoanTypeId = 0,
    //                LoanTypeName = "ทุกประเภท"
    //            });

    //            Status = await psuLoan.GetAllContractStatus(AllowedStatus.ToList());

    //            Status.Insert(0, new ContractStatus()
    //            {
    //                ContractStatusId = 0,
    //                ContractStatusName = "ทุกสถานะ"
    //            });


    //            if (StatusID != 0)
    //            {
    //                int index = Status.FindIndex(a => a.ContractStatusId == StatusID);
    //                await JS.InvokeVoidAsync("SelectedTagName", index);
    //            }
    //            StateHasChanged();
    //        }
    //        catch (Exception ex)
    //        {
    //            await Error.ProcessError(ex);
    //        }
    //    }
    //}

    private async Task OnSearch(string? val, decimal staId, decimal typeId)
    {
        try
        {
            Search.Title = string.Empty;

            if (string.IsNullOrEmpty(val))
            {
                await DataTableV2(null, StaId, TypeID);
                return;
            }
            else if ((val.Trim()).Length < Utility.SearchMinlength)
            {
                await notificationService.WarningDefult($"กรุณาป้อนข้อมูลการค้นหา อย่างน้อย {Utility.SearchMinlength} ตัวอักษร");
                return;
            }

            _pageIndex = 1;
            Search.Title = val.Trim();
            await DataTableV2(Search.Title, staId, typeId);
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task DataTableV2(string? searchName, decimal statusID, decimal typeID)
    {
        newLoanRequestContracts = new();

        await Task.Delay(1);
        StateHasChanged();

        try
        {
            newLoanRequestContracts = await loanRequestContractsTemp
                .Where(c => string.IsNullOrEmpty(searchName) ||
                (c.DebtorNameTh!.Contains(searchName) ||
                c.DebtorSnameTh!.Contains(searchName) ||
                (c.DebtorNameEng!).ToLower().Contains(searchName.ToLower()) ||
                (c.DebtorSnameEng!).ToLower().Contains(searchName.ToLower()) ||
                (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower()) ||
                c.ContractNo!.Contains(searchName)))
                .Where(c => statusID == 0 || c.CurrentStatusId == statusID)
                .Where(c => typeID == 0 || c.LoanTypeId == typeID)
                .ToListAsync();

            StateHasChanged();
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// คาดการณ์ การชำระเงินงวดถัดไป
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private async Task<string> GetPayDateAsync(VLoanRequestContract data)
    {
        DateTime? paidDate = null;
        string payDate = string.Empty;
        List<InstallmentDetail> iDetail = new();

        try
        {
            List<PaymentTransaction> ListPaymentTransaction = await psuLoan.GetAllPaymentTransactionByContractId(data.ContractId);

            if (ListPaymentTransaction.Any())
            {
                iDetail = await psuLoan.GetAllInstallmentDetailByContractId(data.ContractId);
            }

            paidDate = data.PaidDate != null ? data.PaidDate : data.ContractDate;

            #region กรณีจ่ายครบแล้ว
            if (ListPaymentTransaction.Count == data.ContractLoanNumInstallments)
            {
                DateTime? oDate = ListPaymentTransaction
                    .Where(c => c.InstallmentNo == (ListPaymentTransaction.Max(x => x.InstallmentNo)))
                    .Select(x => x.PayDate)
                    .FirstOrDefault();

                payDate = $"ชำระหมดแล้ว " +
                    $"{dateService.ChangeDate(oDate, "dd MMMM yyyy", Utility.DateLanguage_TH)}";
            }
            #endregion

            else if (ListPaymentTransaction.Any())
            {
                DateTime? oDate = null;

                PaymentTransaction lastTransaction = ListPaymentTransaction
                    .Where(c => c.InstallmentNo == (ListPaymentTransaction.Max(x => x.InstallmentNo)))
                    .First();

                if (lastTransaction.PayDate != null)
                {
                    DateTime date = lastTransaction.PayDate.Value.AddMonths(1);
                    int IntYear = date.Year;
                    int ModMonth = date.Month;
                    int day = DateTime.DaysInMonth(IntYear, ModMonth);
                    oDate = new(IntYear, ModMonth, day);
                }

                return dateService.ChangeDate(oDate, "dd MMMM yyyy", Utility.DateLanguage_TH);
            }

            #region get InstallmentDetail โดยอ้างอิงจาก DueDate
            else if (iDetail.Any())
            {
                int index = 0;
                if (ListPaymentTransaction.Any())
                {
                    index = ListPaymentTransaction.Count;
                }

                /// มีการ +1 ไปแล้ว
                DateTime oDate = dateService.ConvertToDateTime(iDetail[index].DueDate);
                return dateService.ChangeDate(oDate, "dd MMMM yyyy", Utility.DateLanguage_TH);
            }
            #endregion

            else
            {
                #region กำหนดวันเอง โดยอ้างอิงจาก PaidDate
                if (ListPaymentTransaction.Any())
                {
                    DateTime? oDate = ListPaymentTransaction
                    .Where(c => c.InstallmentNo == (ListPaymentTransaction.Max(x => x.InstallmentNo)))
                    .Select(x => x.PayDate)
                    .FirstOrDefault();

                    int IntYear = Convert.ToInt32(dateService.ChangeDate(oDate, "yyyy", Utility.DateLanguage_EN)) + 543;
                    int month = Convert.ToInt32(dateService.ChangeDate(oDate, "MM", Utility.DateLanguage_EN)) + 1;
                    int ModMonth = month % 12;
                    return Admin.ManageLoanAgreementV2.SetDay(IntYear, month, ModMonth);
                }
                #endregion

                else
                {
                    var dayList = TransactionService.SetPayDate(paidDate, data.ContractLoanNumInstallments!.Value);

                    return (dayList.Any() ? dayList[0] : string.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
        return payDate;
    }

    private void SeeDetail(RowData<VLoanRequestContract> row)
    {
        VLoanRequestContract data = row.Data;

        //if ((new List<decimal>() { 6, 200 }).Contains(data.CurrentStatusId!.Value))
        //{
        //    return;
        //}
        //else if (data.CurrentStatusId == 7)
        //{
        //    Check(data);
        //}
        //else
        //{
        //    navigationManager.NavigateTo($"/Admin/AgreementDetail/{data.LoanRequestId}");
        //}
    }

    protected async Task SelectLoanTypeIDAsync(ChangeEventArgs e)
    {
        try
        {
            _pageIndex = 1;
            TypeID = Convert.ToDecimal(e.Value!.ToString());
            await DataTableV2(Search.Title, StaId, TypeID);
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    protected async Task SelectStatusAsync(ChangeEventArgs e)
    {
        try
        {
            _pageIndex = 1;
            ListRecord = new();
            StaId = Convert.ToDecimal(e.Value!.ToString());
            await DataTableV2(Search.Title, StaId, TypeID);
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }
}
