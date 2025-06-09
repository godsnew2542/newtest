using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using Radzen;
using System.Globalization;

namespace LoanApp.Services.Services
{
    public class DateService : IDateService
    {
        public string ChangeDate(DateTime? oDate, string fomathDate, string language)
        {
            if (oDate == null)
            {
                return string.Empty;
            }
            string showDate = oDate.Value.ToString(fomathDate, new CultureInfo(language));
            return showDate;
        }

        public DateTime ConvertToDateTime(DateTime? date)
        {
            DateTime oDate = Convert.ToDateTime(date);
            return oDate;
        }

        public string ChangeDateByString(string? StringDate, string?[]? MonthStering = null, string SetShowDate = "")
        {
            string ShowDate = SetShowDate;
            DateModel date = Utility.ChangeDateMonth(StringDate, MonthStering);
            if (!string.IsNullOrEmpty(date.Day))
            {
                ShowDate = $"{Convert.ToDecimal(date.Day)} {date.Month} {date.Year}";
            }

            return ShowDate;
        }

        public bool DateRender(DateRenderEventArgs args, DateTime[] SetDate)
        {
            if (SetDate.Contains(args.Date))
            {
                args.Attributes.Add("style", "background-color: #ff6d41; border-color: white;");
            }
            args.Disabled = SetDate.Contains(args.Date);
            return args.Disabled;
        }

        public List<DateTime> SetDisabledDates(int LastDay, int Select_year, int Select_month)
        {
            List<DateTime> _dates = new List<DateTime>();
            for (int i = 0; i < LastDay; i++)
            {
                DateTime Date = new DateTime(Select_year, Select_month, i + 1);
                _dates.Add(Date);
            }
            return _dates;
        }

        public List<DateTime> SetDisabledWeekday(int LastDay, int Select_year, int Select_month, List<DateTime> listDates)
        {
            List<DateTime> _dates = new List<DateTime>();
            _dates = listDates;

            for (int i = 0; i < LastDay; i++)
            {
                DateTime Date = new DateTime(Select_year, Select_month, i + 1);

                if (Utility.Weekday(Date, DayOfWeek.Friday) == 1 ||
                    Utility.Weekday(Date, DayOfWeek.Friday) == 2)
                {
                    _dates.Add(Date);
                }
            }
            return _dates;
        }
    }
}
