using AntDesign.TableModels;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace LoanApp.Pages.Admin
{
    public partial class ManageLoanAgreementV2
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Parameter] public decimal StatusID { get; set; } = 0;

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private LoanApp.IServices.IUtilityServer utilityServer { get; set; } = null!;
        #endregion

        private IOrderedQueryable<VLoanRequestContract> loanRequestContractsTemp { get; set; } = null!;
        private List<VLoanRequestContract> newLoanRequestContracts { get; set; } = new();
        private List<LoanType> LoanTypeList { get; set; } = new();
        private List<ContractStatus> Status { get; set; } = new();
        private SearchModel Search { get; set; } = new();

        private decimal StaId { get; set; } = 0;
        private decimal TypeID { get; set; } = 0;
        private string? SearchView { get; set; } = null;
        private decimal[] AllowedStatus { get; } = new[] { 6m, 7m, 8m, 80m, 81m, 82m, 99m, 98m, 200m };
        private int _pageIndex { get; set; } = 1;
        private bool Lading { get; set; } = false;

        private List<VLoanRequestContract> ListRecord { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow))
                {
                    string adminCapmId = StateProvider.CurrentUser.CapmSelectNow;

                    loanRequestContractsTemp = _context.VLoanRequestContracts
                    .Where(c => AllowedStatus.Contains(c.CurrentStatusId!.Value))
                    .Where(c => (c.ContractDate == null) || ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                    .Where(c => c.DebtorCampusId == adminCapmId)
                    .OrderBy(c => c.CurrentStatusId);
                }

                if (StatusID != 0)
                {
                    StaId = StatusID;
                    await DataTableV2(Search.Title, StaId, TypeID);
                }
                else
                {
                    await DataTableV2(Search.Title, StaId, TypeID);
                }

                //LoanTypeList = await psuLoan.GetAllLoanType(1)
                LoanTypeList = await psuLoan.GetAllLoanType();

                LoanTypeList.Insert(0, new LoanType()
                {
                    LoanTypeId = 0,
                    LoanTypeName = "ทุกประเภท"
                });

                Status = await psuLoan.GetAllContractStatus(AllowedStatus.ToList());

                Status.Insert(0, new ContractStatus()
                {
                    ContractStatusId = 0,
                    ContractStatusName = "ทุกสถานะ"
                });
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
                if (StatusID != 0)
                {
                    int index = Status.FindIndex(a => a.ContractStatusId == StatusID);
                    await JS.InvokeVoidAsync("SelectedTagName", index);
                    StateHasChanged();
                }
            }
        }

        public static string SetDay(int intYear, int month, int modMonth)
        {
            string StringMonth = $"{modMonth}";
            MonthModel _month = new();
            string payDate = string.Empty;

            if (month == 13)
            {
                month = 1;
                ++intYear;
            }

            if (modMonth == 0)
            {
                StringMonth = Utility.ChangeMonthToInt("12", _month.Th);
            }
            else if (modMonth > 0 && modMonth < 12)
            {
                StringMonth = Utility.ChangeMonthToInt($"{modMonth}", _month.Th);
            }

            if (modMonth == 0)
            {
                payDate = $"{DateTime.DaysInMonth(intYear, 12)} {StringMonth} {intYear}";
            }
            else
            {
                payDate = $"{DateTime.DaysInMonth(intYear, modMonth)} {StringMonth} {intYear}";
            }

            return payDate;
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
                        //DateTime oDate =
                        //    dateService.ConvertToDateTime(ListPaymentTransaction[ListPaymentTransaction.Count - 1].payDate);

                        DateTime? oDate = ListPaymentTransaction
                        .Where(c => c.InstallmentNo == (ListPaymentTransaction.Max(x => x.InstallmentNo)))
                        .Select(x => x.PayDate)
                        .FirstOrDefault();

                        int IntYear = Convert.ToInt32(dateService.ChangeDate(oDate, "yyyy", Utility.DateLanguage_EN)) + 543;
                        int month = Convert.ToInt32(dateService.ChangeDate(oDate, "MM", Utility.DateLanguage_EN)) + 1;
                        int ModMonth = month % 12;
                        return SetDay(IntYear, month, ModMonth);
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

        private void Check(VLoanRequestContract data)
        {
            navigationManager.NavigateTo($"/Admin/CheckPremise/{data.LoanRequestId}");
        }

        /// <summary>
        /// check status 200
        /// </summary>
        /// <param name="LoanRequestId"></param>
        /// <param name="IsNewupload">true == status 200</param>
        private void UploadAgreementPremise(decimal LoanRequestId, bool IsNewupload = false)
        {
            if (!IsNewupload)
            {
                navigationManager.NavigateTo($"/Admin/AgreementPremise/{LoanRequestId}/{3}/{(int)PageControl.AdminManageLoanAgreement}");
            }
            else
            {
                navigationManager.NavigateTo($"/Admin/AgreementPremise/{LoanRequestId}/{3}/{(int)PageControl.AdminManageLoanAgreement}/{true}");
            }
        }

        private void SeeDetail(RowData<VLoanRequestContract> row)
        {
            VLoanRequestContract data = row.Data;

            if ((new List<decimal>() { 6, 200 }).Contains(data.CurrentStatusId!.Value))
            {
                return;
            }
            else if (data.CurrentStatusId == 7)
            {
                Check(data);
            }
            //else if (data.CurrentStatusId == 6)
            //{
            //    UploadAgreementPremise(data.LoanRequestId);
            //}
            //else if (data.CurrentStatusId == 200)
            //{
            //    UploadAgreementPremise(data.LoanRequestId, true);
            //}
            else
            {
                navigationManager.NavigateTo($"/Admin/AgreementDetail/{data.LoanRequestId}");
            }
        }

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

        private bool GetCheckRequestId(VLoanRequestContract requestContract)
        {
            return ListRecord.Any(c => c.LoanRequestId == requestContract.LoanRequestId);
        }

        private void CheckboxClickedV2(decimal? requestId, VLoanRequestContract requestContract)
        {
            if (ListRecord.Any())
            {
                var myTodo = ListRecord.Find(x => x.LoanRequestId == requestId);

                if (myTodo != null)
                {
                    ListRecord.Remove(myTodo);
                }
                else
                {
                    ListRecord.Add(requestContract);
                }
            }
            else
            {
                ListRecord.Add(requestContract);
            }
        }

        private void SetOrClearCheckedAsync(bool data)
        {
            ListRecord = new();
            if (data)
            {
                ListRecord = newLoanRequestContracts;
            }
        }

        private async Task SentEmailWarning(List<VLoanRequestContract> data)
        {
            Lading = true;
            await Task.Delay(1);
            StateHasChanged();

            foreach (var item in data)
            {
                VLoanStaffDetail? staff = await psuLoan.GetUserDetailAsync(item.DebtorStaffId);

                if (staff != null && !string.IsNullOrEmpty(staff.StaffEmail))
                {
                    var name = userService.GetFullNameNoTitleName(staff.StaffId);
                    string userManualLink = "https://loan.psu.ac.th/Files/Manual/Loan_User_Manual/LOAN_User_Manual/Newtopic18.html";
                    int deadLineNumber = 15;

                    MailModel email = new()
                    {
                        IsBodyHtml = true,
                        Title = $"(PSU LOAN) แจ้งเตือนกรุณาส่งหลักฐานหลังได้รับเงินกู้",
                        Name = name,
                        Time = DateTime.Now,
                        Email = staff.StaffEmail,
                        Message = $"<div>\r\n  เรียน คุณ{name} </div>" +
                        $"<div>\r\n  ตามที่ท่านได้ยื่นกู้เงินเงินสวัสดิการของมหาวิทยาลัย\r\n " +
                        $"<strong>เลขที่สัญญา {item.ContractNo} &nbsp;</strong> " +
                        $"\r\n และได้รับเงินกู้เรียบร้อยแล้วนั้น\r\n </div>\r\n" +
                        $"<br>\r\n" +
                        $"<div>\r\n  ตามประกาศสวัสดิการเงินกู้ของมหาวิทยาลัย\r\n  " +
                        $"<strong>ท่านจะต้องยื่นหลักฐานใบเสร็จรับเงิน/หลักฐานค่าใช้จ่าย/สำเนากรมธรรม์</strong>" +
                        $"\r\n แล้วแต่กรณีประเภทเงินกู้ ภายใน 30 วัน นับจากวันรับเงินกู้\r\n </div>\r\n" +
                        $"<br>\r\n" +
                        $"<div>\r\n  ดังนั้น ขอให้ท่านยื่นเอกสารดังกล่าว" +
                        $"\r\n <span style=\"color: rgb(200, 38, 19);\">" +
                        $"\r\n <strong>ภายใน {deadLineNumber} วันนับจากวันที่ท่านได้รับอีเมลฉบับนี้&nbsp;</strong>" +
                        $"\r\n </span>\r\n ให้เรียบร้อย\r\n </div>\r\n" +
                        $"<br>\r\n" +
                        $"<div>\r\n  โดยสามารถดำเนินการได้ที่เมนู\r\n  " +
                        $"&nbsp; &quot;<strong>สัญญากู้ยืมเงิน</strong>&quot; &nbsp;" +
                        $"\r\n  บนระบบ ( กดปุ่ม &quot;อัปโหลดหลักฐาน&quot;)\r\n </div>\r\n" +
                        $"<div>\r\n  ศึกษาวิธีการได้ที่\r\n  <u>\r\n" +
                        $"<a\r\n href=\"{userManualLink}\"\r\n  target=\"_blank\" rel=\"noopener noreferrer\"\r\n style=\"color: var(--communication-foreground,rgba(0, 90, 158, 1));\">" +
                        $"{userManualLink} </a>\r\n </u>\r\n </div>\r\n" +
                        $"<br>\r\n" +
                        $"<div>\r\n  ***หากท่านไม่ดำเนินการภายในเวลาที่กำหนด\r\n  " +
                        $"<strong>\r\n &nbsp;ทางมหาวิทยาลัยจะเรียกเงินกู้คืนทั้งจำนวน พร้อมคิดดอกเบี้ยร้อยละ 7.5" +
                        $"\r\n ต่อปี ตามประกาศมหาวิทยาลัยสงขลานครินทร์ " +
                        $"\r\n เรื่องสวัสดิการเงินกู้และเงินยืมบุคลากรมหาวิทยาลัยสงขลายนครินทร์ " +
                        $"\r\n ฉบับลง{Utility.LoanDocDate} \r\n </strong>\r\n </div>\r\n" +
                        $"<br>\r\n" +
                        $"<div>\r\n อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ\r\n </div>\r\n" +
                        $"<div>\r\n  " +
                        $"หากมีข้อสงสัยหรือต้องการสอบถามรายละเอียดเพิ่มเติม\r\n  กรุณาติดต่อการเจ้าหน้าที่วิทยาเขตที่ท่านสังกัด\r\n" +
                        $"</div>\r\n" +
                        $"<br>\r\n" +
                        $"<div>\r\n  " +
                        $"ขอแสดงความนับถือ\r\n" +
                        $"</div>",
                    };

                    //email.IsDev = utilityServer.CheckDBtest()

                    if (utilityServer.CheckDBtest())
                    {
                        email.CarbonCopy = new() {
                        "naparat.h@psu.ac.th",
                        "duangthida.c@psu.ac.th"
                    };
                    }

                    MailService.SendEmail(email);
                }
            }

            _= Task.Run(() => notificationService.SuccessDefult("แจ้งเตือน email สำเร็จ"));

            Lading = false;
            await Task.Delay(1);
            StateHasChanged();
        }

        private void CallbackLoading(bool e)
        {
            Lading = e;
            StateHasChanged();
        }
    }
}
