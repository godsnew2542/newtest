using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LoanApp.Pages.Admin
{
    public partial class CheckPremise
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        #endregion

        [Parameter] public decimal RequestID { get; set; } = 0;

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;

        #endregion

        private VLoanStaffDetail? StaffDetail { get; set; } = new();
        private LoanRequest? LoanRequest { get; set; } = new();
        private VLoanRequestContract? ReqCon { get; set; } = new();

        private string StaffID { get; set; } = string.Empty;
        private bool EditContractNoVisible { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                StaffID = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                if (RequestID != 0 && !string.IsNullOrEmpty(StaffID))
                {
                    StaffDetail = await psuLoan.GetUserDetailAsync(StaffID);
                    LoanRequest = await psuLoan.GetLoanRequestByLoanRequestId(RequestID);
                    ReqCon = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private string GetData(LoanRequest? data)
        {
            var message = string.Empty;
            if (data != null)
            {
                var LoneType = userService.GetLoanType(data.LoanTypeId);
                var Type = userService.GetLoanName(LoneType);
                var Amount = data.LoanAmount;
                var Installment = data.LoanNumInstallments;

                message = $"ของคุณ{userService.GetFullNameNoTitleName(data.DebtorStaffId)}" +
                    $" ( ประเภท {Type} ยอดเงิน : {String.Format("{0:n2}", Amount)} บาท " +
                    $"จำนวน : {Installment} งวด ) ";
            }
            return message;
        }

        private async Task<List<VAttachmentRequired>> GetAttachmentTitle(LoanRequest Request)
        {
            decimal StepId = 3m;
            List<VAttachmentRequired> VAttachment = new();
            ContractMain? main = await psuLoan.GeContractMainByLoanRequestId(Request.LoanRequestId);

            if (main != null)
            {
                VAttachment = await psuLoan.GetListVAttachmentRequired((byte?)main.LoanTypeId, StepId);
            }
            return VAttachment;
        }

        private async Task<ImgInDatabaseModel> GetAttachmentAsync(VAttachmentRequired Attachment, string? StaffId)
        {
            ImgInDatabaseModel img = new();
            decimal StepId = 3m;
            if (string.IsNullOrEmpty(StaffId))
            {
                return img;
            }

            try
            {
                var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

                var ItemUpload = await _context.ContractAttachments
                    .Where(c => c.LoanRequestId == RequestID)
                    .ToListAsync();

                if (ItemUpload.Any())
                {
                    //  Files\\1465_0000052\\3
                    var RootFolder = $"{Utility.Files_DIR}\\{RequestID}_{StaffId}\\{StepId}";
                    var dir = $"{physicalFilePath}\\{RootFolder}";

                    foreach (var url in ItemUpload)
                    {
                        string[] Split1 = url!.AttachmentAddr!.Split("\\");
                        string file = Split1[Split1.Length - 1];

                        string result = Path.GetFileName(file);
                        string[] Split = result.Split('_');
                        var AttachmentTypeId = Attachment.AttachmentTypeId;
                        if (Split[0] == $"{Attachment.AttachmentTypeId}")
                        {
                            UploadModel upload = new();
                            upload.Id = img.ImgFail.Count + 1;
                            upload.Url = url.AttachmentAddr;
                            upload.Name = url.AttachmentFileName;
                            upload.TempImgName = file;
                            upload.AttachmentTypeId = Attachment.AttachmentTypeId;
                            img.ImgFail.Add(upload);
                        }
                    }

                    var di = new DirectoryInfo(dir);

                    if (di.Exists)
                    {
                        foreach (string file in Directory.EnumerateFiles(dir))
                        {
                            string result = Path.GetFileName(file);
                            var extension = Path.GetExtension(file);

                            /// เช็คประเภทไฟลืที่ไม่อ่านค่ามา
                            if (!Utility.NotReadTypeFile.Contains(Path.GetExtension(file)))
                            {
                                string[] Split = result.Split('_');
                                if (Split[0] == $"{Attachment.AttachmentTypeId}")
                                {
                                    var path = $"{RootFolder}\\{result}";

                                    UploadModel upload = new();
                                    upload.Id = img.ImgSuccess.Count + 1;
                                    upload.Url = SaveFileAndImgService.GetUrl(path);
                                    upload.TempImgName = result;
                                    upload.AttachmentTypeId = Attachment.AttachmentTypeId;
                                    img.ImgSuccess.Add(upload);

                                    img.ImgFail.RemoveAt(img.ImgFail.FindIndex(x => x.TempImgName == result));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return img;
        }

        private void Back()
        {
            navigationManager.NavigateTo("/Admin/ManageLoanAgreement/7");
        }

        private async Task SaveToDbAsync(LoanRequest? Request)
        {
            decimal? ContractStatusId = 0;

            try
            {
                ContractMain? main = await psuLoan.GeContractMainByLoanRequestId(Request?.LoanRequestId);

                if (main != null)
                {
                    ContractStatusId = main.ContractStatusId;
                    main.ContractStatusId = 8;
                    main.AdminStaffIdValidate = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                    main.AdminValidateDate = DateTime.Now;

                    await psuLoan.UpdateContractMain(main);
                    //_context.Update(main);
                    //await _context.SaveChangesAsync();

                    await SaveToHistoryAsync(main, ContractStatusId);
                    navigationManager.NavigateTo("/Admin/ManageLoanAgreement/7");
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task SaveToHistoryAsync(ContractMain contract, decimal? ContractStatusId)
        {
            try
            {
                decimal? LoanStatusId = ContractStatusId;
                string ModifyBy = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);

                await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, LoanStatusId, ModifyBy);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task BackToUploadAsync(LoanRequest? Request)
        {
            decimal? ContractStatusId = 0;

            try
            {
                ContractMain? main = await psuLoan.GeContractMainByLoanRequestId(Request?.LoanRequestId);

                if (main != null)
                {
                    ContractStatusId = main.ContractStatusId;
                    main.ContractStatusId = 200;
                    main.AdminStaffIdValidate = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                    main.AdminValidateDate = DateTime.Now;

                    _context.Update(main);
                    await _context.SaveChangesAsync();

                    await SaveToHistoryAsync(main, ContractStatusId);
                    navigationManager.NavigateTo("/Admin/ManageLoanAgreement/7");
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        /// <summary>
        /// เช็คว่าสามารถแก้ไข เลขที่สัญญาได้ หรือไหม โดยการ  check ว่าเคยมีการชำระเงินหรือยัง (PaymentTransaction) 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>true = สามรถแก้ไขได้ || false = ไม่สามรถแก้ไขได้</returns>
        private async Task<bool> IsChangeContractNo(VLoanRequestContract? data)
        {
            if ((data != null && !string.IsNullOrEmpty(data.ContractNo)))
            {
                var val = await psuLoan.GetAllPaymentTransactionByContractNo(data.ContractNo);
                if (!val.Any())
                {
                    return true;
                }
            }

            return false;
        }

        private void CallbackData(bool e)
        {
            EditContractNoVisible = e;
            var url = navigationManager.Uri.Split(navigationManager.BaseUri);
            if (url.Length == 2)
            {
                navigationManager.NavigateTo(url[1], true);
            }
        }
    }
}
