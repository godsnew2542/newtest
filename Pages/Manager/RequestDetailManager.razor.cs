using Microsoft.AspNetCore.Components;
using LoanApp.Model.Models;
using Microsoft.Extensions.Options;
using LoanApp.Shared;
using Microsoft.JSInterop;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Settings;
using LoanApp.Model.Helper;
using LoanApp.Services.IServices.LoanDb;

namespace LoanApp.Pages.Manager
{
    public partial class RequestDetailManager
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        [Parameter] public decimal RequestID { get; set; } = 0;

        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] private IPsuLoan psuLoan { get; set; } = null!;

        private MonthModel model_month { get; set; } = new();
        private LoanRequest loanRequest { get; set; } = new();
        private VLoanRequestContract? ReqCon { get; set; } = new();
        private LoanType? Loan { get; set; } = new();
        private VLoanStaffDetail? Debtor { get; set; } = new();
        private ContractAttachment attachment { get; set; } = new();

        private string ManagerRemark { get; set; } = string.Empty;
        private bool IsMobile { get; set; } = false;
        private string? HrefPath { get; set; } = null;


        protected override void OnInitialized()
        {
            if (RequestID != 0)
            {
                ReqCon = userService.GetVLoanRequestContract(RequestID);
                Loan = userService.GetLoanType(ReqCon?.LoanTypeId);
                Debtor = userService.GetUserDetail(ReqCon?.DebtorStaffId);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    IsMobile = await JS.InvokeAsync<bool>("isDevice");
                    if (IsMobile)
                    {
                        HrefPath = GeneratePDFService.DownloadPdf(RequestID);
                    }
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }

            }
        }

        public string ChangeDateTime(DateTime? dateTime, string[] selectMonth)
        {
            var ShowDate = string.Empty;
            var formatDate = "dd MMMM yyyy HH.mm น.";

            if (dateTime != null)
            {
                var ConvertDate = dateService.ConvertToDateTime(dateTime);
                ShowDate = dateService.ChangeDate(ConvertDate, formatDate, Utility.DateLanguage_TH);
            }
            return ShowDate;
        }

        public async Task SentMessageAsync(string Action, string Remark, bool Icon)
        {
            List<object> Message = new();
            MessageModel mes = new();

            mes.Action = Action;
            mes.Remark = Remark;
            mes.Icon = Icon;
            mes.ToPage = $"/Manager/ManageLoanRequest";
            mes.Message = Message;
            await sessionStorage.SetItemAsync("Message", mes);
            navigationManager.NavigateTo("/Message");
        }

        public async Task ConFirmPageAsync(bool pass)
        {
            string Action = string.Empty;
            string Remark = string.Empty;
            bool Icon = true;
            decimal? StatusId = 9;
            if (RequestID != 0)
            {
                if (pass) // true
                {
                    //กรณีอนุมัติการกู้
                    Action = "ลงนามอนุมัติสำเร็จ";
                    Remark = "คุณได้ลงนามเห็นควรอนุมัติสัญญาเงินกู้เรียบร้อยแล้ว";
                }
                else
                {
                    //กรณีไม่อนุมัติการกู้
                    Action = "ลงนามอนุมัติไม่สำเร็จ";
                    Remark = (!string.IsNullOrEmpty(ManagerRemark) ? "เนื่องจาก " + ManagerRemark : "");
                    Icon = false;
                    StatusId = 3;
                }

                await SaveToDbAsync(StatusId);
                await SetDataBySentEmailAsync(pass);
                await SentMessageAsync(Action, Remark, Icon);
            }
        }

        private string? GetTelephone(string staffId)
        {
            var Staff = userService.GetUserAddresses(staffId);
            return Staff?.AddrTelephone;
        }

        protected string CheckLoanType(string staffId, byte? TypeId)
        {
            var findLoanParent = userService.GetLoanType(TypeId);
            if (findLoanParent == null)
            {
                return string.Empty;
            }

            List<LoanType> loanType = _context.LoanTypes
                .Where(c => c.LoanParentId == findLoanParent.LoanParentId)
                .ToList();

            /* สถานะที่ไม่เคยกู้ */
            decimal[] AllStatusId = new[] { 0m, 1m, 3m, 99m, 100m };
            byte[] AllLoanTypeId;

            if (loanType.Count != 0)
            {
                var Status = "ผู้กู้ไม่เคยกู้ประเภทนี้";
                List<byte> list = new List<byte>();

                for (int i = 0; i < loanType.Count; i++)
                {
                    var loanTypeId = loanType[i].LoanTypeId;
                    list.Add(loanTypeId);
                }

                AllLoanTypeId = list.ToArray();

                var countLoan = _context.VLoanRequestContracts
                    .Where(c => !AllStatusId.Contains(c.CurrentStatusId.Value) &&
                    AllLoanTypeId.Contains(c.LoanTypeId.Value) &&
                    c.DebtorStaffId == staffId)
                    .Count();

                if (countLoan > 0)
                {
                    Status = "ผู้กู้เคยกู้ประเภทนี้มาแล้ว";
                }
                return Status;
            }
            else
            {
                return "ไม่พบประเภทดังกล่าว";
            }
        }

        public string ChangeDate(string DateString)
        {
            var ShowDate = string.Empty;
            DateModel Date = Utility.ChangeDateMonth(DateString, model_month.Th);

            if (!string.IsNullOrEmpty(Date.Day))
            {
                ShowDate = $"{Date.Day} {Date.Month} {Date.Year}";
            }
            return ShowDate;
        }

        public string ChangeDateTime(DateTime? dateTime)
        {
            var ShowDate = string.Empty;
            var formatDate = "dd MMMM yyyy HH.mm";

            if (dateTime != null)
            {
                var ConvertDate = dateService.ConvertToDateTime(dateTime);
                ShowDate = dateService.ChangeDate(ConvertDate, formatDate, Utility.DateLanguage_TH);
            }
            return ShowDate;
        }

        private ContractAttachment? GetPathFile(decimal? Id)
        {
            ContractAttachment? Attachment = _context.ContractAttachments.Where(c => c.AttachmentId == Id).FirstOrDefault();
            return Attachment;
        }

        private string ConcatFile(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            var FullPath = $"{AppSettings.Value.RequestFilePath}\\{path}";
            return FullPath;
        }

        protected void AddRemark(ChangeEventArgs e)
        {
            ManagerRemark = e.Value.ToString();
        }

        private async Task SetDataBySentEmailAsync(bool pass)
        {
            string messOther = "ไม่ผ่านการพิจารณาโดยผู้บริหาร";

            if (pass)
            {
                messOther = "ได้ผ่านการพิจารณาโดยผู้บริหาร";
            }

            try
            {
                var StaffDetail = userService.GetUserDetail(ReqCon?.DebtorStaffId);
                var DebtorName = userService.GetFullName(ReqCon?.DebtorStaffId);

                var GuarantorDetail = userService.GetUserDetail(ReqCon.ContractGuarantorStaffId);
                var GuarantoName = userService.GetFullName(ReqCon.ContractGuarantorStaffId);

                ApplyLoanModel loan = new();
                loan.LoanTypeID = ReqCon.LoanTypeId;
                loan.LoanAmount = ReqCon.ContractLoanAmount != null ?
                    ReqCon.ContractLoanAmount.Value :
                    0;
                loan.LoanNumInstallments = ReqCon.ContractLoanNumInstallments != null ?
                    (int)ReqCon.ContractLoanNumInstallments.Value :
                    0;

                #region ผู้กู้
                if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
                {
                    var Name = userService.GetFullName(StaffDetail.StaffId);
                    string? Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                    //string user = userService.FindUserName(StateProvider?.CurrentUser.UserName);
                    //if (Utility.UserDev.Contains(user))
                    //{
                    //    Name = $"(mailTest-{user}) {Name}";
                    //    Email = Utility.SendMailTest;
                    //}

                    var email = MailService.MailDebtorByManagerStatus9(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        messOther);
                    MailService.SendEmail(email);
                }
                #endregion

                #region ผู้ค้ำ
                if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
                {
                    var Name = userService.GetFullName(GuarantorDetail.StaffId);
                    string? Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                    //string user = userService.FindUserName(StateProvider?.CurrentUser.UserName);
                    //if (Utility.UserDev.Contains(user))
                    //{
                    //    Name = $"(mailTest-{user}) {Name}";
                    //    Email = Utility.SendMailTest;
                    //}

                    var email = MailService.MailDebtorByManagerStatus9(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        messOther);
                    MailService.SendEmail(email);
                }
                #endregion

                #region Admin
                List<VLoanStaffPrivilege> vLoanStaffPrivilege = await psuLoan.GetVLoanStaffPrivilegeByCampId(StaffDetail?.CampId);
                List<string> listEmailAdmin = psuLoan.GetAllEmailAdmin(vLoanStaffPrivilege);

                if (listEmailAdmin.Any())
                {
                    var emailAdmin = MailService.MailAdminByManagerStatus9("แอดมิน วิทยาเขต",
                        //Utility.MailAdmin,
                        string.Empty,
                        DebtorName,
                        GuarantoName,
                        loan,
                        messOther,
                        listEmailAdmin);
                    MailService.SendEmail(emailAdmin);
                }
                #endregion
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task SaveToDbAsync(decimal? StatusId)
        {
            try
            {
                if (ReqCon == null)
                {
                    return;
                }

                string ModifyBy = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                ContractMain? contract = _context.ContractMains
                    .Where(c => c.ContractId == ReqCon.ContractId)
                    .FirstOrDefault();

                if (contract != null)
                {
                    contract.ApproveStaffId = ModifyBy;
                    contract.ApproveDate = DateTime.Now;
                    contract.ContractRemark = ManagerRemark;
                    contract.ContractStatusId = StatusId;

                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                }

                if (StatusId == 3)
                {
                    LoanRequest? req = _context.LoanRequests
                   .Where(c => c.LoanRequestId == ReqCon.LoanRequestId)
                   .FirstOrDefault();

                    if (req != null)
                    {
                        req.LoanStatusId = StatusId;

                        _context.Update(req);
                        await _context.SaveChangesAsync();
                    }
                }

                decimal LoanStatusId = 5;
                await LogService.GetHisContractMainByContractIDAsync(contract?.ContractId,
                    LoanStatusId,
                    ModifyBy);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        public void Back()
        {
            navigationManager.NavigateTo("./Manager/ManageLoanRequest");
        }
    }
}
