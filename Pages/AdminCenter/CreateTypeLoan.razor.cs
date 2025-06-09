using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Model.Models.Data;
using LoanApp.Model.Settings;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace LoanApp.Pages.AdminCenter
{
    public partial class CreateTypeLoan
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;

        private List<TypeLoanDistinct> ListType { get; set; } = new();
        private LoanType Loan { get; set; } = new();
        private UploadModel ModelUpload { get; set; } = new();
        private List<ContractStep> AttachmentSteps { get; set; } = new();
        private List<AttachmentType> AttachmentList { get; set; } = new();

        public bool changType { get; set; } = false;
        public bool CheckboxMaxAmount { get; set; } = false;
        public bool CheckboxReconcile { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                var DistinctParent = await _context.LoanTypes
                    .Select(c => new { c.LoanParentId, c.LoanParentName })
                    .Distinct()
                    .OrderBy(c => c.LoanParentId)
                    .ToListAsync();

                if (DistinctParent.Any())
                {
                    foreach (var data in DistinctParent)
                    {
                        TypeLoanDistinct tmp = new();
                        tmp.LoanParentId = data.LoanParentId;
                        tmp.LoanParentName = data.LoanParentName;
                        ListType.Add(tmp);
                    }
                }

                #region Default Val
                Loan.Active = 1;
                Loan.LoanTypeName = string.Empty;
                Loan.LoanParentName = string.Empty;
                Loan.LoanInterest = 0;
                Loan.LoanNumInstallments = 0;
                Loan.LoanMaxAmount = 0;
                Loan.IsReconcile = 0;
                #endregion

                await AttachmentDocumentAsync();
                await CleanSessionStorageAsync();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }

        }

        private async Task AttachmentDocumentAsync()
        {
            AttachmentSteps = await _context.ContractSteps.ToListAsync();
        }

        private async Task SetCurrentDataAsync(DTEventArgs value)
        {
            ModelUpload.Name = value.Params[0].ToString();
            ModelUpload.Url = value.Params[1].ToString();
            ModelUpload.TempImgName = value.Params[2].ToString();

            string FileExtention = Path.GetExtension(ModelUpload.Name!);
            if (FileExtention != ".pdf")
            {
                ModelUpload = new UploadModel();
                string alert = "รับเฉพาะไฟล์ .pdf";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                return;
            }
        }

        private void BackPage()
        {
            navigationManager.NavigateTo("./Admin/ManageTypeLoan");
        }

        private void ToggleButton(bool close)
        {
            if (close == true)
            {
                changType = false;
            }
            else
            {
                changType = !changType;
            }
        }

        private void SelectType(bool toggle, TypeLoanDistinct loantype)
        {
            Loan.LoanParentName = loantype.LoanParentName;
            Loan.LoanParentId = loantype.LoanParentId;
            if (string.IsNullOrEmpty(Loan.LoanTypeName))
            {
                Loan.LoanTypeName = loantype.LoanParentName;
            }
            ToggleButton(true);
        }

        private async Task SaveToDbAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Loan.LoanParentName))
                {
                    string alert = "กรูณาระบุชื่อประเภทกู้ยืม";
                    await JS.InvokeVoidAsync("displayTickerAlert", alert);
                    return;
                }
                if (string.IsNullOrEmpty(Loan.LoanTypeName))
                {
                    string alert = "กรูณาระบุชื่อประเภทย่อย";
                    await JS.InvokeVoidAsync("displayTickerAlert", alert);
                    return;
                }
                if (Loan.LoanNumInstallments == 0)
                {
                    string alert = "กรูณาระบุจำนวนงวด";
                    await JS.InvokeVoidAsync("displayTickerAlert", alert);
                    return;
                }
                if (Loan.LoanMaxAmount == 0 && !CheckboxMaxAmount)
                {
                    string alert = "กรูณาระบุจำนวนเงิน";
                    await JS.InvokeVoidAsync("displayTickerAlert", alert);
                    return;
                }
                else if (CheckboxMaxAmount)
                {
                    Loan.LoanMaxAmount = 0;
                }
                if (string.IsNullOrEmpty(ModelUpload.Url))
                {
                    string alert = "กรูณาอัปโหลดไฟล์ สำเนาเอกสารเงื่อนไข";
                    await JS.InvokeVoidAsync("displayTickerAlert", alert);
                    return;
                }

                /* เริ่มใช้เงื่อนไขเมื่อ */
                Loan.Remark += $"_{DateTime.Now.ToString("dd/M/yyyy HH:mm:ss")}";

                _context.LoanTypes.Add(Loan);
                await _context.SaveChangesAsync();
                var id = Loan.LoanTypeId;

                if (!string.IsNullOrEmpty(ModelUpload.Url))
                {
                    await MoveFileAsync(id);
                }

                if (AttachmentList.Count != 0)
                {
                    await AddAttachmentRequiredAsync(AttachmentList, id, Loan);
                }

                BackPage();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }

        }

        private async Task AddAttachmentRequiredAsync(List<AttachmentType> attRequired, int loanTypeId, LoanType loanData)
        {
            try
            {
                for (int i = 0; i < attRequired.Count; i++)
                {
                    var item = attRequired[i];
                    AttachmentRequired AttachmentReq = new();

                    AttachmentReq.LoanTypeId = loanTypeId;
                    AttachmentReq.AttachmentTypeId = item.AttachmentTypeId;
                    AttachmentReq.LoanTypeName = loanData.LoanTypeName;
                    _context.AttachmentRequireds.Add(AttachmentReq);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task MoveFileAsync(int TypeId)
        {
            try
            {
                /* loanDoc/1 */
                var DIR = $"{Utility.DOC_DIR}\\{TypeId}";

                var physicalFilePath = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.PhysicalFilePath : fileUploadSetting.Value.Linux.PhysicalFilePath;

                var dirToSave = $"{physicalFilePath}\\{DIR}";
                Utility.CheckFolder(dirToSave);

                var fileName = ModelUpload.TempImgName;
                var path_To = ModelUpload.Url;
                var filePath_From = $"{dirToSave}\\{fileName}";
                var KeepPath = $"{DIR}\\{fileName}";
                File.Move(path_To!, filePath_From);
                await SaveFileAndImgService.SaveDocAttachmentAsync(KeepPath, ModelUpload.Name!, TypeId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ChangeParentName(string LoanTypeName)
        {
            Loan.LoanParentName = LoanTypeName;
            if (string.IsNullOrEmpty(Loan.LoanTypeName))
            {
                Loan.LoanTypeName = LoanTypeName;
            }
            /* Count ParentName */
            var ParentName_Index = _context.LoanTypes.Select(c => c.LoanParentId).Distinct().ToList();
            var ParentNameCount = ParentName_Index.Count + 1;
            Loan.LoanParentId = (byte)ParentNameCount;
        }

        private async Task AttachmentTypeDataAsync()
        {
            AttachmentList = new();
            if (AttachmentSteps.Count != 0)
            {
                foreach (var id in AttachmentSteps)
                {
                    var NameStorage = $"AttachmentType_{id.ContractStepId}";
                    var checkData = await sessionStorage.GetItemAsStringAsync(NameStorage);
                    if (checkData != null)
                    {
                        var value = await sessionStorage.GetItemAsync<List<decimal>>(NameStorage);
                        if (value.Count != 0)
                        {
                            foreach (var ContractStepId in value)
                            {
                                AttachmentType? Attachment = _context.AttachmentTypes
                                    .Where(e => e.AttachmentTypeId == ContractStepId)
                                    .FirstOrDefault();

                                if (Attachment != null)
                                {
                                    AttachmentList.Add(Attachment);
                                }
                            }
                        }
                    }
                    else { }
                }
            }
        }

        private async Task CleanSessionStorageAsync()
        {
            AttachmentList = new();
            try
            {
                if (AttachmentSteps.Count != 0)
                {
                    foreach (var id in AttachmentSteps)
                    {
                        var NameStorage = $"AttachmentType_{id.ContractStepId}";
                        var checkData = await sessionStorage.GetItemAsStringAsync(NameStorage);
                        if (checkData != null)
                        {
                            await sessionStorage.RemoveItemAsync(NameStorage);
                        }
                        else { }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void CheckboxClicked(object checkedValue)
        {
            CheckboxMaxAmount = (bool)checkedValue;
            Loan.LoanMaxAmount = 0;
        }

        private void CheckReconcile(object checkedValue)
        {
            CheckboxReconcile = (bool)checkedValue;
            if (CheckboxReconcile)
            {
                Loan.IsReconcile = 1;
            }
            else
            {
                Loan.IsReconcile = 0;
            }
        }
    }

    public class TypeLoanDistinct
    {
        public byte? LoanParentId { get; set; }
        public string LoanParentName { get; set; } = string.Empty;
    }
}
