using LoanApp.Components.Document;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LoanApp.Pages.User;

public partial class AgreementDetailPage
{
    [CascadingParameter] private Error Error { get; set; } = null!;
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    #region Parameter
    [Parameter] public decimal RequestID { get; set; } = 0;
    [Parameter] public string StaffID { get; set; } = string.Empty;
    [Parameter] public PageControl PageTo { get; set; } = PageControl.User;
    [Parameter] public string Role { get; set; } = string.Empty;
    [Parameter] public int newRole { get; set; } = 0;
    /// <summary>
    /// LoanApp.Models.BackRootPageEnum
    /// </summary>
    [Parameter] public int rootPage { get; set; } = 0;
    [Parameter] public decimal rootRequestID { get; set; } = 0;

    #endregion

    [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
    [Inject] private ISaveFileAndImgService saveFileAndImgService { get; set; } = null!;

    private DateModel date { get; set; } = new();
    private MonthModel model_month { get; set; } = new();
    private VLoanStaffDetail? DebtorStaff { get; set; } = new();
    private VLoanStaffDetail? GuarantStaff { get; set; } = new();
    private VLoanRequestContract? Request { get; set; } = new();
    private VStaffAddress? StaffAddress { get; set; } = new();
    private VStaffFamily? StaffFamilies { get; set; } = null;
    private List<VStaffChild> StaffChild { get; set; } = new();
    private List<StatusUserModel> ListStatusUser { get; set; } = new();
    private LoanStaffDetail? StaffDetail { get; set; } = new();

    private LoanType LoanData { get; set; } = new();
    private ApplyLoanModel Info { get; set; } = new();
    private DocumentOptionModel OptionLoanAgreement { get; set; } = new();
    private List<string> pathContract { get; set; } = new();
    private List<ListDocModel> ResultDocList { get; set; } = new();

    #region ref Doc
    private RequestAttrachment? RefRequestAttrachment { get; set; }
    #endregion

    #region เอกสารสัญญา
    private LoanAttrachment RefLoanAttrachment { get; set; } = new();
    private LoanGuarantor RefGuarantor { get; set; } = new();
    private LoanPartner RefDebtorPartner { get; set; } = new();
    private LoanPartner RefGuarantorPartner { get; set; } = new();
    private VStaffFamily GuarantorStaffFamilies { get; set; } = new();
    private VStaffAddress GuarantorStaffAssress { get; set; } = new();
    private string? LoanAttrachmentHTML_2 { get; set; } = null;
    #endregion

    private bool loading { get; set; } = true;
    private bool IsMobile { get; set; } = false;
    private string LoanAttrachmentHTML { get; set; } = String.Empty;
    private bool LoadingResultImg { get; set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            loading = true;
            LoanAttrachmentHTML_2 = null;
            try
            {
                string? GuarantStaff_Id = StateProvider?.CurrentUser.StaffId;

                if (RequestID != 0 && !string.IsNullOrEmpty(GuarantStaff_Id))
                {
                    Request = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);

                    if (Request != null)
                    {
                        var GuarantorStaffId = (Request.ContractGuarantorStaffId != null ?
                            Request.ContractGuarantorStaffId : Request.LoanRequestGuaranStaffId);

                        GuarantStaff = await psuLoan.GetUserDetailAsync(GuarantorStaffId);
                        VStaffFamily? GuarantorFamilies = await psuLoan.GetUserFamilyAsync(GuarantorStaffId);
                        VStaffAddress? GuarantorAssress = await psuLoan.GetUserAddressesAsync(GuarantorStaffId);

                        if (GuarantorFamilies != null)
                        {
                            GuarantorStaffFamilies = GuarantorFamilies;
                        }

                        if (GuarantorAssress != null)
                        {
                            GuarantorStaffAssress = GuarantorAssress;
                        }

                        DebtorStaff = await psuLoan.GetUserDetailAsync(Request.DebtorStaffId);
                        StaffAddress = await psuLoan.GetUserAddressesAsync(Request.DebtorStaffId);
                        StaffFamilies = await psuLoan.GetUserFamilyAsync(Request.DebtorStaffId);
                        StaffChild = await psuLoan.GetListVStaffChildAsync(Request.DebtorStaffId);
                        StaffDetail = await psuLoan.GetLoanStaffDetailByStaffId(Request.DebtorStaffId);

                        decimal[] AllStatus = new[] { 1m, 2m, 4m, 9m, 6m, 7m, 8m, 80m, 81m, 82m, 99m, 98m, 3m, 200m };
                        ListStatusUser = userService.SetLStatusUser(Request, AllStatus);

                        await SetAgreementAsync(Request);
                        pathContract = await GetPathContract(Request);
                    }
                }

                loading = false;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                loading = false;
                await Error.ProcessError(ex);
            }
        }
    }

    private async Task SetAgreementAsync(VLoanRequestContract Agreement)
    {
        Info = new();
        LoanData = new();
        OptionLoanAgreement = new();

        try
        {
            LoanType? loanType = await psuLoan.GetLoanTypeAsync(Agreement.LoanTypeId);

            LoanData = (loanType != null ? loanType : new());

            OptionLoanAgreement.DateTitle = dateService.ConvertToDateTime(Agreement.LoanCreatedDate);

            Info.SalaryNetAmount = Agreement.SalaryNetAmount;
            Info.LoanNumInstallments = (Agreement.LoanRequestNumInstallments != null ?
                (int)Agreement.LoanRequestNumInstallments : 0);
            Info.LoanAmount = (Agreement.ContractLoanAmount != null ?
                (int)Agreement.ContractLoanAmount :
                Agreement.LoanRequestLoanAmount != null ?
                (int)Agreement.LoanRequestLoanAmount : 0);

            Info.LoanMonthlyInstallment = Agreement.LoanRequestLoanInstallment;
        }
        catch (Exception)
        {
            throw;
        }
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

    private async Task OpenPdf_2Async()
    {
        LoanAttrachmentHTML_2 = await GenerateHTML_2Async();
    }

    protected async Task<string> GenerateHTML_2Async()
    {
        var HtmlText = await AttrachmentPreviewAsync();
        var HeadHTML = await JS.InvokeAsync<string>("headHTML");
        var ScriptHTML = await JS.InvokeAsync<string>("scriptHTML");

        var Html = $"{HeadHTML} <br/> {HtmlText} <br/> {ScriptHTML}";
        return Html;
    }

    private async Task<string> AttrachmentPreviewAsync()
    {
        var HtmlText = string.Empty;
        var DebtorPartner = string.Empty;
        var GuarantorPartner = string.Empty;

        try
        {
            if (RefLoanAttrachment != null)
            {
                HtmlText = await RefLoanAttrachment.GetBoByHtmlAsync();
            }

            if (DebtorStaff?.MarriedId == "2")
            {
                if (RefDebtorPartner != null)
                {
                    DebtorPartner = await RefDebtorPartner.GetBoByHtmlAsync();
                    HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div> <br/>" +
                        $"{DebtorPartner}";
                }
            }

            if (RefGuarantor != null)
            {
                var GuarantorIT_Text = await RefGuarantor.GetBoByHtmlAsync();
                HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div> <br/>" +
                        $"{GuarantorIT_Text}";
            }

            if (GuarantStaff?.MarriedId == "2")
            {
                if (RefGuarantorPartner != null)
                {
                    GuarantorPartner = await RefGuarantorPartner.GetBoByHtmlAsync();
                    HtmlText = $"{HtmlText}" +
                        $"<div class='page-break'></div> <br/>" +
                        $"{GuarantorPartner}";
                }
            }
            return HtmlText;
        }
        catch (Exception)
        {
            throw;
        }
    }


    private async Task PrintPdfAsync(string html, string _fileName = "แบบคำขอกู้")
    {
        var fileName = $"{_fileName}.pdf";
        if (!string.IsNullOrEmpty(html))
        {
            byte[] pdfBuffer = GeneratePDFService.GeneratePDF(html);
            await GeneratePDFService.SaveFilePDFAsync(pdfBuffer, fileName);
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

    private void BackPage()
    {
        if (!string.IsNullOrEmpty(StaffID))
        {
            if (newRole != 0)
            {
                if (rootPage != 0)
                {
                    /// LoanApp.Models.BackRootPageEnum
                    switch (rootPage)
                    {
                        case (int)BackRootPageEnum.Admin_RequestDetail:
                            if (rootRequestID == 0)
                            {
                                return;
                            }
                            navigationManager.NavigateTo($"/{newRole}/CheckAgreement/{StaffID}/{rootRequestID}");
                            break;

                        case (int)BackRootPageEnum.LoanAgreementOld:
                            navigationManager.NavigateTo($"/{newRole}/LoanAgreementOld");
                            break;
                    }
                }
            }
            else if (Role != "Manager")
            {
                switch (PageTo)
                {
                    case PageControl.AdminCheckAgreement:
                        navigationManager.NavigateTo($"/Admin/CheckAgreement/{StaffID}");
                        break;
                    case PageControl.AdminCheckRequestAgreement:
                        navigationManager.NavigateTo($"/Admin/CheckRequestAgreement/{StaffID}");
                        break;
                    default: break;
                }
            }
            else
            {
                switch (PageTo)
                {
                    case PageControl.AdminCheckAgreement:
                        navigationManager.NavigateTo($"/Manager/CheckAgreement/{StaffID}");
                        break;
                    case PageControl.AdminCheckRequestAgreement:
                        navigationManager.NavigateTo($"/Manager/CheckRequestAgreement/{StaffID}");
                        break;
                    default: break;
                }
            }

        }
        else
        {
            navigationManager.NavigateTo("/LoanAgreement");
        }
    }

    private void ToPageTransaction(VLoanRequestContract agreement)
    {
        decimal Step = 3;
        if (!string.IsNullOrEmpty(StaffID))
        {
            if (newRole != 0)
            {
                if (rootPage != 0)
                {
                    /// LoanApp.Models.BackRootPageEnum
                    switch (rootPage)
                    {
                        case (int)BackRootPageEnum.Admin_RequestDetail:
                            if (rootRequestID == 0)
                            {
                                return;
                            }
                            navigationManager.NavigateTo($"/{newRole}/AgreementDetail/{StaffID}/{agreement.LoanRequestId}/{Step}/{(int)PageTo}/{rootPage}/{rootRequestID}");
                            break;

                        case (int)BackRootPageEnum.LoanAgreementOld:
                            navigationManager.NavigateTo($"/{newRole}/AgreementDetail/{StaffID}/{agreement.LoanRequestId}/{Step}/{(int)PageTo}/{rootPage}/{0}");
                            break;
                    }
                }
            }
            else if (Role != "Manager")
            {
                navigationManager.NavigateTo($"/Admin/AgreementDetail/{StaffID}/{agreement.LoanRequestId}/{Step}/{(int)PageTo}");
            }
            else
            {
                navigationManager.NavigateTo($"/Manager/AgreementDetail/{StaffID}/{agreement.LoanRequestId}/{Step}/{(int)PageTo}");
            }
        }
        else
        {
            navigationManager.NavigateTo($"/AgreementDetail/{agreement.LoanRequestId}/{Step}");
        }
    }

    private async Task<List<string>> GetPathContract(VLoanRequestContract? agreement)
    {
        List<ContractAttachment> contractAttachments = await psuLoan.GetContractFileFromContractAttachmentByLoanRequestIdAsync(agreement?.LoanRequestId);

        //var rootFile = $"{NavigationManager.BaseUri}Files";
        List<string> pathFile = new();

        if (contractAttachments.Any())
        {
            if (contractAttachments.Any())
            {
                string rootDir = saveFileAndImgService.GetFullPhysicalFilePathDir();
                foreach (var contract in contractAttachments)
                {
                    string file = $"{rootDir}\\{contract.AttachmentAddr}";
                    if (File.Exists(file))
                    {
                        pathFile.Add(saveFileAndImgService.GetUrl(contract.AttachmentAddr));
                    }
                }
            }
        }
        StateHasChanged();
        return pathFile;
    }

    private async Task SetLoanDoc()
    {
        LoadingResultImg = true;
        ResultDocList = new();
        await Task.Delay(1);

        ListDocModel? step1 = await SaveFileAndImgService.GetDocByStapAsync(1, Request, true);
        ListDocModel? step2 = await SaveFileAndImgService.GetDocByStapAsync(2, Request, true);
        ListDocModel? step3 = await SaveFileAndImgService.GetDocByStapAsync(3, Request, true);
        if (step1 != null)
        {
            ResultDocList.Add(step1);
        }
        if (step2 != null)
        {
            ResultDocList.Add(step2);
        }
        if (step3 != null)
        {
            ResultDocList.Add(step3);
        }

        LoadingResultImg = false;
        StateHasChanged();
    }

    private string? getFamilyPartner(VStaffFamily? family)
    {
        if (family == null)
        {
            return "-";
        }

        string fullName = $"{family.FamilyPartnerFname} {family.FamilyPartnerMname} {family.FamilyPartnerOldsname}";

        return fullName.Trim() == "" ? null : fullName;
    }
}
