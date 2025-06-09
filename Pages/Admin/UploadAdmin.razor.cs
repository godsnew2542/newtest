using LoanApp.DatabaseModel.LoanEntities;

using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Models.Data;
using LoanApp.Model.Settings;
using LoanApp.Services.IServices.LoanDb;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LoanApp.Pages.Admin;

public partial class UploadAdmin
{
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    [Parameter] public decimal RequestID { get; set; } = 0;

    [Inject] private IOptions<AppSettings> AppSettings { get; set; } = null!;
    [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;
    [Inject] private IPsuLoan psuLoan { get; set; } = null!;


    /// <summary>
    /// ชื่อประเภทเอกสาร
    /// </summary>
    private List<VAttachmentRequired> ItemUploadImg { get; set; } = new();
    private VLoanRequestContract? Request { get; set; } = new();
    private UploadModel ModelUploadPDF { get; set; } = new();
    private List<UploadModel> ResultInfoList { get; set; } = new();
    private List<UploadModel> ShowListResultImg { get; set; } = new();

    private decimal StepID { get; set; } = 2;
    private string RootFolder { get; set; } = string.Empty;
    private string DataUser { get; set; } = string.Empty;
    private string DataUserStatus { get; set; } = string.Empty;
    private decimal ShowResultImgTitle { get; set; } = 0;
    private bool IsConfirm { get; set; } = false;


    protected override async Task OnInitializedAsync()
    {
        IsConfirm = false;

        try
        {
            if (RequestID != 0)
            {
                Request = userService.GetVLoanRequestContract(RequestID);
                if (Request != null)
                {
                    /* Files/31_0001972 */
                    RootFolder = $"{Utility.Files_DIR}\\{RequestID}_{Request.DebtorStaffId}";

                    ItemUploadImg = await _context.VAttachmentRequireds
                        .Where(c => c.LoanTypeId == Request.LoanTypeId &&
                        c.ContractStepId == StepID)
                        .ToListAsync();

                    await CheckFolderFileUploadAsync();

                    LoanRequest? Req = await _context.LoanRequests
                        .Where(c => c.LoanRequestId == RequestID)
                        .FirstOrDefaultAsync();

                    if (Req != null)
                    {
                        DataUser = GetData(Req);
                        DataUserStatus = await DataStatusAsync(Req);
                    }
                }

            }
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private string GetData(LoanRequest data)
    {
        try
        {
            var NameUser = userService.GetFullName(data.DebtorStaffId);
            var Date = data.LoanRequestDate;

            var message = $"แบบคำขอกู้ของ {NameUser} " +
                $"( วันที่ยื่นกู้ {dateService.ChangeDate(Date!.Value, "dd MMMM yyyy HH:mm", Utility.DateLanguage_TH)} น.)";

            return message;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<string> DataStatusAsync(LoanRequest data)
    {
        try
        {
            decimal? Status = data.LoanStatusId;
            ContractStatus? step = await _context.ContractStatuses
                  .Where(c => c.ContractStatusId == Status)
                  .FirstOrDefaultAsync();

            var message1 = $" [ สถานะ : {step?.ContractStatusName} ]";

            return message1;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task CurrentRemoveListAsync(int value)
    {
        var checkData = await sessionStorage.GetItemAsStringAsync("SelectListUpload");
        if (checkData != null)
        {
            var SelectListUpload = await sessionStorage.GetItemAsync<UploadModel>("SelectListUpload");
            var myTodo = ResultInfoList.Find(x => x.Id == SelectListUpload.Id);
            if (myTodo != null)
            {
                ResultInfoList.Remove(myTodo);
                if (!myTodo.CollectByDB)
                {
                    File.Delete(SelectListUpload.Url!);
                }
                await sessionStorage.RemoveItemAsync("SelectListUpload");
            }

        }
    }

    private void SetCurrentData(DTEventArgs value)
    {
        UploadModel Upload = new()
        {
            Id = ResultInfoList.Count() + 1,
            Name = value.Params[0].ToString(),
            Url = value.Params[1].ToString(),
            TempImgName = value.Params[2].ToString(),
            AttachmentTypeId = (decimal)value.Params[3]
        };
        ResultInfoList.Add(Upload);
    }

    private void SetCurrentDataUploadPDF(UploadModel value)
    {
        ModelUploadPDF = new()
        {
            Id = value.Id,
            Name = value.Name,
            Url = value.Url,
            TempImgName = value.TempImgName,
            AttachmentTypeId = value.AttachmentTypeId
        };
    }

    private async Task CheckFolderFileUploadAsync()
    {
        try
        {
            var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

            // Files/31_0001972/2
            var dir = $"{physicalFilePath}\\{RootFolder}\\{StepID}";
            var di = new DirectoryInfo(dir);
            if (di.Exists)
            {
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    string result = Path.GetFileName(file);
                    string[] Split = result.Split('_');
                    var myTodo = ItemUploadImg.Find(x => x.AttachmentTypeId == Convert.ToInt32(Split[0]));

                    if (myTodo != null)
                    {
                        var path = $"{RootFolder}\\{StepID}\\{result}";
                        await AddDataAsync(path, RequestID, result, myTodo.AttachmentTypeId);
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task AddDataAsync(string path, decimal loanRequestId, string tempName, decimal AttachmentTypeId)
    {
        try
        {
            ContractAttachment? CAttachment = await _context.ContractAttachments
                .Where(c => c.AttachmentAddr == path)
                .Where(c => c.LoanRequestId == loanRequestId)
                .FirstOrDefaultAsync();

            if (CAttachment != null)
            {
                UploadModel Upload = new()
                {
                    Id = ResultInfoList.Count + 1,
                    Name = CAttachment.AttachmentFileName,
                    Url = CAttachment.AttachmentAddr,
                    TempImgName = tempName,
                    AttachmentTypeId = AttachmentTypeId,
                    CollectByDB = true
                };

                ResultInfoList.Add(Upload);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void Back()
    {
        navigationManager.NavigateTo($"Admin/Pdf/{RequestID}");
    }

    private async Task ConfirmAsync()
    {
        int StatusId = 9;
        string Remark = string.Empty;
        bool Icon;

        try
        {
            string Action;
            if (RequestID != 0 && !string.IsNullOrEmpty(RootFolder))
            {
                /* Save file สัญญา */
                decimal? AttachmentId = await SaveToFolderAttachmentAsync(ModelUploadPDF);

                // Save file other ที่ผู้กู้ไม่ได้ upload มาในวันที่เลือกวันทำสัญญา
                if (ItemUploadImg.Count != 0 && ResultInfoList.Count != 0)
                {
                    await SaveToFolderSTEP_TwoAsync(ResultInfoList);
                }

                await SaveToDBAsync(StatusId, AttachmentId);

                Action = "ทำสัญญาเสร็จสิ้น";
                Remark = "ผู้กู้สามารถติดตามผลได้จากเมนู สัญญาเงินกู้ และทางอีเมล";
                Icon = true;
                await SentMessageAsync(Action, Remark, Icon);
            }
            else
            {
                Action = "ไม่สามารถดำเนินการได้";
                Icon = false;
                await SentMessageAsync(Action, Remark, Icon);
            }

            // sent mail
            await SetDataBySentEmail();
            navigationManager.NavigateTo("/Message");
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private async Task SetDataBySentEmail()
    {
        try
        {
            var StaffDetail = userService.GetUserDetail(Request!.DebtorStaffId);
            var DebtorName = userService.GetFullNameNoTitleName(Request.DebtorStaffId);

            var GuarantorDetail = userService.GetUserDetail(Request.LoanRequestGuaranStaffId);
            var GuarantoName = userService.GetFullNameNoTitleName(Request.LoanRequestGuaranStaffId);

            ApplyLoanModel loan = new()
            {
                LoanTypeID = Request.LoanTypeId,
                LoanAmount = Request.ContractLoanAmount != null ?
                Request.ContractLoanAmount.Value :
                0,
                LoanInterest = Request.ContractLoanInterest,
                LoanNumInstallments = Request.ContractLoanNumInstallments != null ?
                (int)Request.ContractLoanNumInstallments :
                0
            };

            #region ผู้กู้
            if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
            {
                var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                var Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                MailModel email = MessageDebtor(Name,
                   Email,
                   DebtorName,
                   GuarantoName,
                   loan,
                   new List<string>());
                MailService.SendEmail(email);
            }
            #endregion

            #region ผู้ค้ำ
            if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
            {

                var Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);
                var Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                MailModel email = MessageDebtor(Name,
                   Email,
                   DebtorName,
                   GuarantoName,
                   loan,
                   new List<string>());
                MailService.SendEmail(email);
            }
            #endregion


            #region Admin
            List<VLoanStaffPrivilege> vLoanStaffPrivilege = await psuLoan.GetVLoanStaffPrivilegeByCampId(StaffDetail?.CampId);
            List<string> listEmailAdmin = psuLoan.GetAllEmailAdmin(vLoanStaffPrivilege);

            if (listEmailAdmin.Any())
            {
                MailModel emailAdmin = MessageDebtor("การเจ้าหน้าที่วิทยาเขต",
                       //Utility.MailAdmin,
                       string.Empty,
                       DebtorName,
                       GuarantoName,
                       loan,
                       listEmailAdmin,
                       true);
                MailService.SendEmail(emailAdmin);
            }

            #endregion
        }
        catch (Exception)
        {
            throw;
        }
    }

    private MailModel MessageDebtor(string name, string emailUser, string debtorName, string guarantoName, ApplyLoanModel loan, List<string> listEmail, bool IsAdmin = false)
    {
        MailModel email = new()
        {
            IsAdmin = IsAdmin,
            Title = $"(PSU LOAN) ทำสัญญากู้ยืมเงินเรียบร้อย รอรับเงินกู้",
            Name = name,
            Email = emailUser,
            Time = DateTime.Now,
            ListEmail = listEmail,
        };

        var loanName = userService.GetLoanType(loan.LoanTypeID);

        email.Message = $"เรียนคุณ{name}" +
            $"ตามที่ คุณ{debtorName} (ผู้กู้) ได้ยื่นกู้ โดยประสงค์ให้ คุณ{guarantoName} (ผู้ค้ำ) เป็นผู้ค้ำนั้น" +
            $"\n" +
            $"ได้ทำสัญญากู้ยืมเงินเรียบร้อยแล้ว เมื่อวันที่ {dateService.ChangeDate(email.Time, "dd MMMM yyyy", Utility.DateLanguage_TH)} เวลา {dateService.ChangeDate(email.Time, "HH:mm", Utility.DateLanguage_TH)} น." +
            $"\n" +
            $"\nโดยมีรายละเอียดการกู้ ดังนี้" +
            $"\n ประเภท {loanName?.LoanParentName} " +
            $"\n วงเงินที่ขอกู้จำนวน {String.Format("{0:n2}", loan.LoanAmount)} บาท " +
            $"\n ดอกเบี้ย {loan.LoanInterest} %" +
            $"\n จำนวนงวด {loan.LoanNumInstallments} งวด " +
            $"\n " +
            $"ขณะนี้อยู่ในระหว่างการดำเนินการโอนเงินจากกองคลัง" +
            $"\n " +
            $"สามารถดูรายละเอียดการกู้/การค้ำประกันได้ที่ loan.psu.ac.th " +
            $"\n อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ";
        return email;
    }

    private async Task SaveToFolderSTEP_TwoAsync(List<UploadModel> InfoList)
    {
        try
        {
            if (InfoList.Any())
            {
                var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

                var dirToSave = $"{physicalFilePath}\\{RootFolder}\\{StepID}";
                Utility.CheckFolder(dirToSave);

                for (int i = 0; i < InfoList.Count; i++)
                {
                    var ele = InfoList[i];
                    var fileName = $"{ele.AttachmentTypeId}_{ele.TempImgName}";
                    var path_To = ele.Url;

                    if (!ele.CollectByDB && ele.AttachmentTypeId != 0) // รอแก้ไขเพิ่ม
                    {
                        var filePath_From = $"{dirToSave}\\{fileName}";
                        var KeepPath = $"{RootFolder}\\{fileName}";
                        File.Move(path_To!, filePath_From);
                        await SaveFileAndImgService.SaveImagesAsync(KeepPath, ele.Name!, RequestID);
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<decimal?> SaveToFolderAttachmentAsync(UploadModel file)
    {
        try
        {
            var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

            var dirToSave = $"{physicalFilePath}\\{RootFolder}\\{Utility.ATTACHMENT_DIR}";
            Utility.CheckFolder(dirToSave);
            decimal? id = 0;

            var fileName = $"{file.AttachmentTypeId}_{file.TempImgName}";
            var path_To = file.Url;
            var filePath_From = $"{dirToSave}\\{fileName}";
            var KeepPath = $"{RootFolder}\\{Utility.ATTACHMENT_DIR}\\{fileName}";

            File.Move(path_To!, filePath_From);
            id = await SaveFileAndImgService.SaveImagesAsync(KeepPath, file.Name!, RequestID);
            if (id == null || id == 0)
            {
                var data = await SaveFileAndImgService.GetContractAttachmentAsync(KeepPath);
                id = data?.AttachmentId;
            }
            return id;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task SaveToDBAsync(int StatusId, decimal? AttachmentId)
    {
        try
        {
            ContractMain? Contract = _context.ContractMains
                .Where(c => c.LoanRequestId == RequestID)
                .FirstOrDefault();
            if (Contract != null)
            {
                Contract.ContractStatusId = StatusId;
                Contract.AdminStaffIdUpload = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
                Contract.AdminUploadDate = DateTime.Now;

                _context.Update(Contract);
                await _context.SaveChangesAsync();
            }

            LoanRequest? req = _context.LoanRequests
                .Where(c => c.LoanRequestId == RequestID)
                .FirstOrDefault();

            if (req != null)
            {
                req.LoanAttachmentId = AttachmentId;
                _context.Update(req);
                await _context.SaveChangesAsync();
            }
            await SaveToHistoryAsync(req, Contract);
        }
        catch (Exception)
        {
            throw;
        }

    }

    private async Task SaveToHistoryAsync(LoanRequest? req, ContractMain? contract)
    {
        try
        {
            decimal Old_LoanStatusId = 4;
            string ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

            if (req != null)
            {
                await LogService.GetHisLoanRequestByRequestIDAsync(req.LoanRequestId, Old_LoanStatusId, ModifyBy);
            }

            if (contract != null)
            {
                await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, Old_LoanStatusId, ModifyBy);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task SentMessageAsync(string Action, string Remark, bool Icon)
    {
        List<object> Message = new();
        MessageModel mes = new();

        mes.Action = Action;
        mes.Remark = Remark;
        mes.Icon = Icon;
        mes.ToPage = $"/Admin/ManageLoanRequest";
        mes.Message = Message;
        mes.ButtonText = $"กลับหน้าจัดการคำขอกู้";
        await sessionStorage.SetItemAsync("Message", mes);
    }

    private void ShowImg(List<UploadModel> LResultImg, decimal AttTypeId)
    {
        ShowListResultImg = new();
        ShowResultImgTitle = AttTypeId;

        if (LResultImg.Count != 0)
        {
            for (int i = 0; i < LResultImg.Count; i++)
            {
                var resultImg = LResultImg[i];

                if (resultImg.AttachmentTypeId == AttTypeId)
                {
                    ShowListResultImg.Add(resultImg);
                }
            }
        }
    }

    private string GetUrl(UploadModel upload)
    {
        var path = string.Empty;
        if (upload.CollectByDB)
        {
            path = $"{AppSettings.Value.RequestFilePath}\\{upload.Url}";
        }
        else
        {
            path = $"{AppSettings.Value.RequestFilePath}\\Temp\\{upload.TempImgName}";
        }
        return path;
    }
}
