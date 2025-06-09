using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using LoanApp.Model.Models;

using LoanApp.Components.Document;
using LoanApp.Shared;
using Microsoft.EntityFrameworkCore;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Settings;
using LoanApp.Model.Helper;
using Org.BouncyCastle.Crypto.Agreement;

namespace LoanApp.Pages.Admin;

public partial class RequestDetail
{
    #region CascadingParameter
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

    #endregion

    [Parameter] public decimal RequestID { get; set; } = 0;

    #region Inject
    [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

    #endregion

    private MonthModel model_month { get; set; } = new();
    private VLoanRequestContract? V_Agreement { get; set; } = new();
    private ContractMain Agreement { get; set; } = new();
    private LoanType? LoanData { get; set; } = new();
    private List<SelectModel> Select { get; set; } = new();
    private List<ContractAttachment> ConAttachment { get; set; } = new();
    private ApplyLoanModel ModelApplyLoan { get; set; } = new();
    private DocumentOptionModel OptionLoanAgreement { get; set; } = new();
    private VLoanStaffDetail? DebtorStaffDetail { get; set; } = new();
    private VStaffAddress DebtorStaffAddress { get; set; } = new();
    private VLoanStaffDetail? GuarantorStaffDetail { get; set; } = new();
    private VStaffFamily DebtorStaffFamilies { get; set; } = new();
    private VStaffFamily GuarantorStaffFamilies { get; set; } = new();
    private SelectModel SelectRemark { get; set; } = new();
    private RequestAttrachment RefRequestAttrachment { get; set; } = new();
    private List<VStaffChild> Debtor_Child { get; set; } = new();
    private List<VStaffChild> Guarantor_Child { get; set; } = new();

    private string? LoanAttrachmentHTML { get; set; } = null;
    private string Message { get; set; } = string.Empty;

    /// <summary>
    /// 6m, 7m, 8m, 9m, 80m, 81m, 82m, 200m 
    /// </summary>
    private decimal[] AllStatusId { get; set; } = new[] { 6m, 7m, 8m, 9m, 80m, 81m, 82m, 200m };
    private bool IsData { get; set; } = true;
    private string StorageName { get; } = "BackToManageLoanRequest";
    private bool IsMobile { get; set; } = false;
    private bool changType { get; set; } = false;

    /// <summary>
    /// เช็คว่า ชื่อผู้ค้ำตรงกับ ชื่อคู่สมรถของผู้กู้หรือไหม
    /// </summary>
    private bool isFamilyPartnerFullnameByGuarantor { get; set; } = false;

    #region นับจำนวนสัญญาของ ผู้กู้
    private List<VLoanRequestContract> Debtor_CheckLoanAgreement { get; set; } = new();
    private int LoanSuccess { get; set; } = 0;
    private int WaitingDocumentLoan { get; set; } = 0;
    private List<VLoanRequestContract> Debtor_CheckLoanGuarantor { get; set; } = new();

    #endregion

    #region นับจำนวนสัญญาของ ผู้ค้ำ
    private List<VLoanRequestContract> Guarantor_CheckLoanGuarantor { get; set; } = new();

    #endregion

    protected async override Task OnInitializedAsync()
    {
        Agreement.ContractNo = string.Empty;

        try
        {
            if (RequestID != 0)
            {
                V_Agreement = userService.GetVLoanRequestContract(RequestID);
                Select = setDataSelect();

                if (V_Agreement == null)
                {
                    IsData = false;
                    return;
                }

                if (V_Agreement.LoanCreatedDate != null)
                {
                    OptionLoanAgreement.DateTitle = V_Agreement.LoanCreatedDate.Value;
                }

                LoanData = await _context.LoanTypes
                    .Where(c => c.LoanTypeId == V_Agreement.LoanTypeId)
                    .FirstOrDefaultAsync();
                DebtorStaffDetail = userService.GetUserDetail(V_Agreement.DebtorStaffId);
                var DebtorAddress = userService.GetUserAddresses(V_Agreement.DebtorStaffId);
                var Debtorfamily = await psuLoan.GetUserFamilyAsync(V_Agreement.DebtorStaffId);

                if (DebtorAddress != null)
                {
                    DebtorStaffAddress = DebtorAddress;
                }
                if (Debtorfamily != null)
                {
                    DebtorStaffFamilies = Debtorfamily;
                }

                GuarantorStaffDetail = await psuLoan.GetUserDetailAsync(V_Agreement.LoanRequestGuaranStaffId);
                var Guarantorfamily = await psuLoan.GetUserFamilyAsync(V_Agreement.LoanRequestGuaranStaffId);
                if (Guarantorfamily != null)
                {
                    GuarantorStaffFamilies = Guarantorfamily;
                }
                await GetFileAsync(RequestID);
            }

            ApplyLoanModel? checkData = await sessionStorage.GetItemAsync<ApplyLoanModel?>(StorageName);

            if (checkData != null)
            {
                ModelApplyLoan = checkData;
            }

            Debtor_Child = await GetChild(DebtorStaffDetail?.StaffId);
            Guarantor_Child = await GetChild(GuarantorStaffDetail?.StaffId);

            if (!string.IsNullOrEmpty(DebtorStaffFamilies.FamilyPartnerFname) && !string.IsNullOrEmpty(GuarantorStaffFamilies.FamilyPartnerFname) && GuarantorStaffDetail != null)
            {
                var partnerFullNameDebtor = $"{DebtorStaffFamilies.FamilyPartnerFname} {DebtorStaffFamilies.FamilyPartnerOldsname}";

                var partnerFullNameGuarantor = $"{GuarantorStaffDetail.StaffNameThai} {GuarantorStaffDetail.StaffSnameThai}";

                if (partnerFullNameDebtor == partnerFullNameGuarantor)
                {
                    isFamilyPartnerFullnameByGuarantor = true;
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
            try
            {
                IsMobile = await JS.InvokeAsync<bool>("isDevice");

                if (DebtorStaffDetail?.StaffId != null)
                {
                    Debtor_CheckLoanAgreement = await psuLoan.GetListAgreementDataFormVLoanRequestContract(DebtorStaffDetail.StaffId, AllStatusId.ToList(), Utility.ShowDataYear);

                    List<VLoanRequestContract> success = await psuLoan.GetListAgreementDataFormVLoanRequestContract(DebtorStaffDetail.StaffId, new List<decimal>() { 99 }, Utility.ShowDataYear);

                    LoanSuccess = success.Count;

                    //LoanSuccess = _context.VLoanRequestContracts
                    //    .Where(c => c.CurrentStatusId!.Value == 99m)
                    //    .Where(c => c.DebtorStaffId == DebtorStaffDetail!.StaffId)
                    //    .Where(c => (c.ContractDate == null) ||
                    //    ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                    //    .Count()

                    WaitingDocumentLoan = Debtor_CheckLoanAgreement.Count(c => c.CurrentStatusId!.Value == 6);
                }

                Debtor_CheckLoanGuarantor = await CheckLoanGuarantor(DebtorStaffDetail?.StaffId);
                Guarantor_CheckLoanGuarantor = await CheckLoanGuarantor(GuarantorStaffDetail?.StaffId);

                StateHasChanged();
            }
            catch (Exception ex)
            {
                StateHasChanged();
                await Error.ProcessError(ex);
            }
        }
    }

    private List<SelectModel> setDataSelect()
    {
        List<SelectModel> _selectModel = new();

        _selectModel.AddRange(new List<SelectModel> {
            new() { Name = "คุณสมบัติของผู้กู้ไม่ผ่าน", ID = 1 },
            new() { Name = "คุณสมบัติของผู้ค้ำไม่ผ่าน", ID = 2 },
            new() { Name = $"เงินคงเหลือสุทธิน้อยกว่า {Utility.percentAmountTotal} % ของเงินเดือน", ID = 3 },
            new() { Name = "เจ้าตัวประสงค์ยกเลิกคำขอกู้", ID = 4 },
            new() { Name = "อื่นๆ โปรดระบุ", ID = 99 }
        });
        return _selectModel;
    }

    private void ChangeRemarkName(string? LoanRemark)
    {
        V_Agreement ??= new();
        V_Agreement.LoanRemark = LoanRemark;
    }

    private async Task<List<VStaffChild>> GetChild(string? StaffID)
    {
        if (string.IsNullOrEmpty(StaffID))
        {
            return new List<VStaffChild>();
        }

        return await psuLoan.GetListVStaffChildAsync(StaffID);
    }

    private string GetStaffSalaryId(string? staffId)
    {
        var staffSalaryId = "ไม่มี";
        VSStaff? _VSStaff = userService.GetVSStaff(staffId);

        if (_VSStaff != null && !string.IsNullOrEmpty(_VSStaff.StaffSalaryId))
        {
            staffSalaryId = _VSStaff.StaffSalaryId;
        }
        return staffSalaryId;
    }

    private string ChangeDateTime(DateTime? dateTime)
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

    private string StartAndEndDate(string? Date, string[] selectMonth)
    {
        var ShowDate = string.Empty;
        if (!string.IsNullOrEmpty(Date))
        {
            var StringDate = $"{Date:dd MM yyyy}";
            ShowDate = dateService.ChangeDateByString(StringDate, selectMonth);
        }
        return ShowDate;
    }

    private void ToggleButton(bool close)
    {
        if (close)
        {
            changType = false;
        }
        else
        {
            changType = !changType;
        }
    }

    private void SelectType(SelectModel selectmodel)
    {
        SelectRemark = new();
        SelectRemark = selectmodel;
        V_Agreement!.LoanRemark = SelectRemark.Name;

        ToggleButton(true);
    }

    private async Task OpenPdfAsync()
    {
        LoanAttrachmentHTML = string.Empty;
        LoanAttrachmentHTML = await GenerateHTMLAsync();
    }

    private async Task DownloadPdfAsync()
    {
        Message = string.Empty;
        var fileName = $"เอกสารสัญญา.pdf";
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

    protected async Task<string> GenerateHTMLAsync()
    {
        if (V_Agreement == null)
        {
            return string.Empty;
        }
        ModelApplyLoan.LoanAmount = V_Agreement.LoanRequestLoanAmount != null ? V_Agreement.LoanRequestLoanAmount.Value : 0;

        ModelApplyLoan.LoanNumInstallments = V_Agreement.LoanRequestNumInstallments != null ? (int)V_Agreement.LoanRequestNumInstallments : 0;

        ModelApplyLoan.LoanMonthlyInstallment = V_Agreement.LoanRequestLoanInstallment;
        ModelApplyLoan.SalaryNetAmount = V_Agreement.SalaryNetAmount;

        var HeadHTML = await JS.InvokeAsync<string>("headHTML");
        var ScriptHTML = await JS.InvokeAsync<string>("scriptHTML");
        var HtmlText = string.Empty;

        if (RefRequestAttrachment != null)
        {
            HtmlText = await RefRequestAttrachment.GetBoByHtmlAsync();
        }

        var Html = $"{HeadHTML} <br/> {HtmlText} <br/> {ScriptHTML}";
        return Html;
    }

    private async Task<List<VLoanRequestContract>> CheckLoanGuarantor(string? staffId)
    {
        if (string.IsNullOrEmpty(staffId))
        {
            return new List<VLoanRequestContract>();
        }

        try
        {
            return await _context.VLoanRequestContracts
            .Where(c => AllStatusId.Contains(c.CurrentStatusId!.Value))
            .Where(c => c.ContractGuarantorStaffId == staffId)
            .Where(c => (c.ContractDate == null) ||
            ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
            .AsNoTracking()
            .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    protected string CheckLoanType(string? staffId, byte? TypeId)
    {
        if (string.IsNullOrEmpty(staffId))
        {
            return string.Empty;
        }

        LoanType? findLoanParent = userService.GetLoanType(TypeId);

        if (findLoanParent == null)
        {
            return string.Empty;
        }

        List<LoanType> loanType = _context.LoanTypes
            .Where(c => c.LoanParentId == findLoanParent.LoanParentId)
            .AsNoTracking()
            .ToList();

        /* สถานะที่ไม่เคยกู้ */
        //decimal[] decimalStatusId = new[] { 0m, 1m, 3m, 98m, 99m, 100m }
        List<decimal> decimalStatusId = new() { 0, 1, 3, 98, 99, 100 };

        //byte[] allLoanTypeId
        List<byte> allLoanTypeId;

        if (loanType.Any())
        {
            //var status = "ไม่เคยกู้ประเภทนี้"
            var status = "ไม่มี";
            //List<byte> list = new();

            //for (int i = 0; i < loanType.Count; i++)
            //{
            //    list.Add(loanType[i].LoanTypeId);
            //}

            //allLoanTypeId = list.ToArray();
            allLoanTypeId = loanType
                .Select(x => x.LoanTypeId)
                .ToList();

            var countLoan = _context.VLoanRequestContracts
                .Where(c => !decimalStatusId.Contains(c.CurrentStatusId!.Value))
                .Where(c => allLoanTypeId.Contains(c.LoanTypeId!.Value))
                .Where(c => c.DebtorStaffId == staffId)
                .Count();

            if (countLoan > 0)
            {
                //status = "เคยกู้ประเภทนี้"
                status = "มี";
            }
            return status;
        }
        else
        {
            return "ไม่พบประเภทดังกล่าว";
        }
    }

    private async Task NextPageAsync()
    {
        int StatusId = 2;
        var mess = "ผลการพิจารณาตรวจสอบคำขอกู้ได้ผ่านการตรวจสอบ และอยู่ระหว่างรออนุมัติเรียบร้อยแล้ว";
        try
        {
            await AddToDdAsync(StatusId);
            SetDataBySentEmail(mess, StatusId);
            navigationManager.NavigateTo("./Admin/ManageLoanRequest");
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private void SetDataBySentEmail(string messOther, int StatusId)
    {
        try
        {
            var StaffDetail = userService.GetUserDetail(V_Agreement?.DebtorStaffId);
            var DebtorName = userService.GetFullNameNoTitleName(V_Agreement?.DebtorStaffId);

            var GuarantorDetail = userService.GetUserDetail(V_Agreement?.LoanRequestGuaranStaffId);
            var GuarantoName = userService.GetFullNameNoTitleName(V_Agreement?.LoanRequestGuaranStaffId);

            ApplyLoanModel loan = new()
            {
                LoanTypeID = V_Agreement?.LoanTypeId,
                LoanAmount = V_Agreement?.LoanRequestLoanAmount != null ? V_Agreement.LoanRequestLoanAmount.Value : 0,
                LoanInterest = V_Agreement?.LoanRequestLoanInterest,
                LoanNumInstallments = V_Agreement?.LoanRequestNumInstallments != null ? (int)V_Agreement.LoanRequestNumInstallments : 0,
            };

            #region ผู้กู้
            if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
            {
                var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                var Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                var email = MessageDebtor(Name,
                    Email,
                    DebtorName,
                    GuarantoName,
                    loan,
                    messOther,
                    StatusId);
                MailService.SendEmail(email);
            }
            #endregion

            #region ผู้ค้ำ
            if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
            {
                var Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);
                var Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                var email = MessageGuarantor(Name,
                    Email,
                    DebtorName,
                    GuarantoName,
                    loan,
                    messOther,
                    StatusId);
                MailService.SendEmail(email);
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
        ApplyLoanModel loan,
        string messOther,
        int StatusId)
    {
        MailModel email = new();
        var loanData = userService.GetLoanType(loan.LoanTypeID);
        var ConvertDate = dateService.ConvertToDateTime(V_Agreement?.LoanCreatedDate);

        email.Title = "(PSU LOAN) แจ้งผลการพิจารณาอนุมัติคำขอกู้";
        email.Name = name;
        email.Email = emailUser;
        email.Time = DateTime.Now;
        if (StatusId == 2)
        {
            email.Message = $"เรียน คุณ{name} \n" +
                            $"\n" +
                            $"ตามที่คุณได้ยื่นคำขอกู้ เมื่อวันที่ {dateService.ChangeDate(ConvertDate, "dd MMMM yyyy", Utility.DateLanguage_TH)}" +
                            $"เวลา {dateService.ChangeDate(ConvertDate, "HH.mm น.", Utility.DateLanguage_TH)} โดยประสงค์ให้ คุณ{guarantoName} (ผู้ค้ำ) เป็นผู้ค้ำนั้น \n" +
                            $"\n" +
                            $"{messOther} เมื่อวันที่ {dateService.ChangeDate(email.Time, "HH.mm น.", Utility.DateLanguage_TH)}" +
                            $"\n" +
                            $"โดยมีรายละเอียดการกู้ ดังนี้ \n" +
                            $"ประเภทการกู้ : {loanData?.LoanParentName} \n" +
                            $"จำนวนเงินที่ต้องการกู้ : {string.Format("{0:n2}", loan.LoanAmount)} บาท \n" +
                            $"อัตราดอกเบี้ย : {loan.LoanInterest} % \n" +
                            $"จำนวนงวดที่ต้องการผ่อน : {loan.LoanNumInstallments} งวด \n" +
                            $"\n" +
                            $"กรุณานัดหมายเพื่อทำสัญญาเงินกู้ได้ที่เมนู 'ภาพรวม' หรือ 'สัญญากู้ยืมเงิน' \n" +
                            $"\n" +
                            $"หากมีข้อสงสัยหรือต้องการสอบถามรายละเอียดเพิ่มเติม กรุณาติดต่อการเจ้าหน้าที่วิทยาเขตที่สังกัด \n" +
                            $"อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ \n" +
                            $"\n" +
                            $"ขอแสดงความนับถือ";
        }
        else
        {
            email.Message = $"เรียน คุณ{name} \n" +
                            $"\n" +
                            $"ตามที่คุณได้ยื่นคำขอกู้ เมื่อวันที่ {dateService.ChangeDate(ConvertDate, "dd MMMM yyyy", Utility.DateLanguage_TH)}" +
                            $"เวลา {dateService.ChangeDate(ConvertDate, "HH.mm น.", Utility.DateLanguage_TH)} โดยประสงค์ให้ คุณ{guarantoName} (ผู้ค้ำ) เป็นผู้ค้ำนั้น \n" +
                            $"\n" +
                            $"{messOther} หมายเหตุ : {(V_Agreement?.LoanRemark != null ? V_Agreement.LoanRemark : "-")} เมื่อวันที่ {dateService.ChangeDate(email.Time, "HH.mm น.", Utility.DateLanguage_TH)} \n" +
                            $"\n" +
                            $"คุณสามารถยื่นคำขอกู้ได้ใหม่ที่เมนู 'ยื่นคำขอกู้' \n" +
                            $"\n" +
                            $"หากมีข้อสงสัยหรือต้องการสอบถามรายละเอียดเพิ่มเติม กรุณาติดต่อการเจ้าหน้าที่วิทยาเขตที่สังกัด \n" +
                            $"อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ \n" +
                            $"\n" +
                            $"ขอแสดงความนับถือ";
        }

        return email;
    }

    private MailModel MessageGuarantor(string name,
        string emailUser,
        string debtorName,
        string guarantoName,
        ApplyLoanModel loan,
        string messOther,
        int StatusId)
    {
        MailModel email = new();
        var loanData = userService.GetLoanType(loan.LoanTypeID);
        var ConvertDate = dateService.ConvertToDateTime(V_Agreement?.LoanCreatedDate);

        email.Title = "(PSU LOAN) แจ้งผลการพิจารณาอนุมัติคำขอกู้ที่คุณถูกประสงค์ให้เป็นผู้ค้ำ";
        email.Name = name;
        email.Email = emailUser;
        email.Time = DateTime.Now;
        if (StatusId == 2)
        {
            email.Message = $"เรียน คุณ{name} \n" +
                            $"\n" +
                            $"ตามที่คุณ{debtorName}(ผู้กู้)ได้ยื่นคำขอกู้ เมื่อวันที่ {dateService.ChangeDate(ConvertDate, "dd MMMM yyyy", Utility.DateLanguage_TH)}" +
                            $"เวลา {dateService.ChangeDate(ConvertDate, "HH.mm น.", Utility.DateLanguage_TH)} โดยประสงค์ให้ คุณ{guarantoName} (ผู้ค้ำ) เป็นผู้ค้ำนั้น \n" +
                            $"\n" +
                            $"{messOther} เมื่อวันที่ {dateService.ChangeDate(email.Time, "HH.mm น.", Utility.DateLanguage_TH)} \n" +
                            $"\n" +
                            $"โดยมีรายละเอียดการกู้ ดังนี้ \n" +
                            $"ประเภทการกู้ : {loanData?.LoanParentName} \n" +
                            $"จำนวนเงินที่ต้องการกู้ : {string.Format("{0:n2}", loan.LoanAmount)} บาท \n" +
                            $"อัตราดอกเบี้ย : {loan.LoanInterest} % \n" +
                            $"จำนวนงวดที่ต้องการผ่อน : {loan.LoanNumInstallments} งวด \n" +
                            $"\n" +
                            $"\n สามารถดูรายละเอียดคำขอกู้ได้ที่เมนู 'การค้ำประกัน'" +
                            $"\n" +
                            $"หากมีข้อสงสัยหรือต้องการสอบถามรายละเอียดเพิ่มเติม กรุณาติดต่อการเจ้าหน้าที่วิทยาเขตที่สังกัด \n" +
                            $"อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ \n" +
                            $"\n" +
                            $"ขอแสดงความนับถือ";
        }
        else
        {
            email.Message = $"เรียน คุณ{name} \n" +
                            $"\n" +
                            $"ตามที่คุณได้ยื่นคำขอกู้ เมื่อวันที่ {dateService.ChangeDate(ConvertDate, "dd MMMM yyyy", Utility.DateLanguage_TH)}" +
                            $"เวลา {dateService.ChangeDate(ConvertDate, "HH.mm น.", Utility.DateLanguage_TH)} โดยประสงค์ให้ คุณ{guarantoName} (ผู้ค้ำ) เป็นผู้ค้ำนั้น \n" +
                            $"\n" +
                            $"{messOther} เมื่อวันที่ {dateService.ChangeDate(email.Time, "HH.mm น.", Utility.DateLanguage_TH)} \n" +
                            $"\n" +
                            $"โดยมีรายละเอียดการกู้ ดังนี้ \n" +
                            $"ประเภทการกู้ : {loanData?.LoanParentName} \n" +
                            $"จำนวนเงินที่ต้องการกู้ : {string.Format("{0:n2}", loan.LoanAmount)} บาท \n" +
                            $"อัตราดอกเบี้ย : {loan.LoanInterest} % \n" +
                            $"จำนวนงวดที่ต้องการผ่อน : {loan.LoanNumInstallments} งวด \n" +
                            $"\n" +
                            $"\n สามารถดูรายละเอียดคำขอกู้ได้ที่เมนู 'การค้ำประกัน'" +
                            $"\n" +
                            $"หากมีข้อสงสัยหรือต้องการสอบถามรายละเอียดเพิ่มเติม กรุณาติดต่อการเจ้าหน้าที่วิทยาเขตที่สังกัด \n" +
                            $"อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ \n" +
                            $"\n" +
                            $"ขอแสดงความนับถือ";
        }

        return email;
    }

    /// <summary>
    /// ไม่เพิ่มข้อมูลเพราะ เดี่ยวจะ update ตอนได้เงินเงินจริง ||
    ///  1.LOAN_TOTAL_AMOUNT จำนวนเงินรวมดอกเบี้ย ||
    ///  2.LOAN_INSTALLMENT จำนวนเงินจ่ายต่องวด ||
    ///  3.Agreement.NewContractNo ไว้ใส่ตอน บันทึกวันโอนเงิน
    /// </summary>
    /// <param name="StatusId"></param>
    /// <returns></returns>
    private async Task AddToDdAsync(int StatusId)
    {
        try
        {
            string AdminStaffId = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
            var _VSStaff = userService.GetVSStaff(V_Agreement?.DebtorStaffId);

            /* Add data in table ContractMain */
            Agreement.DebtorStaffId = V_Agreement!.DebtorStaffId;
            Agreement.GuarantorStaffId = V_Agreement.LoanRequestGuaranStaffId;
            Agreement.LoanTypeId = V_Agreement.LoanTypeId;
            Agreement.LoanAmount = V_Agreement.LoanRequestLoanAmount;
            Agreement.LoanInterest = V_Agreement.LoanRequestLoanInterest;
            Agreement.LoanNumInstallments = V_Agreement.LoanRequestNumInstallments;
            Agreement.ContractStatusId = StatusId;
            Agreement.LoanRequestId = V_Agreement.LoanRequestId;
            Agreement.AdminStaffIdRecord = AdminStaffId;
            Agreement.StaffSalaryId = _VSStaff?.StaffSalaryId;
            Agreement.DebtorCitizenId = _VSStaff?.CitizenId;

            //_context.ContractMains.Add(Agreement)
            //await _context.SaveChangesAsync()
            await psuLoan.AddContractMain(Agreement);

            await LRequestCheckApproveAsync(StatusId, AdminStaffId);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task LRequestCheckApproveAsync(int StatusId, string AdminStaffId)
    {
        /* Update data in table LoanRequest */
        if (V_Agreement == null)
        {
            return;
        }
        try
        {
            LoanRequest? req = await _context.LoanRequests
                .Where(c => c.LoanRequestId == V_Agreement.LoanRequestId)
                .FirstOrDefaultAsync();

            if (req != null)
            {
                req.LoanStatusId = StatusId;
                req.LoanRemark = V_Agreement.LoanRemark;
                req.ApproveDate = DateTime.Now;
                req.ApproveBy = AdminStaffId;

                _context.Update(req);
                await _context.SaveChangesAsync();

                string ModifyBy = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                await LogService.GetHisLoanRequestByRequestIDAsync(req.LoanRequestId, 1m, ModifyBy);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task GetFileAsync(decimal id)
    {
        ConAttachment = await _context.ContractAttachments
            .Where(c => c.LoanRequestId == id)
            .ToListAsync();
    }

    private async Task NotConfirmAsync()
    {
        try
        {
            int StatusId = 3;
            string AdminStaffId = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);

            var mess = "ผลการพิจารณาตรวจสอบคำขอกู้ไม่ผ่าน";
            await LRequestCheckApproveAsync(StatusId, AdminStaffId);
            SetDataBySentEmail(mess, StatusId);
            navigationManager.NavigateTo("./Admin/ManageLoanRequest");
        }
        catch (Exception ex)
        {
            await Error.ProcessError(ex);
        }
    }

    private void Back()
    {
        navigationManager.NavigateTo($"Admin/ManageLoanRequest/{true}/{ModelApplyLoan.LoanTypeID}/{ModelApplyLoan.ContractStatusId}");
    }

    private string GetUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return "";
        }
        var path = $"{AppSettings.Value.RequestFilePath}\\{url}";
        return path;
    }

    private async Task<string?> GetTypeName(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        string fileName = Path.GetFileName(path);
        var split = fileName.Split("_");

        if (split != null && split.Count() >= 1)
        {
            AttachmentType? attachment = await psuLoan.GetAttachmentTypeById(Convert.ToDecimal(split[0]));

            return attachment?.AttachmentNameThai;
        }
        return null;
    }

    /// <summary>
    /// เช็คปีทำงานคงเหลือ ต้องน้อยกว่า หรือเท่ากับ 4 ปี 0 เดือน จึงแจ้งเตือน
    /// null = ไม่พบข้อมูล || true = แจ้งเตือน <= 4 ปี || false = ปกติ
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <returns></returns>
    private bool? CheckStaffRemainWork(decimal? year, decimal? month)
    {
        bool? warning = false;

        if (year == null && month == null)
        {
            warning = null;
        }
        else if (year == 4 && month == 0)
        {
            warning = true;
        }
        else if (year <= 3)
        {
            warning = true;
        }

        return warning;
    }

    /// <summary>
    /// ตรวจสอบ list การค้ำประกัน ที่ยังมีผลอยู่
    /// </summary>
    /// <param name="_requestID"></param>
    /// <param name="staffId"></param>
    private void TopageGuarantorLoanGuarantor(decimal _requestID, string? staffId)
    {
        if (string.IsNullOrEmpty(staffId))
        {
            return;
        }
        navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Admin}/CheckGurantorAgreement/{staffId}/{_requestID}");
    }

    /// <summary>
    /// ตรวจสอบ list การค้ำประกัน ที่ยังมีผลอยู่
    /// </summary>
    /// <param name="_requestID"></param>
    /// <param name="staffId"></param>
    private void TopageDebtorCheckAgreement(decimal _requestID, string? staffId)
    {
        if (string.IsNullOrEmpty(staffId))
        {
            return;
        }
        navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Admin}/CheckAgreement/{staffId}/{_requestID}");
    }

    /// <summary>
    /// page [{newRole:int}/AgreementDetail/{StaffID}/{RequestID:decimal}/{rootPage:int}/{rootRequestID:decimal}]
    /// </summary>
    /// <param name="_requestID"></param>
    /// <param name="staffId"></param>
    private void TopageAgreementDetail(decimal _requestID, string? staffId)
    {
        if (string.IsNullOrEmpty(staffId))
        {
            return;
        }
        navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Admin}/AgreementDetail/{staffId}/{_requestID}/{(int)BackRootPageEnum.Admin_RequestDetail}/{RequestID}");
    }
}
