using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Models.Data;
using LoanApp.Pages.User;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Globalization;

namespace LoanApp.Pages.Admin;

public partial class ManageAppointment
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #endregion

    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;

    private TimeView Tview { get; set; } = new();
    private List<RemoveListTime> UpdateTimeList { get; set; } = new();
    private List<PersonofTime> ListPerson { get; set; } = new();
    private PersonofTime DeletePerson { get; set; } = new();

    private string DateFormat { get; set; } = "yyyy-MM-dd";
    private string DateValue { get; set; } = string.Empty;
    private DateTime DateValueCheck { get; set; }
    private DateTime SelectDateValue { get; set; } = DateTime.Now;
    private DateTime DateNow { get; set; } = DateTime.Now;
    private List<string> ListTime { get; set; } = new List<string>(Utility.Time);
    private DateTime ChangeDateValueCheck { get; set; }
    private DateTime[] dates { get; set; } = new[] { DateTime.Now };

    private string? AdminCampId { get; set; } = null;

    protected async override Task OnInitializedAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow))
            {
                AdminCampId = StateProvider?.CurrentUser.CapmSelectNow;

                ListTime = new List<string>(Utility.Time);
                DateValue = dateService.ChangeDate(DateNow, DateFormat, Utility.DateLanguage_EN);
                DateValueCheck = DateNow;
                ChangeDateValueCheck = DateNow;

                await SearchDate();
            }
        }
        catch (Exception ex)
        {
            await notificationService.Error(notificationService.ExceptionLog(ex));
            //await Error.ProcessError(notificationService.ExceptionLog(ex) );
        }
    }

    void OnChange(DateTime? value, string format)
    {
        if (value != null)
        {
            DateValueCheck = dateService.ConvertToDateTime(value);
            DateValue = dateService.ChangeDate(DateValueCheck, format, Utility.DateLanguage_EN);
            SetDisabledDates(value.Value);
        }
    }

    void NewOnChange(DateTime? value, RemoveListTime item)
    {
        ChangeDateValueCheck = dateService.ConvertToDateTime(value);
        CheckChangeDate(ChangeDateValueCheck, item);
    }

    private PersonofTime SetDeletePerson(PersonofTime personofTime, decimal? RequestId = null)
    {
        //List<decimal?> Listmodel = (RequestId != null ? new() { RequestId } : personofTime.LoanRequestId!);
        List<decimal?> Listmodel = new();
        List<VLoanRequestContract> loanRequestContract = new();

        if (RequestId != null)
        {
            Listmodel.Add(RequestId);
            loanRequestContract = personofTime.RequestContracts.Where(x => x.LoanRequestId == RequestId).ToList();
        }
        else
        {
            Listmodel = personofTime.RequestContracts.Select(x => (decimal?)x.LoanRequestId).ToList();
            loanRequestContract = personofTime.RequestContracts;
        }

        PersonofTime model = new()
        {
            Date = personofTime.Date,
            LoanRequestId = Listmodel,
            Time = personofTime.Time,
            RequestContracts = loanRequestContract,
        };

        return model;
    }

    private async Task SearchDate()
    {
        ListTime = new List<string>(Utility.Time);
        UpdateTimeList = new();
        try
        {
            bool pass = CheckAfterDate(DateNow, DateValueCheck);
            if (pass)
            {
                SelectDateValue = DateValueCheck;
                ListPerson = await SetPeopleTime(SelectDateValue, ListTime);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void SelectTime(string time, RemoveListTime value)
    {
        Tview.ContractId = value.ContractId;
        Tview.SelectTime = time;
        Tview.Time = $"{time} - {int.Parse(time.Substring(0, 2)) + 1}.00";
        Tview.Date = dateService.ChangeDate(dateService.ConvertToDateTime(value.NewDate), "dd MMMM yyyy", Utility.DateLanguage_EN);
        var dateString = $"{dateService.ChangeDate(dateService.ConvertToDateTime(value.NewDate), "yyyy-MM-dd", Utility.DateLanguage_EN)} {time.Substring(0, 2)}:00:00.000";
        value.NewDate = Convert.ToDateTime(dateString);
    }

    private DTEventArgs CurrentDateList(DateTime data)
    {
        DTEventArgs date = new();
        date.Params.Add(Convert.ToInt32(dateService.ChangeDate(data, "dd", Utility.DateLanguage_EN))); // Day
        date.Params.Add(Convert.ToInt32(dateService.ChangeDate(data, "MM", Utility.DateLanguage_EN))); // Month
        date.Params.Add(Convert.ToInt32(dateService.ChangeDate(data, "yyyy", Utility.DateLanguage_EN))); // Year
        return date;
    }

    private string GetLoan(decimal? loanTypeId)
    {
        var loan = userService.GetLoanType((byte?)loanTypeId);
        var LoanName = userService.GetLoanName(loan);
        return LoanName;
    }

    /// <summary>
    /// ยกเลิกทั้งช่วง ถึงจะลง table LOAN_MEETING_SCHEDULE 
    /// </summary>
    /// <param name="personofTime"></param>
    /// <returns></returns>
    private async Task HiddenListTimeAsync(PersonofTime personofTime)
    {
        try
        {
            var _listPerson = ListPerson;

            int ListPersonIndex = _listPerson.FindIndex(x => x.Time == personofTime.Time);
            _listPerson.RemoveAt(ListPersonIndex);
            ListPerson = _listPerson;

            List<decimal?> ListRequestId = personofTime.RequestContracts
                .Select(x => (decimal?)x.LoanRequestId)
                .ToList();

            UpdateTimeList = SetUpdateTimeList(personofTime.RequestContracts);
            //UpdateTimeList = SetUpdateTimeList(ListRequestId);

            var stringDate = dateService.ChangeDate(personofTime.Date, "yyyy-MM-dd", Utility.DateLanguage_EN);
            var FullDate = $"{stringDate} {personofTime.Time!.Substring(0, 2)}:{personofTime.Time.Substring(3, 2)}:00.000";
            await AddDbMeetingScheduleAsync(FullDate);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    //private List<RemoveListTime> SetUpdateTimeList(List<decimal?> ListRequestId)
    //{
    //    List<RemoveListTime> ListModel = new();
    //    foreach (var id in ListRequestId)
    //    {
    //        var item = userService.GetVLoanRequestContract(id);
    //        RemoveListTime model = new()
    //        {
    //            ContractId = item!.ContractId!.Value,
    //            LoanRequestId = item.LoanRequestId,
    //            DebtorStaffId = item.DebtorStaffId!,
    //            ContractDate = item.ContractDate,
    //            LoanTypeId = item.LoanTypeId
    //        };
    //        ListModel.Add(model);
    //    }
    //    return ListModel;
    //}

    private List<RemoveListTime> SetUpdateTimeList(List<VLoanRequestContract> ListRequest)
    {
        List<RemoveListTime> ListModel = new();
        foreach (var item in ListRequest)
        {
            RemoveListTime model = new()
            {
                ContractId = item!.ContractId!.Value,
                LoanRequestId = item.LoanRequestId,
                DebtorStaffId = item.DebtorStaffId!,
                ContractDate = item.ContractDate,
                LoanTypeId = item.LoanTypeId
            };
            ListModel.Add(model);
        }
        return ListModel;
    }

    private async Task HiddenSelectAsync(PersonofTime personofTime)
    {
        try
        {
            var _listPerson = ListPerson;

            int ListPersonIndex = _listPerson.FindIndex(x => x.Time == personofTime.Time);
            int RequestIdIndex = _listPerson[ListPersonIndex].RequestContracts
                .FindIndex(x => x.LoanRequestId == personofTime.LoanRequestId![0]);
            var uu = _listPerson[ListPersonIndex].RequestContracts
                .Where(x => x.LoanRequestId == personofTime.LoanRequestId![0])
                .ToList();

            _listPerson[ListPersonIndex].RequestContracts.RemoveAt(RequestIdIndex);

            ListPerson = _listPerson;
            UpdateTimeList = SetUpdateTimeList(uu);
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    /// <summary>
    /// TODO ยังไม่ได้ Set ว่าไม่ว่าง DateType = 1(ว่าง), 0(ไม่ว่าง)
    /// TODO จะเข้าก็ต่อเมื่อเลือกทัั้งช่วงเท่านั้น
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    private async Task AddDbMeetingScheduleAsync(string date)
    {
        try
        {
            string AdminStaffId = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
            if (!string.IsNullOrEmpty(AdminStaffId))
            {
                LoanMeetingSchedule schedule = new()
                {
                    StaffId = AdminStaffId,
                    ScheduleDate = Convert.ToDateTime(date),
                    DateType = "1",
                    CampusId = StateProvider?.CurrentUser.CapmSelectNow,
                };
                await psuLoan.AddLoanMeetingSchedules(schedule);
            }
            else
            {
                string alert = $"เกิดข้อผิดพลาด (ไม่พบ StaffId)";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void CheckChangeDate(DateTime date, RemoveListTime value)
    {
        value.ListTime = new List<string>(Utility.Time);
        bool pass = CheckAfterDate(DateNow, date);

        if (!pass)
        {
            value.Datepass = false;
            value.Icon = "far fa-times-circle";
            value.Color = "color: red";
            value.NewDate = null;
        }
        else
        {
            value.Datepass = true;
            value.Icon = "fas fa-check-circle";
            value.Color = "color: green";
            value.NewDate = date;
            CheckScheduleAdminAway(value); // เช็ควันที่ Admin ไม่ว่าง
        }
    }

    private void CheckScheduleAdminAway(RemoveListTime val)
    {
        List<string> tepmTime = new List<string>(Utility.Time);

        foreach (var time in tepmTime)
        {
            string dateString = $"{dateService.ChangeDate(dateService.ConvertToDateTime(val.NewDate), "yyyy-MM-dd", Utility.DateLanguage_EN)} {time.Substring(0, 2)}:00:00.000";
            var Ldate = Convert.ToDateTime(dateString);
            var Schedule = _context.LoanMeetingSchedules
                .Where(c => c.ScheduleDate == Ldate)
                .Where(c => c.DateType == "0")
                .Count();

            if (Schedule != 0)
            {
                var index = val.ListTime.FindIndex(x => x.StartsWith(time));
                val.ListTime.RemoveAt(index);
            }
            else
            {
                var ContractDate = _context.ContractMains
                .Where(c => c.ContractDate != null && c.ContractDate.Value == Ldate)
                .Count();

                if (ContractDate >= Utility.MaxSchedulePeople)
                {
                    var index = val.ListTime.FindIndex(x => x.StartsWith(time));
                    val.ListTime.RemoveAt(index);
                }
            }
        }
    }

    private async Task UpdateScheduleAsync(TimeView data)
    {
        try
        {
            var stringDate = $"{data.Date} {data.SelectTime.Substring(0, 2)}:00:00.000";
            var Ldate = Convert.ToDateTime(stringDate);

            var ContractDate = await _context.ContractMains
                .Where(c => c.ContractDate!.Value == Ldate &&
                c.ContractDate != null)
                .CountAsync();

            if (ContractDate >= Utility.MaxSchedulePeople)
            {
                string alert = $"ไม่สามารถเปลื่ยนแปลงวันที่จองได้เนื่องจากวันที่คุณเลือกเต็ม";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                return;
            }
            else
            {
                /* UpDate ContractDate */
                ContractMain? conMain = await _context.ContractMains
                    .Where(c => c.ContractId == data.ContractId)
                    .FirstOrDefaultAsync();

                if (conMain != null)
                {
                    conMain.ContractDate = Ldate;

                    _context.Update(conMain);
                    await _context.SaveChangesAsync();

                    /* Remove UpdateTimeList by ContractId*/
                    var myTodo = UpdateTimeList.Find(x => x.ContractId == data.ContractId);
                    if (myTodo != null)
                    {
                        UpdateTimeList.Remove(myTodo);

                        var req = await _context.VLoanRequestContracts
                            .Where(c => c.ContractId == data.ContractId)
                            .FirstOrDefaultAsync();

                        if (req != null)
                        {
                            SetDataBySentEmail(req);
                        }

                        Tview = new();
                    }

                }
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private void SetDataBySentEmail(VLoanRequestContract ReqCon)
    {
        try
        {
            var StaffDetail = userService.GetUserDetail(ReqCon.DebtorStaffId);
            var DebtorName = userService.GetFullNameNoTitleName(ReqCon.DebtorStaffId);

            var GuarantorDetail = userService.GetUserDetail(ReqCon.ContractGuarantorStaffId);
            var GuarantoName = userService.GetFullNameNoTitleName(ReqCon.ContractGuarantorStaffId);

            ApplyLoanModel loan = new();
            loan.LoanTypeID = ReqCon.LoanTypeId;
            loan.LoanAmount = ReqCon.ContractLoanAmount!.Value;
            loan.LoanInterest = ReqCon.ContractLoanNumInstallments;

            #region ผู้กู้
            if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
            {
                var Email = string.Empty;
                var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);


                var email = MailService.MailDebtorAppointment(Name,
                    Email,
                    DebtorName,
                    GuarantoName,
                    loan);
                MailService.SendEmail(email);
            }
            #endregion

            #region ผู้ค้ำ
            if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
            {
                var Email = string.Empty;
                var Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);
                Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                var email = MailService.MailDebtorAppointment(Name,
                    Email,
                    DebtorName,
                    GuarantoName,
                    loan);
                MailService.SendEmail(email);
            }
            #endregion
        }
        catch (Exception)
        {
            throw;
        }
    }

    private bool CheckAfterDate(DateTime DateNow, DateTime NewDate)
    {
        bool pass = false;
        try
        {
            int[] _Weekday = new[] { 1, 2 };
            var Weekday = Utility.Weekday(NewDate, DayOfWeek.Friday);

            var CurrentDate = CurrentDateList(DateNow);
            int Current_year = (int)CurrentDate.Params[2];
            int Current_month = (int)CurrentDate.Params[1];
            int Current_Day = (int)CurrentDate.Params[0];

            var FutureDate = CurrentDateList(NewDate);
            int Future_year = (int)FutureDate.Params[2];
            int Future_month = (int)FutureDate.Params[1];
            int Future_Day = (int)FutureDate.Params[0];

            if (!_Weekday.Contains(Weekday))
            {
                pass = true;
            }

            SetDisabledDates(NewDate);
            return pass;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<List<PersonofTime>> SetPeopleTime(DateTime date, List<string> time)
    {
        List<PersonofTime> personofTimes = new();
        if (time.Any())
        {
            try
            {
                var info = await psuLoan.GetListVLoanRequestContractByCurrentStatusIdAsync(4, date);

                if (!string.IsNullOrEmpty(AdminCampId))
                {
                    info = info.Where(c => c.DebtorCampusId == AdminCampId).ToList();
                }

                for (int i = 0; i < time.Count; i++)
                {
                    var item = time[i];

                    DateTime newDate = date;
                    string dateString = $"{date.Day}-{date.Month}-{date.Year} {item}";
                    string format = "dd-MMM-yyyy HH:mm";
                    bool success = DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate);

                    if (!success)
                    {
                        int timeInt = Utility.TimeInt[i];

                        newDate = new DateTime(date.Year, date.Month, date.Day, timeInt, 0, 0);
                    }

                    var result = info
                        .Where(c => c.ContractDate != null)
                        .Where(c => c.ContractDate == newDate)
                        .ToList();

                    PersonofTime model = new()
                    {
                        Time = item,
                        Date = date,
                        RequestContracts = result,
                        DateResult = newDate,
                    };
                    personofTimes.Add(model);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }
        return personofTimes;
    }

    private void SetDisabledDates(DateTime NewDate)
    {
        List<DateTime> _dates = new();

        var _NewDate = CurrentDateList(NewDate);
        int year = (int)_NewDate.Params[2];
        int month = (int)_NewDate.Params[1];

        int LastDay = DateTime.DaysInMonth(year, month);
        _dates = dateService.SetDisabledWeekday(LastDay, year, month, _dates);
        dates = _dates.ToArray();
    }
}

public class RemoveListTime
{
    public decimal ContractId { get; set; }
    public decimal LoanRequestId { get; set; }
    public string DebtorStaffId { get; set; } = string.Empty;
    public DateTime? ContractDate { get; set; }
    public bool Datepass { get; set; } = false;
    public string Icon { get; set; } = "far fa-times-circle";
    public string Color { get; set; } = "color: red";
    public DateTime? NewDate { get; set; }
    public List<string> ListTime { get; set; } = new();
    public decimal? LoanTypeId { get; set; }
}

public class PersonofTime
{
    public List<decimal?>? LoanRequestId { get; set; } = null;
    public string? Time { get; set; } = null;
    public DateTime? Date { get; set; } = null;
    public DateTime? DateResult { get; set; } = null;
    public List<VLoanRequestContract> RequestContracts { get; set; } = new();

}
