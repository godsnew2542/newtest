using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace LoanApp.Pages.User;

public partial class AttachmentForChoosedate
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

    #endregion

    #region Parameter
    [Parameter] public decimal RequestID { get; set; } = 0;
    [Parameter] public string Role { get; set; } = string.Empty;
    [Parameter] public string StaffID { get; set; } = string.Empty;

    #endregion

    #region Inject
    [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;

    #endregion

    private List<UploadModel> FileValue { get; set; } = new();
    private StepUserChooseDateModel StepsChooseDate { get; set; } = new();
    private List<VAttachmentRequired> ItemUploadImg { get; set; } = new();

    private string Storage_1 { get; } = "ChooseDateTime_1";
    private string Storage_2 { get; } = "FileChoosedate_2";
    private string Dataheader { get; set; } = string.Empty;
    private string Timeheader { get; set; } = string.Empty;
    private string DataApply { get; set; } = string.Empty;
    private int ContractStepId { get; set; } = 2;
    private List<string> DateValue { get; set; } = new();

    protected async override Task OnInitializedAsync()
    {
        StepsChooseDate.Current = 2;

        try
        {
            if (RequestID != 0)
            {
                LoanRequest? Req = await psuLoan.GetLoanRequestByLoanRequestId(RequestID);

                if (Req != null)
                {
                    ItemUploadImg = await psuLoan.GetListVAttachmentRequired(Req.LoanTypeId, ContractStepId);

                    Dataheader = await GetData(Req);

                    if (string.IsNullOrEmpty(StaffID))
                    {
                        StaffID = (!string.IsNullOrEmpty(Req.DebtorStaffId) ?
                            Req.DebtorStaffId : string.Empty);
                    }
                }
            }
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
            List<UploadModel>? checkFile = await sessionStorage.GetItemAsync<List<UploadModel>>(Storage_2);
            DataApply = await GetDateAsync();
            Timeheader = await GetTimeAsync();

            if (checkFile != null)
            {
                FileValue = checkFile;
            }

            /*ข้อมูลมีการเปลี่ยนแปลง บอก ui run ใหม่*/
            StateHasChanged();
        }
    }

    private async Task<string> GetData(LoanRequest data)
    {
        LoanType? loanType = await psuLoan.GetLoanTypeAsync(data.LoanTypeId);
        var LoanName = userService.GetLoanSubName(loanType);
        var Amount = data.LoanAmount;
        var Installment = data.LoanNumInstallments;
        var message = $"{LoanName} ยอดเงิน {String.Format("{0:n2}", Amount)} บาท จำนวน {Installment} งวด";
        return message;
    }

    private async Task<string> GetDateAsync()
    {
        var message = string.Empty;
        List<string>? checkDate = await GetSessionStorage_1();
        if (checkDate != null)
        {
            DateValue = checkDate;
            message = $"{DateValue[0]}";
        }
        else { }

        return message;
    }

    private async Task<string> GetTimeAsync()
    {
        var message = string.Empty;
        List<string>? checkDate = await GetSessionStorage_1();
        if (checkDate != null)
        {
            DateValue = checkDate;
            message = $"{DateValue[1]}";
        }

        return message;
    }

    private async Task<List<string>?> GetSessionStorage_1()
    {
        List<string>? checkDate = await sessionStorage.GetItemAsync<List<string>>(Storage_1);
        return checkDate;
    }

    private void Back()
    {
        if (Role == "Admin" || Role == "Manager")
        {
            navigationManager.NavigateTo($"/{Role}/UploadForChoosedate/{true}/{StaffID}/{RequestID}");
        }
        else
        {
            navigationManager.NavigateTo($"/User/UploadForChoosedate/{true}/{RequestID}");
        }
    }

    private async Task<string> GetHeaderModel()
    {
        LoanRequest? Req = await psuLoan.GetLoanRequestByLoanRequestId(RequestID);
        if (Req != null)
        {
            var message = $"คุณยืนยันการนัดหมายทำสัญญา วันที่ {DataApply} เวลา {Timeheader} น. ใช่หรือไม่";
            return message;
        }
        return string.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id">LoanRequestId</param>
    /// <returns></returns>
    private async Task NextPageAsync(decimal id)
    {
        decimal StatusId = 4; // รอทำสัญญา

        try
        {
            VLoanStaffDetail? Detail = await psuLoan.GetUserDetailAsync(StaffID);

            if (Detail == null)
            {
                string alert = $"กรุณาเข้าไปใส่ข้อมูล เกี่ยวกับธนาคาร ในข้อมูลส่วนตัว";
                //await JS.InvokeVoidAsync("displayTickerAlert", alert)
                await notificationService.Warning(alert);
                return;
            }

            if (id != 0)
            {
                ContractMain? contract = await psuLoan.GeContractMainByLoanRequestId(id);

                LoanRequest? req = await psuLoan.GetLoanRequestByLoanRequestId(id);

                if (DateValue.Any() && contract != null && req != null)
                {
                    contract.ContractDate = ChangeDateTimeDB(DateValue[0], DateValue[2]);
                    contract.ContractStatusId = StatusId;
                    req.LoanStatusId = StatusId;

                    if (FileValue.Any())
                    {
                        await SaveToFolderImagesAsync(FileValue);
                    }

                    await psuLoan.UpdateContractMain(contract);
                    await psuLoan.UpdateLoanRequest(req);

                    await SaveToHistoryAsync(req, contract);
                    await SetDataBySentEmail(req);
                    await SetMessageAsync();
                }
            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task SaveToHistoryAsync(LoanRequest req, ContractMain contract)
    {
        decimal Old_LoanStatusId = 2;
        string ModifyBy = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);

        await LogService.GetHisLoanRequestByRequestIDAsync(req.LoanRequestId, Old_LoanStatusId, ModifyBy);
        await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, Old_LoanStatusId, ModifyBy);
    }

    private DateTime ChangeDateTimeDB(string StringDate, string StringTime)
    {
        MonthModel monthM = new();
        var Hour = StringTime.Substring(0, 2);
        var Minutes = StringTime.Substring(3, 2);
        var date = StringDate.Split(' ');
        var month = "01";
        int year = Convert.ToInt32(date[2]) - 543;
        DateTime idate = DateTime.Now;

        for (int i = 0; i < monthM.Th.Length; i++)
        {
            var th = monthM.Th[i];
            if (th == date[1])
            {
                month = monthM.Number1[i];
            }
        }

        idate = new DateTime(year, Convert.ToInt32(month), Convert.ToInt32(date[0]), Convert.ToInt32(Hour), Convert.ToInt32(Minutes), 0);
        return idate;
    }

    private async Task SaveToFolderImagesAsync(List<UploadModel> ResultInfoList)
    {
        var STEP_ID = 2;
        try
        {
            if (!string.IsNullOrEmpty(StaffID))
            {
                /* Files/9_0001972/2 */
                var DIR = $"{Utility.Files_DIR}\\{RequestID}_{StaffID}\\{STEP_ID}";

                await SaveFileAndImgService.SaveToFolderImagesAsync(ResultInfoList, DIR, RequestID);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task SetDataBySentEmail(LoanRequest req)
    {
        try
        {
            var StaffDetail = userService.GetUserDetail(req.DebtorStaffId);
            var DebtorName = userService.GetFullNameNoTitleName(req.DebtorStaffId);

            var GuarantorDetail = userService.GetUserDetail(req.GuarantorStaffId);
            var GuarantoName = userService.GetFullNameNoTitleName(req.GuarantorStaffId);

            ApplyLoanModel loan = new();
            loan.LoanTypeID = req.LoanTypeId;

            if (StaffDetail != null && GuarantorDetail != null)
            {
                #region ผู้กู้
                if (!string.IsNullOrEmpty(StaffDetail.StaffEmail))
                {
                    var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                    var Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                    var email = MessageDebtor(Name, Email, DebtorName, GuarantoName, loan);
                    MailService.SendEmail(email);
                }
                #endregion

                #region ผู้ค้ำ
                if (!string.IsNullOrEmpty(GuarantorDetail.StaffEmail))
                {
                    var Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);
                    var Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                    var email = MessageGuarantor(Name, Email, DebtorName, GuarantoName, loan);
                    MailService.SendEmail(email);
                }
                #endregion
            }

            #region Admin
            List<VLoanStaffPrivilege> vLoanStaffPrivilege = await psuLoan.GetVLoanStaffPrivilegeByCampId(StaffDetail?.CampId);
            List<string> listEmailAdmin = psuLoan.GetAllEmailAdmin(vLoanStaffPrivilege);

            if (listEmailAdmin.Any())
            {
                var emailAdmin = MessageAdmin("การเจ้าหน้าที่วิทยาเขต",
                    //Utility.MailAdmin,
                    listEmailAdmin,
                    DebtorName,
                    GuarantoName,
                    loan);
                MailService.SendEmail(emailAdmin);
            }

            #endregion

        }
        catch (Exception)
        {
            throw;
        }
    }

    private MailModel MessageDebtor(string name, string emailUser, string debtorName, string guarantoName, ApplyLoanModel loan)
    {
        MailModel email = new();
        var loanName = userService.GetLoanType(loan.LoanTypeID);

        email.Title = $"(PSU LOAN) นัดหมายทำสัญญา ประเภท{loanName?.LoanParentName}";
        email.Name = name;
        email.Email = emailUser;
        email.Time = DateTime.Now;
        email.Message = $"เรียน คุณ{debtorName}" +
            $"\n" +
            $"คุณได้นัดหมายเพื่อทำสัญญากู้ยืมเงิน" +
            $"ในวันที่ {DateValue[0]} เวลา {DateValue[1]} น. " +
            $"\n" +
            $"กรุณามาให้ตรงตามเวลานัดหมาย และเตรียมเอกสาร/หลักฐานเพื่อทำสัญญากู้ยืมเงินมาให้ครบถ้วน" +
            $"\n";
        return email;
    }

    private MailModel MessageGuarantor(string name, string emailUser, string debtorName, string guarantoName, ApplyLoanModel loan)
    {
        MailModel email = new();
        var loanName = userService.GetLoanType(loan.LoanTypeID);

        email.Title = $"(PSU LOAN) นัดหมายทำสัญญา ประเภท{loanName?.LoanParentName}";
        email.Name = name;
        email.Email = emailUser;
        email.Time = DateTime.Now;
        email.Message = $"เรียน คุณ{guarantoName} โดย {debtorName}" +
            $"\n" +
            $"ได้นัดหมายเพื่อทำสัญญากู้ยืมเงิน" +
            $"ในวันที่ {DateValue[0]} เวลา {DateValue[1]} น. " +
            $"\n" +
            $"กรุณามาให้ตรงตามเวลานัดหมาย และเตรียมเอกสาร/หลักฐานเพื่อทำสัญญากู้ยืมเงินมาให้ครบถ้วน" +
            $"\n";
        return email;
    }

    private MailModel MessageAdmin(string name, List<string> listEmail, string debtorName, string guarantoName, ApplyLoanModel loan)
    {
        MailModel email = new();
        var loanName = userService.GetLoanType(loan.LoanTypeID);

        email.IsAdmin = true;
        email.Title = $"(PSU LOAN) นัดหมายทำสัญญา ประเภท{loanName?.LoanParentName}";
        email.Name = name;
        email.ListEmail = listEmail;
        email.Time = DateTime.Now;
        email.Message = $"{debtorName} (ผู้กู้) โดยมี {guarantoName} (ผู้ค้ำ) " +
            $"\n ได้ทำการเลือกวันนัดหมายเพื่อทำสัญญาในวันที่ {DateValue[0]} เวลา {DateValue[1]} น. ";

        return email;
    }

    private async Task SetMessageAsync()
    {
        List<object> Message = new();
        MessageModel mes = new();
        var titleMessage = "";
        string pages = string.Empty;

        mes.Action = "นัดหมายทำสัญญาสำเร็จ";
        //mes.Title = $"กรุณานำเอกสารในการทำสัญญาทั้งหมดมาให้พร้อมในวันทำสัญญา และ มาให้ตรงวันเวลาที่นัดหมาย วันที่ {DateValue[0]} เวลา {DateValue[1]} น."

        mes.TitleHtmlText = TitleRenderFunction(DateValue[0], DateValue[1]);

        if (Role == RoleTypeEnum.Admin.ToString())
        {
            pages = $"/Admin/AdminHome";
        }
        else
        {
            pages = $"/HomeUser";
        }
        mes.ToPage = pages;
        if (ItemUploadImg.Any())
        {
            for (int i = 0; i < ItemUploadImg.Count; i++)
            {
                var listName = ItemUploadImg[i];
                bool NoFile = true;
                var AttachmentName = listName.AttachmentNameThai;
                for (int x = 0; x < FileValue.Count; x++)
                {
                    var item = FileValue[x];
                    if (listName.AttachmentTypeId == item.AttachmentTypeId)
                    {
                        NoFile = false;
                    }
                }
                if (NoFile)
                {
                    titleMessage = $"เอกสารที่คุณยังไม่ได้อัปโหลดและต้องนำมาในวันนัดหมาย มีดังนี้";
                    var subMessage = $"{AttachmentName}  1 ฉบับ";
                    Message.Add(subMessage);
                }
            }

        }
        mes.TitleMessage = titleMessage;
        mes.Message = Message;
        await sessionStorage.SetItemAsync("Message", mes);
        navigationManager.NavigateTo("/Message", true);
    }

    private async Task<string> GetBookBank()
    {
        var mess = "ไม่พบข้อมูล";
        //var Detail = _context.LoanStaffDetails
        //        .Where(c => c.StaffId == StaffID)
        //        .FirstOrDefault();
        var Detail = await psuLoan.GetLoanStaffDetailByStaffId(StaffID);

        if (Detail != null)
        {
            if (string.IsNullOrEmpty(Detail.BookBankNo) &&
                string.IsNullOrEmpty(Detail.BookBankName) &&
                string.IsNullOrEmpty(Detail.BookBankBranch))
            {
                mess = "ไม่พบข้อมูลบัญชีธนาคาร";
            }
            else
            {
                mess = $"หมายเลขบัญชี {Detail.BookBankNo} {Detail.BookBankName} สาขา {Detail.BookBankBranch}";
            }
        }
        return mess;
    }
}
