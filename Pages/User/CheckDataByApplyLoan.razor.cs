using LoanApp.Model.Models;
using LoanApp.Components.Document;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.Options;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Settings;

namespace LoanApp.Pages.User
{
    public partial class CheckDataByApplyLoan
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #endregion

        #region Inject
        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;
        [Inject] private IServices.IUtilityServer utilityServer { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] IConfiguration Config { get; set; } = null!;

        #endregion

        private LoanType? loanData { get; set; } = new();
        private ApplyLoanModel info { get; set; } = new();
        private List<UploadModel> FileUpload { get; set; } = new();
        private DateModel date { get; set; } = new();
        private LoanRequest? Request { get; set; } = new();
        private ContractAttachment AttachmentFile { get; set; } = new();

        private MonthModel model_month { get; set; } = new();
        private VLoanStaffDetail? StaffDetail { get; set; } = new();
        private VLoanStaffDetail? GuarantorDetail { get; set; } = new();
        public VStaffAddress StaffAssress { get; set; } = new();
        public VStaffFamily? StaffFamilies { get; set; } = new();
        public List<VStaffChild> StaffChild { get; set; } = new();
        private StepsUserApplyLoanModel StepsUser { get; set; } = new();
        private DocumentOptionModel OptionLoanAgreement { get; set; } = new();
        private RequestAttrachment RefRequestAttrachment { get; set; } = new();

        public string? LoanAttrachmentHTML { get; set; }
        public string AuthState_StaffID { get; set; } = string.Empty;
        private bool IsMobile { get; set; } = false;
        private string Message { get; set; } = string.Empty;
        private bool IsLoading { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            StepsUser.Current = 2;

            try
            {
                var checkData = await sessionStorage.GetItemAsStringAsync("FromLoan_1");
                var checkUploadImg = await sessionStorage.GetItemAsStringAsync("FromLoan_2");

                if (!string.IsNullOrEmpty(checkData))
                {
                    info = await sessionStorage.GetItemAsync<ApplyLoanModel>("FromLoan_1");

                    loanData = await psuLoan.GetLoanTypeAsync(info.LoanTypeID);

                    if (loanData != null)
                    {
                        AuthState_StaffID = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

                        if (!string.IsNullOrEmpty(AuthState_StaffID))
                        {
                            await SetData(AuthState_StaffID, info.GuarantorId);
                        }
                    }

                    await SetTHDataAsync(info);
                }

                if (!string.IsNullOrEmpty(checkUploadImg))
                {
                    FileUpload = await sessionStorage.GetItemAsync<List<UploadModel>>("FromLoan_2");
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
                IsMobile = await JS.InvokeAsync<bool>("isDevice");
                StateHasChanged();
            }
        }

        private async Task SetData(string DebtorId, string GuarantorId)
        {
            try
            {
                StaffDetail = await psuLoan.GetUserDetailAsync(DebtorId);
                GuarantorDetail = await psuLoan.GetUserDetailAsync(GuarantorId);
                StaffChild = await psuLoan.GetListVStaffChildAsync(DebtorId);

                var Address = await psuLoan.GetUserAddressesAsync(DebtorId);
                StaffFamilies = await psuLoan.GetUserFamilyAsync(DebtorId);

                if (Address != null)
                {
                    StaffAssress = Address;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task SetTHDataAsync(ApplyLoanModel loan)
        {
            LoanRequest? Lrequest = await psuLoan.GetLoanRequestByLoanRequestId(loan.LoanRequestId);
            if (Lrequest != null)
            {
                info.LoanMonthlyInstallment = Lrequest.LoanInstallment;
            }
        }

        private string ChangeDate(string? StringDate, string[] monthString)
        {
            date.ShowDate = "-";
            DateModel DateData = Utility.ChangeDateMonth(StringDate, monthString);
            if (!string.IsNullOrEmpty(DateData.Day))
            {
                date.ShowDate = $"{DateData.Day} {DateData.Month} {DateData.Year}";
            }
            return date.ShowDate;
        }

        private async Task DownloadPdfAsync()
        {
            Message = string.Empty;
            var fileName = $"แบบคำขอกู้.pdf";
            var html = await GenerateHTMLAsync();
            if (!string.IsNullOrEmpty(html))
            {
                byte[] pdfBuffer = GeneratePDFService.GeneratePDF(html);
                await GeneratePDFService.SaveFilePDFAsync(pdfBuffer, fileName);
            }
            else
            {
                Message = "เกิดข้อผิดพลาดในการ ดาวน์โหลด PDF";
            }
        }

        private string GetStaffSalaryId(string? staffId)
        {
            var StaffSalaryId = "ไม่มี";
            VSStaff? _VSStaff = userService.GetVSStaff(staffId);

            if (_VSStaff != null)
            {
                if (!string.IsNullOrEmpty(_VSStaff.StaffSalaryId))
                {
                    StaffSalaryId = _VSStaff.StaffSalaryId;
                }
            }
            return StaffSalaryId;
        }

        private void BackPage()
        {
            if (FileUpload.Count != 0)
            {
                navigationManager.NavigateTo($"/uploaddoc/Edit/1");
            }
            else
            {
                navigationManager.NavigateTo($"/Applyloan/Edit/{info.LoanRequestId}");
            }
        }

        private async Task NextPageAsync()
        {
            IsLoading = true;
            try
            {
                if (FileUpload.Count != 0)
                {
                    await SaveToFolderImagesAsync();
                }

                await SaveToDbAsync();
                await SetDataBySentEmail();
                await SetMessageAsync();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task SaveToDbAsync()
        {
            try
            {
                Request = await psuLoan.GetLoanRequestByLoanRequestId(info.LoanRequestId);

                if (Request != null)
                {
                    var _VSStaff = userService.GetVSStaff(Request?.DebtorStaffId);

                    Request!.LoanStatusId = 1;
                    Request.LoanRequestDate = DateTime.Now;
                    Request.LoanCreatedDate = DateTime.Now;
                    Request.StaffSalaryId = _VSStaff?.StaffSalaryId;
                    Request.DebtorCitizenId = _VSStaff?.CitizenId;

                    _context.Update(Request);
                    await _context.SaveChangesAsync();

                    string ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
                    await LogService.GetHisLoanRequestByRequestIDAsync(Request.LoanRequestId, 0m, ModifyBy);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task SaveToFolderImagesAsync()
        {
            var STEP_ID = 1;
            /* Files/9_0001972/1 */
            var DIR = $"{Utility.Files_DIR}\\{info.LoanRequestId}_{AuthState_StaffID}\\{STEP_ID}";

            var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

            var dirToSave = $"{physicalFilePath}\\{DIR}";

            try
            {
                Utility.CheckFolder(dirToSave);

                for (var i = 0; i < FileUpload.Count; i++)
                {
                    var ele = FileUpload[i];
                    var fileName = $"{ele.AttachmentTypeId}_{ele.TempImgName}";
                    var path_To = ele.Url;

                    var filePath_From = Path.Combine(dirToSave, fileName);
                    File.Move(path_To!, filePath_From);
                    var KeepPath = $"{DIR}\\{fileName}";
                    await SaveImagesAsync(KeepPath, ele.Name!);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task SaveImagesAsync(string path, string filename)
        {
            AttachmentFile = new ContractAttachment();
            var count_ContractAttachment = _context.ContractAttachments.Count();

            AttachmentFile.AttachmentId = count_ContractAttachment + 1;
            AttachmentFile.AttachmentFileName = filename;
            AttachmentFile.AttachmentAddr = path;
            AttachmentFile.LoanRequestId = info.LoanRequestId;

            _context.ContractAttachments.Add(AttachmentFile);
            await _context.SaveChangesAsync();
        }

        private async Task OpenPdfAsync()
        {
            LoanAttrachmentHTML = string.Empty;
            LoanAttrachmentHTML = await GenerateHTMLAsync();
        }

        protected async Task<string> GenerateHTMLAsync()
        {
            var HtmlText = string.Empty;
            var HeadHTML = await JS.InvokeAsync<string>("headHTML");
            var ScriptHTML = await JS.InvokeAsync<string>("scriptHTML");

            if (RefRequestAttrachment != null)
            {
                HtmlText = await RefRequestAttrachment.GetBoByHtmlAsync();
            }

            var Html = $"{HeadHTML} <br/> {HtmlText} <br/> {ScriptHTML}";
            return Html;
        }

        private async Task SetDataBySentEmail()
        {
            try
            {
                var DebtorName = userService.GetFullNameNoTitleName(StaffDetail?.StaffId);
                var GuarantoName = userService.GetFullNameNoTitleName(GuarantorDetail?.StaffId);

                ApplyLoanModel loan = new()
                {
                    LoanTypeID = info.LoanTypeID,
                    LoanAmount = info.LoanAmount,
                    LoanInterest = loanData?.LoanInterest,
                    LoanNumInstallments = info.LoanNumInstallments
                };

                if (StaffDetail != null && GuarantorDetail != null)
                {
                    #region ผู้กู้
                    if (!string.IsNullOrEmpty(StaffDetail.StaffEmail))
                    {
                        var Email = string.Empty;
                        var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                        Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                        var email = MessageDebtor(Name, Email, DebtorName, GuarantoName, loan);
                        MailService.SendEmail(email);
                    }
                    #endregion

                    #region ผู้ค้ำ
                    if (!string.IsNullOrEmpty(GuarantorDetail.StaffEmail))
                    {
                        var Email = string.Empty;
                        var Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);
                        Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

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

        private MailModel MessageDebtor(string name,
            string emailUser,
            string debtorName,
            string guarantoName,
            ApplyLoanModel loan)
        {
            MailModel email = new();
            var loanName = userService.GetLoanType(loan.LoanTypeID);
            var InterestMess = loan.LoanInterest == 0 ? "ไม่มี" : $"{loan.LoanInterest} ";

            email.Title = $"(PSU LOAN) ยืนยันการยื่นคำขอกู้สำเร็จ";
            email.Name = name;
            email.Email = emailUser;
            email.Time = DateTime.Now;
            email.Message = $"เรียน คุณ{name}" +
                            $"\n" +
                            $"\n คุณได้ยืนยันการยื่นคำขอกู้ เมื่อวันที่ " +
                            $"{dateService.ChangeDate(email.Time, "dd MMMM yyyy", Utility.DateLanguage_TH)} " +
                            $"เวลา {dateService.ChangeDate(email.Time, "HH:mm", Utility.DateLanguage_TH)} น. สำเร็จ" +
                            $"\n ขณะนี้อยู่ในระหว่างพิจารณาตรวจสอบ" +
                            $"\n" +
                            $"\n มีรายละเอียดการยื่นกู้ ดังนี้ " +
                            $"\n ประเภทการกู้ : {loanName?.LoanParentName} " +
                            $"\n จำนวนเงินที่ต้องการกู้ : {String.Format("{0:n2}", loan.LoanAmount)} บาท " +
                            $"\n อัตราดอกเบี้ย : {InterestMess} %" +
                            $"\n จำนวนงวดที่ต้องการผ่อน : {loan.LoanNumInstallments} งวด" +
                            $"\n" +
                            $"\n โปรดรอผลพิจารณาตรวจสอบหรือ ติดตามการดำเนินการได้ที่เมนู 'สัญญากู้ยืมเงิน'" +
                            $"\n" +
                            $"\n อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ " +
                            $"\n หากมีข้อสงสัยหรือต้องการสอบถามรายละเอียดเพิ่มเติม กรุณาติดต่อการเจ้าหน้าที่วิทยาเขตที่สังกัด " +
                            $"\n" +
                            $"\n ขอแสดงความนับถือ";

            return email;
        }

        private MailModel MessageGuarantor(string name,
            string emailUser,
            string debtorName,
            string guarantoName,
            ApplyLoanModel loan)
        {
            MailModel email = new();
            var loanName = userService.GetLoanType(loan.LoanTypeID);
            var InterestMess = loan.LoanInterest == 0 ? "ไม่มี" : $"{loan.LoanInterest} %";

            email.Title = $"(PSU LOAN) มีคำขอกู้ที่ท่านถูกประสงค์ให้เป็นผู้ค้ำ";
            email.Name = name;
            email.Email = emailUser;
            email.Time = DateTime.Now;
            email.Message = $"เรียน คุณ{name}" +
                            $"\n" +
                            $"\n คุณ{debtorName}(ผู้กู้) ได้ยืนยันการยื่นคำขอกู้เมื่อวันที่ " +
                            $"{dateService.ChangeDate(email.Time, "dd MMMM yyyy", Utility.DateLanguage_TH)} " +
                            $"เวลา {dateService.ChangeDate(email.Time, "HH:mm", Utility.DateLanguage_TH)} น. สำเร็จ" +
                            $"\n" +
                            $"โดยประสงค์ให้ คุณ{guarantoName}(ผู้ค้ำ) เป็นผู้ค้ำ" +
                             $"\n" +
                            $"\n มีรายละเอียดการยื่นกู้ ดังนี้" +
                            $"\n ประเภทการกู้ : {loanName?.LoanParentName} " +
                            $"\n จำนวนเงินที่ต้องการกู้ : {String.Format("{0:n2}", loan.LoanAmount)} บาท " +
                            $"\n อัตราดอกเบี้ย : {InterestMess} %" +
                            $"\n จำนวนงวดที่ต้องการผ่อน : {loan.LoanNumInstallments} งวด" +
                            $"\n" +
                            $"\n สามารถดูรายละเอียดคำขอกู้ได้ที่เมนู 'การค้ำประกัน'" +
                            $"\n" +
                            $"\n อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ " +
                            $"\n หากมีข้อสงสัยหรือต้องการสอบถามรายละเอียดเพิ่มเติม กรุณาติดต่อการเจ้าหน้าที่วิทยาเขตที่สังกัด " +
                            $"\n" +
                            $"\n ขอแสดงความนับถือ";

            return email;
        }

        private MailModel MessageAdmin(string name, List<string> listEmail, string debtorName, string guarantoName, ApplyLoanModel loan)
        {
            MailModel email = new()
            {
                IsAdmin = true,
                Title = $" (PSU LOAN) คำขอกู้รอพิจารณาตรวจสอบ (คุณ{debtorName})",
                Name = name,
                ListEmail = listEmail,
                Time = DateTime.Now,
            };

            var loanName = userService.GetLoanType(loan.LoanTypeID);
            var InterestMess = loan.LoanInterest == 0 ? "ไม่มี" : $"{loan.LoanInterest} %";

            email.Message = $"เรียน {name}" +
                $"\n" +
                $"\n คุณ{debtorName}(ผู้กู้) ได้ยืนยันการยื่นคำขอกู้เมื่อวันที่ " +
                $"{dateService.ChangeDate(email.Time, "dd MMMM yyyy", Utility.DateLanguage_TH)} " +
                $"เวลา {dateService.ChangeDate(email.Time, "HH:mm", Utility.DateLanguage_TH)} น." +
                $"โดยประสงค์ให้ คุณ{guarantoName}(ผู้ค้ำ) เป็นผู้ค้ำ" +
                $"\n" +
                $"\n มีรายละเอียดการยื่นกู้ ดังนี้" +
                $"\n ประเภทการกู้ : {loanName?.LoanParentName} " +
                $"\n จำนวนเงินที่ต้องการกู้ : {String.Format("{0:n2}", loan.LoanAmount)} บาท " +
                $"\n อัตราดอกเบี้ย : {InterestMess} %" +
                $"\n จำนวนงวดที่ต้องการผ่อน : {loan.LoanNumInstallments} งวด" +
                $"\n" +
                $"\n กรุณาตรวจสอบรายละเอียด และพิจารณาตรวจสอบคำขอกู้ ได้ที่เมนู 'คำขอกู้'" +
                $"\n" +
                $"\n ขอแสดงความนับถือ";
            return email;
        }

        private string GetUrl(string? url)
        {
            var path = string.Empty;
            if (url != null)
            {
                //string[] SubUrl = url.Split(AppSettings.Value.RootFilePath)

                string? rootFilePath = (Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.RootFilePath : fileUploadSetting.Value.Linux.RootFilePath);

                string[] SubUrl = url.Split(rootFilePath);

                if (SubUrl.Length >= 2)
                {
                    path = $"{SubUrl[1]}";
                }

                if (utilityServer.CheckDBtest())
                {
                    path = GetUrlV2(url);
                }
            }

            return path;
        }

        private string GetUrlV2(string? url)
        {
            var path = string.Empty;
            if (url != null)
            {
                var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

                string[] SubUrl = url.Split(physicalFilePath);

                if (SubUrl.Length >= 2)
                {
                    path = $"{SubUrl[1]}";
                }

                var t = SaveFileAndImgService.GetUrl(path, true);

                path = navigationManager.BaseUri + t;
            }

            return path;
        }

        private async Task SetMessageAsync()
        {
            List<object> Message = new();
            MessageModel mes = new();
            mes.Action = "ยื่นคำขอกู้สำเร็จ";
            mes.Title = $"โปรดรอผลการตรวจสอบจากเจ้าหน้าที่ และติดตามการดำเนินการได้ที่เมนู สัญญากู้ยืมเงิน";
            mes.ToPage = $"/HomeUser";
            mes.Message = Message;
            mes.ButtonHidden = true;
            await sessionStorage.SetItemAsync("Message", mes);
            navigationManager.NavigateTo("/Message", true);
        }

        private string GetAttachmentTypeName(decimal TypeId)
        {
            string Name = "ไม่พบชื่อไฟล์";

            var AType = _context.AttachmentTypes
                .Where(c => c.AttachmentTypeId == TypeId)
                .FirstOrDefault();

            if (AType != null)
            {
                Name = (!string.IsNullOrEmpty(AType.AttachmentNameThai) ?
                    AType.AttachmentNameThai : string.Empty);
            }
            return Name;
        }
    }
}