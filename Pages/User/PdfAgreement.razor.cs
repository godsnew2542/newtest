using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using LoanApp.Model.Helper;

namespace LoanApp.Pages.User
{
    public partial class PdfAgreement
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        #region Parameter
        [Parameter] public decimal RequestID { get; set; } = 0;
        [Parameter] public string Page { get; set; } = string.Empty;
        [Parameter] public decimal StepID { get; set; } = 0;
        [Parameter] public int FromPage { get; set; } = 0;
        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public string Role { get; set; } = string.Empty;
        [Parameter] public int newRole { get; set; } = 0;
        [Parameter] public int rootPage { get; set; } = 0;
        [Parameter] public decimal rootRequestID { get; set; } = 0;

        #endregion

        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;


        private ContractAttachment? ConAttachment { get; set; } = new();

        private string Url { get; set; } = string.Empty;
        private bool ExistsFile { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            ConAttachment = new ContractAttachment();
            try
            {
                if (RequestID != 0)
                {
                    LoanRequest? loanReq = await psuLoan.GetLoanRequestByLoanRequestId(RequestID);

                    if (loanReq != null)
                    {
                        Url = $"{AppSettings.Value.RequestFilePath}";

                        if (string.IsNullOrEmpty(StaffID))
                        {
                            StaffID = (!string.IsNullOrEmpty(loanReq.DebtorStaffId) ?
                                loanReq.DebtorStaffId :
                                string.Empty);
                        }

                        if (loanReq?.LoanAttachmentId != null)
                        {
                            ConAttachment = await psuLoan.GetContractAttachmentByAttachmentId(loanReq.LoanAttachmentId);

                            if (ConAttachment != null)
                            {
                                Url = $"{Url}\\{ConAttachment.AttachmentAddr}";
                                CheckFileExists(ConAttachment.AttachmentAddr);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private void CheckFileExists(string? Addr)
        {
            if (!string.IsNullOrEmpty(Addr))
            {
                var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

                var path = $"{physicalFilePath}\\{Addr}";
                ExistsFile = File.Exists(path);
            }
        }

        private void BackPage()
        {
            if (!string.IsNullOrEmpty(Page))
            {
                if (Page == "GuarantDetail")
                {
                    if (FromPage != 0)
                    {
                        if (newRole != 0)
                        {
                            if (rootPage != 0)
                            {
                                switch (rootPage)
                                {
                                    case (int)BackRootPageEnum.Admin_RequestDetail:
                                        if (rootRequestID == 0)
                                        {
                                            return;
                                        }
                                        navigationManager.NavigateTo($"/{newRole}/GuarantDetail/{RequestID}/{StaffID}/{FromPage}/{rootPage}/{rootRequestID}");
                                        return;

                                    case (int)BackRootPageEnum.CheckGurantorAgreement:
                                        navigationManager.NavigateTo($"/{newRole}/GuarantDetail/{RequestID}/{StaffID}/{FromPage}/{rootPage}/{rootRequestID}");
                                        return;
                                }
                            }
                        }
                        else if (Role != "Manager")
                        {
                            navigationManager.NavigateTo($"/Admin/GuarantDetail/{RequestID}/{StaffID}/{FromPage}");
                        }
                        else
                        {
                            navigationManager.NavigateTo($"/Manager/GuarantDetail/{RequestID}/{StaffID}/{FromPage}");
                        }
                    }
                    else
                    {
                        navigationManager.NavigateTo($"/GuarantDetail/{RequestID}");
                    }
                }
                else if (Page == "AgreementDetail")
                {
                    if (FromPage != 0)
                    {
                        if (newRole != 0)
                        {
                            if (rootPage != 0)
                            {
                                switch (rootPage)
                                {
                                    case (int)BackRootPageEnum.Admin_RequestDetail:
                                        if (rootRequestID == 0)
                                        {
                                            return;
                                        }
                                        navigationManager.NavigateTo($"/{newRole}/AgreementDetail/{StaffID}/{RequestID}/{StepID}/{FromPage}/{rootPage}/{rootRequestID}");
                                        return;

                                    case (int)BackRootPageEnum.LoanAgreementOld:
                                        navigationManager.NavigateTo($"/{newRole}/AgreementDetail/{StaffID}/{RequestID}/{StepID}/{FromPage}/{rootPage}/{rootRequestID}");
                                        return;
                                }
                            }
                        }
                        else if (Role != "Manager")
                        {
                            navigationManager.NavigateTo($"/Admin/AgreementDetail/{StaffID}/{RequestID}/{StepID}/{FromPage}");
                        }
                        else
                        {
                            navigationManager.NavigateTo($"/Manager/AgreementDetail/{StaffID}/{RequestID}/{StepID}/{FromPage}");
                        }
                    }
                    else
                    {
                        navigationManager.NavigateTo($"/AgreementDetail/{RequestID}/{StepID}");
                    }
                }
            }
            else
            {
                navigationManager.NavigateTo($"/HomeUser");
            }
        }
    }
}
