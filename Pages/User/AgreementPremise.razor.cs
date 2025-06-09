using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Models.Data;
using LoanApp.Model.Settings;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Reflection.Metadata;

namespace LoanApp.Pages.User
{
    public partial class AgreementPremise
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #endregion

        #region Parameter
        [Parameter] public decimal RequestID { get; set; } = 0;
        [Parameter] public decimal StepID { get; set; } = 0;
        [Parameter] public int FromPage { get; set; } = 0;
        [Parameter] public string? Role { get; set; } = null;

        /// <summary>
        /// true = แก้ไขไฟล์ 
        /// </summary>
        [Parameter] public bool EditUpload { get; set; } = false;

        #endregion

        #region Inject
        [Inject] private IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private Services.IServices.INotificationService notification { get; set; } = null!;

        #endregion



        private VLoanRequestContract? Request { get; set; } = new();
        private List<VAttachmentRequired> ItemUpload { get; set; } = new();
        private List<UploadModel> ResultInfoList { get; set; } = new();
        private List<ListDocModel> ResultDocList { get; set; } = new();

        /// <summary>
        /// สำหรับตรวจสอบเอกสารท่จำเป็นต้อง upload Step 3
        /// </summary>
        private List<decimal> ReqDocList { get; } = new() { 11m, 13m, 14m };
        private string? StaffID { get; set; } = string.Empty;
        private bool LoadingResultImg { get; set; } = false;
        private string? Fullname { get; set; } = null;

        /// <summary>
        /// check การอัปหลดแต่ละประเภทของไฟล์ => false = อัปโหลดมาไม่ครบ
        /// </summary>
        private bool? IsFileSuccess { get; set; } = null;
        private bool EditContractNoVisible { get; set; } = false;


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {


                    if (RequestID != 0)
                    {
                        Request = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);

                        if (Request != null)
                        {
                            Fullname = $"{"คุณ"}{Request.DebtorNameTh} {Request.DebtorSnameTh}";
                            StaffID = Request.DebtorStaffId;

                            ItemUpload = await psuLoan.GetListVAttachmentRequired(Request.LoanTypeId, StepID);

                            if (EditUpload)
                            {
                                await GetFileOldAsync(Request);
                            }
                        }
                    }
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
            }
        }

        private string? GetHeaderFullNameAndLoanTypeName()
        {
            if (Request != null)
            {
                return $"คุณ{Request.DebtorNameTh} {Request.DebtorSnameTh} {Request.LoanTypeName}";
            }
            return null;
        }

        private async Task GetFileOldAsync(VLoanRequestContract req)
        {
            var RootFolder = $"{Utility.Files_DIR}\\{req.LoanRequestId}_{req.DebtorStaffId}";

            var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

            var dir = $"{physicalFilePath}\\{RootFolder}\\{StepID}";
            var di = new DirectoryInfo(dir);
            if (di.Exists)
            {
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    string result = Path.GetFileName(file);
                    string[] Split = result.Split('_');
                    var myTodo = ItemUpload.Find(x => x.AttachmentTypeId == Convert.ToInt32(Split[0]));

                    if (myTodo != null)
                    {
                        var path = $"{RootFolder}\\{StepID}\\{result}";
                        await AddDataAsync(path, RequestID, result, myTodo.AttachmentTypeId);
                    }
                }
            }
        }

        private async Task AddDataAsync(string path, decimal loanRequestId, string tempName, decimal AttachmentTypeId)
        {
            try
            {
                ContractAttachment? contractAttachment = await SaveFileAndImgService.GetContractAttachmentAsync(path, loanRequestId);

                if (contractAttachment != null)
                {
                    DTEventArgs arg = new();
                    arg.Params.Add(!string.IsNullOrEmpty(contractAttachment.AttachmentFileName) ?
                        contractAttachment.AttachmentFileName : string.Empty);
                    arg.Params.Add(!string.IsNullOrEmpty(contractAttachment.AttachmentAddr) ?
                        contractAttachment.AttachmentAddr : string.Empty);
                    arg.Params.Add(tempName);
                    arg.Params.Add(AttachmentTypeId);

                    var data = SetCurrentData(arg);
                    var upload = ResultInfoList.Find(x => x.Id == data.Id);
                    if (upload != null)
                    {
                        upload.CollectByDB = true;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private UploadModel SetCurrentData(DTEventArgs value)
        {
            UploadModel ModelUpload = new()
            {
                Id = ResultInfoList.Count() + 1,
                Name = value.Params[0].ToString(),
                Url = value.Params[1].ToString(),
                TempImgName = value.Params[2].ToString(),
                AttachmentTypeId = (decimal)value.Params[3]
            };
            ResultInfoList.Add(ModelUpload);
            return ModelUpload;
        }

        private async Task CurrentRemoveListAsync(int value)
        {
            try
            {
                /// เช็ค user can remove file
                ///  - EditUpload && string.IsNullOrEmpty(Role) 
                ///  - string.IsNullOrEmpty(Role)
                bool isRemoveFromDB = (EditUpload && string.IsNullOrEmpty(Role) ? true : string.IsNullOrEmpty(Role) ? true : false);

                if (isRemoveFromDB)
                {
                    var model = await SaveFileAndImgService.RemoveDocListAsync(value, ResultInfoList, isRemoveFromDB);

                    if (model != null)
                    {
                        ResultInfoList = model.ResultList;

                        if (model.RemoveResult != null)
                        {
                            await RemoveContractAttachmentAsync(model.RemoveResult.Url!, Request!.LoanRequestId);
                        }
                    }
                }
                else
                {
                    UploadModel? uploadModel = await SaveFileAndImgService.ReadStorageSelectUploadAsync();
                    if (uploadModel != null)
                    {
                        if (uploadModel.CollectByDB)
                        {
                            bool CheckConfirmButton = await JS.InvokeAsync<Boolean>("ConfirmButton", "ลบไฟล์ออกจากระบบ ต้องการดำเนินการต่อหรือไม่ ?");

                            if (CheckConfirmButton)
                            {
                                var model = await SaveFileAndImgService.BackUpFileDocListAsync(value, ResultInfoList, StateProvider?.CurrentUser.StaffId);
                                if (model != null)
                                {
                                    ResultInfoList = model.ResultList;

                                    if (model.RemoveResult != null)
                                    {
                                        await RemoveContractAttachmentAsync(model.RemoveResult.Url!, Request!.LoanRequestId);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var model = await SaveFileAndImgService.BackUpFileDocListAsync(value, ResultInfoList, StateProvider?.CurrentUser.StaffId);
                            if (model != null)
                            {
                                ResultInfoList = model.ResultList;

                                if (model.RemoveResult != null)
                                {
                                    await RemoveContractAttachmentAsync(model.RemoveResult.Url!, Request!.LoanRequestId);
                                }
                            }
                        }
                    }

                }
                await Task.Delay(1);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task RemoveContractAttachmentAsync(string Url, decimal LoanRequestId)
        {
            try
            {
                ContractAttachment? contractAttachment = await SaveFileAndImgService.GetContractAttachmentAsync(Url, LoanRequestId);
                /// ลบออกจาก DB
                await psuLoan.RemoveContractAttachmentAsync(contractAttachment);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Back()
        {
            if (FromPage != 0)
            {
                switch (FromPage)
                {
                    case (int)PageControl.AdminCheckAgreement:
                        navigationManager.NavigateTo($"/Admin/CheckAgreement/{StaffID}");
                        break;

                    case (int)PageControl.AdminCheckRequestAgreement:
                        navigationManager.NavigateTo($"/Admin/CheckRequestAgreement/{StaffID}");
                        break;

                    case (int)PageControl.AdminManageLoanAgreement:
                        if (EditUpload)
                        {
                            navigationManager.NavigateTo($"/Admin/ManageLoanAgreement/200");
                        }
                        else
                        {
                            navigationManager.NavigateTo($"/Admin/ManageLoanAgreement/6");
                        }
                        break;

                    case (int)PageControl.LoanAgreementOld:
                        navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Admin}/LoanAgreementOld");
                        break;

                    default: break;
                }
            }
            else
            {
                navigationManager.NavigateTo($"/LoanAgreement");
            }
        }

        private void CheckIsFileSuccess()
        {
            IsFileSuccess = null;
            List<decimal> AttachmentTypeIdReq = new();
            foreach (var item in ReqDocList)
            {
                var myTodo = ItemUpload.Find(x => x.AttachmentTypeId == item);
                if (myTodo != null)
                {
                    AttachmentTypeIdReq.Add(myTodo.AttachmentTypeId);
                }
            }

            if (AttachmentTypeIdReq.Any())
            {
                foreach (var item in AttachmentTypeIdReq)
                {
                    var myTodo = ResultInfoList.Find(x => x.AttachmentTypeId == item);
                    if (myTodo == null)
                    {
                        IsFileSuccess = false;
                        StateHasChanged();
                        return;
                    }
                }
            }

            IsFileSuccess = true;
            StateHasChanged();
        }

        private async Task ConfirmdocAsync(List<UploadModel> listImg)
        {
            try
            {
                await SaveToFolderImagesAsync(listImg);

                ContractMain? ConMain = await psuLoan.GeContractMainByContractIdAsync(Request!.ContractId);
                if (ConMain != null)
                {
                    decimal? Old_ContractStatusId = ConMain.ContractStatusId;
                    ConMain.ContractStatusId = 7;

                    await psuLoan.UpdateContractMain(ConMain);

                    await SaveToHistoryAsync(ConMain, Old_ContractStatusId);
                    await SetMessageAsync();
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task SetMessageAsync()
        {
            List<object> Message = new();
            var subMessage = $"โปรดรอผลการตรวจสอบจากเจ้าหน้าที่";
            Message.Add(subMessage);

            MessageModel mes = new()
            {
                Title = "อัปโหลดหลักฐานสำเร็จ",
                Message = Message,
            };

            var page = string.Empty;
            if (FromPage != 0)
            {
                switch (FromPage)
                {
                    case (int)PageControl.AdminCheckAgreement:
                        page = $"/Admin/CheckAgreement/{StaffID}";
                        break;
                    default: break;
                }
            }
            else
            {
                page = $"/LoanAgreement";
            }
            mes.ToPage = page;
            await sessionStorage.SetItemAsync("Message", mes);
            navigationManager.NavigateTo("/Message", true);
        }

        private async Task SaveToHistoryAsync(ContractMain contract, decimal? Old_LoanStatusId)
        {
            string ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
            await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, Old_LoanStatusId, ModifyBy);
        }

        private async Task SaveToFolderImagesAsync(List<UploadModel> ResultInfoList)
        {
            var STEP_ID = 3;
            try
            {
                var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

                if (!string.IsNullOrEmpty(StaffID))
                {
                    // Files/9_0001972/3
                    var DIR = $"{Utility.Files_DIR}\\{RequestID}_{StaffID}\\{STEP_ID}";
                    var dirToSave = $"{physicalFilePath}\\{DIR}";
                    Utility.CheckFolder(dirToSave);

                    foreach (var ele in ResultInfoList)
                    {
                        if (!ele.CollectByDB)
                        {
                            var fileName = $"{ele.AttachmentTypeId}_{ele.TempImgName}";
                            var path_To = ele.Url;

                            var filePath_From = $"{dirToSave}\\{fileName}";
                            var KeepPath = $"{DIR}\\{fileName}";
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

        private async Task ShowImgAsync(List<UploadModel> LResultImg, decimal AttTypeId, VAttachmentRequired TitleList)
        {
            LoadingResultImg = true;
            ResultDocList = new();
            ListDocModel docModel = new();
            var i18N = await SaveFileAndImgService.GetAttachmentNameAsync(AttTypeId);
            docModel.TitleList.Add(TitleList);

            await Task.Delay(1);

            foreach (var resultImg in LResultImg)
            {
                UploadModel upload = new();
                if (resultImg.AttachmentTypeId == AttTypeId)
                {
                    upload = resultImg;

                    if (!resultImg.CollectByDB)
                    {
                        upload.Url = $"Temp\\{resultImg.TempImgName}";
                    }
                    docModel.ResultList.Add(upload);
                }
            }
            ResultDocList.Add(docModel);
            LoadingResultImg = false;
            StateHasChanged();
        }

        /// <summary>
        /// เช็คว่าสามารถแก้ไข เลขที่สัญญาได้ หรือไหม โดยการ  check ว่าเคยมีการชำระเงินหรือยัง (PaymentTransaction) 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>true = สามรถแก้ไขได้ || false = ไม่สามรถแก้ไขได้</returns>
        private async Task<bool> IsChangeContractNo(VLoanRequestContract? data)
        {
            if ((!string.IsNullOrEmpty(Role) && Role == RoleTypeEnum.Admin.ToString()) && (data != null && !string.IsNullOrEmpty(data.ContractNo)))
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
