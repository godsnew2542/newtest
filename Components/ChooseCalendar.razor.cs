using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Pages.User;
using LoanApp.Services.IServices;
using LoanApp.Services.IServices.LoanDb;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System.Data;

namespace LoanApp.Components;

public partial class ChooseCalendar
{
    [Parameter] public VLoanRequestContract? V_ReqCon { get; set; } = null;
    [Parameter] public string? CampIdNow { get; set; } = null;


    [Inject] private IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private INotificationService notificationService { get; set; } = null!;


    private DateTime DateNow { get; set; } = DateTime.Now;
    private DateTime? SelectDateValue { get; set; } = null;

    private DateTime? defaultDate { get; set; } = DateTime.Now;
    private DateTime[] Dates { get; set; } = new DateTime[0];
    private List<string> ListTime { get; set; } = new();


    private TimeView timeView { get; set; } = new();


    private bool isChooseTime { get; set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await OnChange(DateNow);

            StateHasChanged();
        }
    }

    private async Task OnChange(DateTime? _date)
    {
        Dates = DateRenderSpecial(_date, V_ReqCon?.AdminRecordDate);

        if (_date != null)
        {
            List<string> test = Dates
                .Select(x => dateService.ChangeDate(x.Date, "dd MMMM yyyy", Utility.DateLanguage_TH))
                .ToList();

            if (test.Contains(dateService.ChangeDate(_date, "dd MMMM yyyy", Utility.DateLanguage_TH)) ||
                _date.Value.DayOfWeek == DayOfWeek.Saturday ||
                _date.Value.DayOfWeek == DayOfWeek.Sunday)
            {
                return;
            }

            var pp_date = new DateTime(_date.Value.Year, _date.Value.Month, _date.Value.Day);

            //await SearchDate(_date.Value);
            await SearchDate(pp_date);
        }
    }

    private async Task SearchDate(DateTime selectDate)
    {
        isChooseTime = false;

        if (CheckAfterDate(DateNow, selectDate))
        {
            ListTime = await CheckScheduleAdminAway(selectDate);
            isChooseTime = true;
            SelectDateValue = selectDate;
            timeView.DateTimeData = selectDate;
        }
        else
        {
            ListTime = new List<string>(Utility.Time);
        }
    }

    private async Task<List<string>> CheckScheduleAdminAway(DateTime date)
    {
        List<string> tepmTime = (Utility.Time).ToList();
        List<string> tempRemove = new();
        var temp = new List<string>(Utility.Time);
        string? oDate = dateService.ChangeDate(date, "yyyy-MM-dd", Utility.DateLanguage_EN);

        _ = Task.Run(() => notificationService.SuccessDefult(oDate));

        var pp = await psuLoan.GetListLoanMeetingSchedulesByScheduleDateV2(date, "0", CampIdNow);

        _ = Task.Run(() => notificationService.SuccessDefult($"{pp.Count}"));
        try
        {
            for (int i = 0; i < tepmTime.Count; i++)
            {
                var time = tepmTime[i];

                string dateString = $"{oDate} {time.Substring(0, 2)}:{time.Substring(3, 2)}:00.000";
                var ldate = Convert.ToDateTime(dateString);

                var Schedule = await psuLoan.GetListLoanMeetingSchedulesByScheduleDate(ldate, "0", CampIdNow);

                _ = Task.Run(() => notificationService.SuccessDefult($"Schedule: {Schedule.Count} => {ldate}"));


                if (Schedule.Any())
                {
                    tempRemove.Add(time);
                }
                else
                {
                    var contractDate = await psuLoan.GetVLoanRequestContractByContractDate(ldate, CampIdNow);

                    if (contractDate.Count >= Utility.MaxSchedulePeople)
                    {
                        tempRemove.Add(time);
                    }
                }
            }
            if (tempRemove.Any())
            {
                foreach (var item in tempRemove)
                {
                    temp.Remove(item);
                }
            }

            return temp;
        }
        catch (Exception ex)
        {
            _ = Task.Run(() => notificationService.ErrorDefult(notificationService.ExceptionLog(ex)));
            return new List<string>();
        }
    }

    private DateTime[] DateRenderSpecial(DateTime? odate, DateTime? AdminRecordDate)
    {
        List<DateTime> resultDates = new();
        if (odate == null)
        {
            return resultDates.ToArray();
        }

        List<DateTime> _dates = new();
        LoanWithSetDateModel checkLoanBySetDate = new();

        #region DateNow
        int Current_Day = Convert.ToInt32(dateService.ChangeDate(DateNow, "dd", Utility.DateLanguage_EN));
        int Current_month = Convert.ToInt32(dateService.ChangeDate(DateNow, "MM", Utility.DateLanguage_EN));
        int Current_year = Convert.ToInt32(dateService.ChangeDate(DateNow, "yyyy", Utility.DateLanguage_EN));

        #endregion

        #region Parameter Date
        int Select_month = Convert.ToInt32(dateService.ChangeDate(odate, "MM", Utility.DateLanguage_EN));
        int Select_year = Convert.ToInt32(dateService.ChangeDate(odate, "yyyy", Utility.DateLanguage_EN));
        int LastDay = DateTime.DaysInMonth(Select_year, Select_month);

        #endregion

        try
        {
            if (Select_year < Current_year)
            {
                _dates = dateService.SetDisabledDates(LastDay, Select_year, Select_month);
            }
            else if (Select_year == Current_year)
            {
                if (Select_month < Current_month)
                {
                    _dates = dateService.SetDisabledDates(LastDay, Select_year, Select_month);
                }
                else if (Select_month == Current_month)
                {
                    for (int i = 0; i < Current_Day; i++)
                    {
                        if (i + 1 != Current_Day)
                        {
                            DateTime date = new(Select_year, Select_month, i + 1);
                            _dates.Add(date);
                        }
                    }

                    _dates = dateService.SetDisabledWeekday(LastDay, Select_year, Select_month, _dates);
                }
                else if (Select_month > Current_month)
                {
                    if ((Select_month - Current_month) >= Utility.MaxSelectMonth)
                    {
                        _dates = dateService.SetDisabledDates(LastDay, Select_year, Select_month);
                    }
                    else
                    {
                        _dates = dateService.SetDisabledWeekday(LastDay, Select_year, Select_month, _dates);
                    }
                }
                else
                {
                    _dates = dateService.SetDisabledDates(LastDay, Select_year, Select_month);
                }
            }
            else if (Select_year > Current_year)
            {
                var _SelectMonth = Select_month + 12;
                if ((_SelectMonth - Current_month) >= Utility.MaxSelectMonth)
                {
                    _dates = dateService.SetDisabledDates(LastDay, Select_year, Select_month);
                }
                else
                {
                    _dates = dateService.SetDisabledWeekday(LastDay, Select_year, Select_month, _dates);
                }
            }

            if (V_ReqCon != null && checkLoanBySetDate.LoanType.Contains((int)V_ReqCon.LoanTypeId!))
            {
                List<DateTime> LoanBySetDate = new();

                for (int i = 0; i < LastDay; i++)
                {
                    var day = i + 1;
                    if (!checkLoanBySetDate.Day.Contains(day))
                    {
                        DateTime Date = new(Select_year, Select_month, day);
                        LoanBySetDate.Add(Date);
                    }
                }
                resultDates = _dates.Union(dateService.SetDisabledWeekday(LastDay, Select_year, Select_month, LoanBySetDate)).ToList();
            }
            else
            {
                resultDates = _dates;
            }

            //Set not select 7 || 3 Days (don't count Saturday And Sunday) After Admin Submit (Status 2)
            if (AdminRecordDate != null)
            {
                resultDates = new List<DateTime>().Union(GetChooseDateByManager(AdminRecordDate)).ToList();
            }

            return resultDates.ToArray();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private List<DateTime> GetChooseDateByManager(DateTime? AdminRecordDate)
    {
        List<DateTime> _dates = new();
        int CheckChooseDateByManager = 0;
        int day = 0;
        int month = 0;
        int year = 0;

        for (int i = 0; i < Utility.PendingChooseDate + 10; i++)
        {
            DateTime recordDate = DateTime.Now;
            if (CheckChooseDateByManager != Utility.PendingChooseDate)
            {
                if (i == 0)
                {
                    recordDate = dateService.ConvertToDateTime(AdminRecordDate);
                    day = Convert.ToInt32(dateService.ChangeDate(recordDate, "dd", Utility.DateLanguage_EN));
                    month = Convert.ToInt32(dateService.ChangeDate(recordDate, "MM", Utility.DateLanguage_EN));
                    year = Convert.ToInt32(dateService.ChangeDate(recordDate, "yyyy", Utility.DateLanguage_EN));
                }
                int LastDay = DateTime.DaysInMonth(year, month);
                DateTime Date = new DateTime(year, month, day);

                int CheckDayOfWeek = Utility.Weekday(Date, DayOfWeek.Friday);
                int[] Weekday = new[] { 1, 2 };

                if (!Weekday.Contains(CheckDayOfWeek))
                {
                    ++CheckChooseDateByManager;
                    _dates.Add(Date);
                }

                if (LastDay == day)
                {
                    day = 1;
                    ++month;
                    if (month == 13)
                    {
                        month = 1;
                        ++year;
                    }
                }
                else
                {
                    ++day;
                }
            }
        }
        return _dates;
    }

    private bool CheckAfterDate(DateTime DateNow, DateTime NewDate)
    {
        bool pass = false;
        if (Utility.Weekday(NewDate, DayOfWeek.Friday) != 1 &&
            Utility.Weekday(NewDate, DayOfWeek.Friday) != 2)
        {
            int Current_Day = Convert.ToInt32(dateService.ChangeDate(DateNow, "dd", Utility.DateLanguage_EN));
            int Current_month = Convert.ToInt32(dateService.ChangeDate(DateNow, "MM", Utility.DateLanguage_EN));
            int Current_year = Convert.ToInt32(dateService.ChangeDate(DateNow, "yyyy", Utility.DateLanguage_EN));

            int Future_Day = Convert.ToInt32(dateService.ChangeDate(NewDate, "dd", Utility.DateLanguage_EN));
            int Future_month = Convert.ToInt32(dateService.ChangeDate(NewDate, "MM", Utility.DateLanguage_EN));
            int Future_year = Convert.ToInt32(dateService.ChangeDate(NewDate, "yyyy", Utility.DateLanguage_EN));

            /* check Current And Select Day */
            if (Future_year == Current_year)
            {
                if (Future_month == Current_month)
                {
                    if (Future_Day >= Current_Day)
                    {
                        pass = true;
                    }
                }
                else if (Future_month > Current_month)
                {
                    if ((Future_month - Current_month) >= Utility.MaxSelectMonth)
                    {
                        pass = false;
                    }
                    else
                    {
                        pass = true;
                    }
                }
            }
            else if (Future_year > Current_year)
            {
                pass = true;
            }
        }
        return pass;
    }
}
