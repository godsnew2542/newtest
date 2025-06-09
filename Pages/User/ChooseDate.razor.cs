using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Linq;

namespace LoanApp.Pages.User
{
    public partial class ChooseDate
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider StateProvider { get; set; } = null!;

        #region Parameter
        [Parameter] public decimal RequestID { get; set; } = 0;
        [Parameter] public bool Edit { get; set; } = false;
        [Parameter] public string Role { get; set; } = string.Empty;
        [Parameter] public string StaffID { get; set; } = string.Empty;

        #endregion

        #region Inject
        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;

        #endregion

        private VLoanRequestContract? V_ReqCon { get; set; } = null;
        private TimeView Tview { get; set; } = new();
        private StepUserChooseDateModel StepsChooseDate { get; set; } = new();
        private LoanWithSetDateModel CheckLoanBySetDate { get; set; } = new();
        private List<ListDocModel> ResultDocList { get; set; } = new();

        private int IndexoffColor { get; set; } = 100;
        private DateTime DateNow { get; set; } = DateTime.Now;
        private DateTime DateValueCheck { get; set; } = DateTime.Now;
        private DateTime SelectDateValue { get; set; }
        private bool ChooseTime { get; set; } = false;
        public List<string> ListTime { get; set; } = new();
        private DateTime[] Dates { get; set; } = new DateTime[0];
        private List<string> ChangeDateValue { get; set; } = new();
        private DateTime? Value { get; set; } = DateTime.Now;
        private string StorageName { get; } = "BackToManageLoanRequest";
        private bool LoadingResultImg { get; set; } = false;

        private string? CampIdNow { get; set; } = null;

        protected async override Task OnInitializedAsync()
        {
            StepsChooseDate.Current = 0;

            try
            {
                if (RequestID != 0)
                {
                    V_ReqCon = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);
                }

                #region Set CampId 
                if (string.IsNullOrEmpty(Role))
                {
                    if (V_ReqCon != null)
                    {
                        CampIdNow = V_ReqCon?.DebtorCampusId;
                    }
                    else
                    {
                        VLoanStaffDetail? staffDetail = await psuLoan.GetUserDetailAsync(StateProvider?.CurrentUser.StaffId);

                        CampIdNow = staffDetail?.CampusId;
                    }
                }
                else
                {
                    CampIdNow = StateProvider?.CurrentUser.CapmSelectNow;
                }

                #endregion

                DateRenderSpecial(DateValueCheck, V_ReqCon?.AdminRecordDate);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!Edit)
                {
                    return;
                }

                await GetChooseDateAsync();
                DateRenderSpecial(DateValueCheck, V_ReqCon?.AdminRecordDate);

                StateHasChanged();
            }
        }

        private string Gettime(string time)
        {
            var Date = time.Split(".");
            var part1 = Convert.ToInt32(Date[0]) + 1;
            var newDate = $"{Date[0]}.{Date[1]}-{part1}.{Date[1]}";
            return newDate;
        }

        private async Task GetChooseDateAsync()
        {
            var NameStorage = "ChooseDateTime_1";
            var checkData = await sessionStorage.GetItemAsStringAsync(NameStorage);
            if (checkData != null)
            {
                ChangeDateValue = await sessionStorage.GetItemAsync<List<string>>(NameStorage);
                DateTime idate = ChangeDateEng(ChangeDateValue[0]);
                Value = idate;
                await OnChange(idate);

                SelectTime(ChangeDateValue[2]);
            }
        }

        private DateTime ChangeDateEng(string StringDate)
        {
            MonthModel monthM = new();
            var date = StringDate.Split(' ');
            var month = "01";
            int year = Convert.ToInt32(date[2]) - 543;

            for (int i = 0; i < monthM.Th.Length; i++)
            {
                var th = monthM.Th[i];
                if (th == date[1])
                {
                    month = monthM.Number1[i];
                }
            }

            DateTime idate = new DateTime(year, Convert.ToInt32(month), Convert.ToInt32(date[0]));
            return idate;
        }

        private async Task OnChange(DateTime? value)
        {
            Tview = new();
            if (value != null)
            {
                //DateValueCheck = dateService.ConvertToDateTime(value);
                DateValueCheck = value.Value;
                DateRenderSpecial(DateValueCheck, V_ReqCon?.AdminRecordDate);
                await SearchDate();
                IndexoffColor = 100;

                StateHasChanged();
            }
        }

        private async Task SearchDate()
        {
            ChooseTime = false;

            if (CheckAfterDate(DateNow, DateValueCheck))
            {
                await CheckScheduleAdminAway(DateValueCheck);
                ChooseTime = true;
                SelectDateValue = DateValueCheck;
            }
            else
            {
                ListTime = new List<string>(Utility.Time);
            }
        }

        private void DateRenderSpecial(DateTime odate, DateTime? AdminRecordDate)
        {
            try
            {
                List<DateTime> _dates = new();
                List<DateTime> resultDates = new();

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

                if (V_ReqCon != null && CheckLoanBySetDate.LoanType.Contains((int)V_ReqCon.LoanTypeId!))
                {
                    List<DateTime> LoanBySetDate = new();
                    /// ปรับ จาก 7 วัน เป็น 3 วัน
                    //Utility.PendingChooseDate = 3;

                    for (int i = 0; i < LastDay; i++)
                    {
                        var day = i + 1;
                        if (!CheckLoanBySetDate.Day.Contains(day))
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

                Dates = resultDates.ToArray();
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
                DateTime RecordDate = DateTime.Now;
                if (CheckChooseDateByManager != Utility.PendingChooseDate)
                {
                    if (i == 0)
                    {
                        RecordDate = dateService.ConvertToDateTime(AdminRecordDate);
                        day = Convert.ToInt32(dateService.ChangeDate(RecordDate, "dd", Utility.DateLanguage_EN));
                        month = Convert.ToInt32(dateService.ChangeDate(RecordDate, "MM", Utility.DateLanguage_EN));
                        year = Convert.ToInt32(dateService.ChangeDate(RecordDate, "yyyy", Utility.DateLanguage_EN));
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

        private async Task CheckScheduleAdminAway(DateTime date)
        {
            List<string> tepmTime = (Utility.Time).ToList();
            string? oDate = dateService.ChangeDate(date, "yyyy-MM-dd", Utility.DateLanguage_EN);

            List<string> tempRemove = new();

            var temp = new List<string>(Utility.Time);

            var _scheduleList = await psuLoan.GetListLoanMeetingSchedulesByScheduleDateV2(date, "0", CampIdNow);

            foreach (var time in tepmTime)
            {
                string dateString = $"{oDate} {time.Substring(0, 2)}:{time.Substring(3, 2)}:00.000";
                var ldate = Convert.ToDateTime(dateString);
                var _timeInt= Convert.ToInt32(time.Substring(0, 2));

                //var Schedule = await psuLoan.GetListLoanMeetingSchedulesByScheduleDate(ldate, "0", CampIdNow)
                var Schedule = _scheduleList.Where(c => c.ScheduleDate.Hour == _timeInt).ToList();

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
            ListTime = temp;
        }

        private async Task NextpageAsync(decimal id)
        {
            if (Tview.Date != null)
            {
                if (Tview.DateTimeData != null)
                {
                    string? oDate = dateService.ChangeDate(Tview.DateTimeData, "yyyy-MM-dd", Utility.DateLanguage_EN);
                    string dateString = $"{oDate} {Tview.SelectTime.Substring(0, 2)}:{Tview.SelectTime.Substring(3, 2)}:00.000";
                    var ldate = Convert.ToDateTime(dateString);

                    var schedule = await psuLoan.GetListLoanMeetingSchedulesByScheduleDate(ldate, "0", CampIdNow);

                    if (schedule.Any())
                    {
                        _ = Task.Run(() => { notificationService.WarningDefult("ไม่สามารถเลือก เวลานี้ได้"); });
                        return;
                    }
                }

                List<object> tmp = new()
                {
                    Tview.Date, // Day
                    Tview.Time, // Time View (10.00 - 11.00)
                    Tview.SelectTime // time (10.00)
                };
                await sessionStorage.SetItemAsync("ChooseDateTime_1", tmp);
                if (Role == "Admin" || Role == "Manager")
                {
                    navigationManager.NavigateTo($"/{Role}/UploadForChoosedate/{StaffID}/{RequestID}");
                }
                else
                {
                    navigationManager.NavigateTo($"/User/UploadForChoosedate/{id}");
                }
            }
            else
            {
                string alert = $"กรุณาเลือกวันเวลาเพื่อนัดหมายทำสัญญา";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                return;
            }
        }

        private void SelectTime(string time)
        {
            Tview.SelectTime = time;
            Tview.Time = $"{time} - {int.Parse(time.Substring(0, 2)) + 1}.00";
            Tview.Date = dateService.ChangeDate(SelectDateValue, "dd MMMM yyyy", Utility.DateLanguage_TH);
            Tview.DateTimeData = SelectDateValue;

            int Index2 = 0;
            if (ListTime.Count != 0)
            {
                for (int i = 0; i < ListTime.Count; i++)
                {
                    var _Listtime = ListTime[i];

                    if (_Listtime == time)
                    {
                        Index2 = i;
                    }
                }

            }
            IndexoffColor = Index2;
        }

        private async Task BackAsync()
        {
            if (Role == "Admin")
            {
                var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
                if (!string.IsNullOrEmpty(checkData))
                {
                    ApplyLoanModel ModelApplyLoan = await sessionStorage.GetItemAsync<ApplyLoanModel>(StorageName);
                    navigationManager.NavigateTo($"Admin/ManageLoanRequest/{true}/{ModelApplyLoan.LoanTypeID}/{ModelApplyLoan.ContractStatusId}");
                }
                else
                {
                    navigationManager.NavigateTo($"/Admin/AdminHome");
                }
            }
            else if (Role == "Manager")
            {
                navigationManager.NavigateTo($"/Manager/CheckRequestAgreement/{StaffID}");
            }
            else
            {
                navigationManager.NavigateTo("/LoanAgreement");
            }
        }

        private async Task GetDocStap1Async()
        {
            LoadingResultImg = true;
            try
            {
                var stap1 = await SaveFileAndImgService.GetDocByStapAsync(1, V_ReqCon);
                if (stap1 != null)
                {
                    ResultDocList.Add(stap1);
                }
                LoadingResultImg = false;
            }
            catch (Exception ex)
            {
                LoadingResultImg = false;
                await Error.ProcessError(ex);
            }
            StateHasChanged();
        }
    }

    public class TimeView
    {
        public decimal ContractId { get; set; }
        public string SelectTime { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string? Date { get; set; }
        public DateTime? DateTimeData { get; set; }
    }
}
