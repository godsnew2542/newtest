using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.Model.Helper;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System.Data;
using System.Globalization;

namespace LoanApp.Pages.Admin;

public partial class CloseAppointment
{
    [CascadingParameter] private UserStateProvider stateProvider { get; set; } = null!;

    [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private IEntitiesCentralService entitiesCentralService { get; set; } = null!;

    private List<VLoanRequestContract> requestContracts { get; set; } = new();
    private List<LoanMeetingSchedule> meetingSchedules { get; set; } = new();
    private List<CCampus> cCampuses { get; set; } = new();
    private LoanMeetingSchedule? loanMeetingSchedule { get; set; } = null;

    private List<string> listTime { get; set; } = Utility.Time.ToList();
    private DateTime? tempDate { get; set; } = DateTime.Now;
    private DateTime? selectDate { get; set; } = DateTime.Now;
    private DateTime[] dates { get; set; } = new[] { DateTime.Now };

    private string? adminCampId { get; set; } = null;
    private bool isMobile { get; set; } = false;

    private bool closeScheduleVisible { get; set; } = false;
    private bool deleteScheduleVisible { get; set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            adminCampId = stateProvider.CurrentUser.CapmSelectNow;
            if (!string.IsNullOrEmpty(adminCampId) && selectDate != null)
            {
                SetDisabledDates(selectDate!.Value);

                await onSearchDate(selectDate);
                cCampuses = await entitiesCentralService.GetCampusListAsync();
                isMobile = await userService.CheckDevice();
            }

            StateHasChanged();
        }
    }

    private void OnChange(DateTime? value)
    {
        tempDate = null;
        if (value != null)
        {
            tempDate = dateService.ConvertToDateTime(value);
            SetDisabledDates(value.Value);
        }
    }

    private void SetDisabledDates(DateTime newDate)
    {
        List<DateTime> _dates = new();

        int month = Convert.ToInt32(dateService.ChangeDate(newDate, "MM", Utility.DateLanguage_EN));
        int year = Convert.ToInt32(dateService.ChangeDate(newDate, "yyyy", Utility.DateLanguage_EN));

        int LastDay = DateTime.DaysInMonth(year, month);
        _dates = dateService.SetDisabledWeekday(LastDay, year, month, _dates);
        dates = _dates.ToArray();
    }

    private async Task onSearchDate(DateTime? idate)
    {
        if (idate == null)
        {
            return;
        }

        selectDate = idate;
        meetingSchedules = new();
        requestContracts = new();

        var _adminCampId = adminCampId == "00" ? null : adminCampId;

        try
        {
            meetingSchedules = await psuLoan.GetListLoanMeetingSchedulesByScheduleDateV2(idate, "0", _adminCampId);
        }
        catch (Exception ex)
        {
            await notificationService.Error(notificationService.ExceptionLog(ex));
            return;
        }

        try
        {
            requestContracts = await psuLoan.GetListVLoanRequestContractByCurrentStatusIdAsync(4, idate!.Value);
        }
        catch (Exception ex)
        {
            await notificationService.Error(notificationService.ExceptionLog(ex));
            return;
        }

        if (!string.IsNullOrEmpty(_adminCampId))
        {
            requestContracts = requestContracts.Where(c => c.DebtorCampusId == _adminCampId).ToList();
        }
    }

    private int GetTimeValue(int timeInt, string timeString, List<VLoanRequestContract> vLoans)
    {
        if (!vLoans.Any() || selectDate == null)
        {
            return 0;
        }

        DateTime date = selectDate!.Value;
        DateTime newDate = date;

        string dateString = $"{date.Day}-{date.Month}-{date.Year} {timeString}";
        string format = "dd-MMM-yyyy HH:mm";
        bool success = DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate);

        if (!success)
        {
            newDate = new DateTime(date.Year, date.Month, date.Day, timeInt, 0, 0);
        }

        var result = vLoans
            .Where(c => c.ContractDate != null)
            .Where(c => c.ContractDate == newDate)
            .ToList();
        return result.Count;
    }

    private bool CheckAdminSchedule(int timeInt, string timeString, List<LoanMeetingSchedule> schedules)
    {
        if (!schedules.Any() || selectDate == null)
        {
            return true;
        }

        DateTime date = selectDate!.Value;
        DateTime newDate = date;

        string dateString = $"{date.Day}-{date.Month}-{date.Year} {timeString}";
        string format = "dd-MMM-yyyy HH:mm";
        bool success = DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate);

        if (!success)
        {
            newDate = new DateTime(date.Year, date.Month, date.Day, timeInt, 0, 0);
        }
        var result = schedules
            .Where(c => c.ScheduleDate == newDate)
            .ToList();

        return result.Any() ? false : true;
    }

    private void onCloseSchedule(int timeInt, string timeString)
    {
        if (selectDate == null)
        {
            Task.Run(() => notificationService.Warning("กรุณาเลือกวันที่"));
            return;
        }

        if (string.IsNullOrEmpty(stateProvider.CurrentUser.StaffId))
        {
            Task.Run(() => notificationService.Warning("ไม่พบข้อมูล StaffId"));
            return;
        }

        DateTime date = selectDate!.Value;
        DateTime newDate = date;

        string dateString = $"{date.Day}-{date.Month}-{date.Year} {timeString}";
        string format = "dd-MMM-yyyy HH:mm";
        bool success = DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate);

        if (!success)
        {
            newDate = new DateTime(date.Year, date.Month, date.Day, timeInt, 0, 0);
        }

        loanMeetingSchedule = new()
        {
            StaffId = stateProvider.CurrentUser.StaffId!,
            ScheduleDate = newDate,
            DateType = "0",
            CampusId = adminCampId
        };

        closeScheduleVisible = true;
    }

    private void HandleCancelCloseSchedule()
    {
        closeScheduleVisible = false;
        loanMeetingSchedule = null;
    }

    private async Task HandleOkCloseSchedule(LoanMeetingSchedule schedule)
    {
        try
        {
            var _adminCampId = adminCampId == "00" ? null : adminCampId;
            var idate = schedule.ScheduleDate;
            var checkSchedules = await psuLoan.GetListLoanMeetingSchedulesByScheduleDateV2(idate, "0", _adminCampId);

            if (checkSchedules.Any())
            {
                var p = checkSchedules.FirstOrDefault(c => c.ScheduleDate == idate);

                if (p != null)
                {
                    _ = Task.Run(() => notificationService.Warning("มีการปิดวันที่ทำสัญญานี้แล้ว"));
                    return;
                }
            }

            await psuLoan.AddLoanMeetingSchedules(schedule);

            _ = Task.Run(() => notificationService.SuccessDefult("บันทึกข้อมูลเรียบร้อย"));
        }
        catch (Exception ex)
        {
            await notificationService.Error(notificationService.ExceptionLog(ex));
        }
        finally
        {
            closeScheduleVisible = false;
            loanMeetingSchedule = null;
            await onSearchDate(selectDate);
        }
    }

    private async Task onDeleteSchedule(int timeInt, string timeString)
    {
        if (selectDate == null)
        {
            Task.Run(() => notificationService.Warning("กรุณาเลือกวันที่"));
            return;
        }

        if (string.IsNullOrEmpty(stateProvider.CurrentUser.StaffId))
        {
            _ = Task.Run(() => notificationService.Warning("ไม่พบข้อมูล StaffId"));
            return;
        }

        DateTime date = selectDate!.Value;
        DateTime newDate = date;

        string dateString = $"{date.Day}-{date.Month}-{date.Year} {timeString}";
        string format = "dd-MMM-yyyy HH:mm";
        bool success = DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate);

        if (!success)
        {
            newDate = new DateTime(date.Year, date.Month, date.Day, timeInt, 0, 0);
        }

        var _adminCampId = adminCampId == "00" ? null : adminCampId;
        var checkSchedules = await psuLoan.GetListLoanMeetingSchedulesByScheduleDateV2(newDate, "0", _adminCampId);

        var message = "ไม่สามารถเปิดวันที่ทำสัญญานี้ได้ เนื่องจากเปิดวันที่ทำสัญญาไปแล้ว";

        if (!checkSchedules.Any())
        {
            _ = Task.Run(() => notificationService.Warning(message));
            return;
        }

        var p = checkSchedules.FirstOrDefault(c => c.ScheduleDate == newDate);
        if (p == null)
        {
            _ = Task.Run(() => notificationService.Warning(message));
            return;
        }

        loanMeetingSchedule = p;
        deleteScheduleVisible = true;
    }

    private void HandleCancelDeleteSchedule()
    {
        deleteScheduleVisible = false;
        loanMeetingSchedule = null;
    }

    private async Task HandleOkDeleteSchedule(LoanMeetingSchedule schedule)
    {
        try
        {
            await psuLoan.DeleteLoanMeetingSchedules(schedule);
            _ = Task.Run(() => notificationService.SuccessDefult("บันทึกข้อมูลเรียบร้อย"));
        }
        catch (Exception ex)
        {
            await notificationService.Error(notificationService.ExceptionLog(ex));
        }
        finally
        {
            deleteScheduleVisible = false;
            loanMeetingSchedule = null;
            await onSearchDate(selectDate);
        }
    }
}
