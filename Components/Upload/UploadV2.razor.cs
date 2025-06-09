using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using LoanApp.Model.Settings;
using System.Text;
using DocumentFormat.OpenXml.Office2016.Excel;

namespace LoanApp.Components.Upload
{
    public partial class UploadV2
    {
        #region Parameter
        /// <summary>
        /// EventCallback
        /// </summary>
        [Parameter]
        public EventCallback<UploadModel> OnUploadCallback { get; set; }

        /// <summary>
        /// สำหรับกำหนดจำนวนไฟล์สูงสุดที่อนุญาตให้ upload
        /// </summary>
        [Parameter]
        public int MaxAllowedFiles { get; set; } = 1;

        /// <summary>
        /// Title text ค่าเริ่มต้น Upload
        /// </summary>
        [Parameter]
        public string? Title { get; set; } = null;

        /// <summary>
        /// สำหรับกำหนดประเภทไฟล์ที่อนุญาตให้ upload ค่าเริ่มต้น { "JPEG", "JPG", "PNG" }
        /// </summary>
        [Parameter]
        public List<string> FileTypes { get; set; } = new List<string>() { "JPEG", "JPG", "PNG", "PDF" };

        /// <summary>
        /// ใช่สำหรับการส่ง type File ที่ต้องการใหม่ทั้งหมด
        /// </summary>
        [Parameter] public List<string>? ResetTypeFile { get; set; } = null;

        /// <summary>
        /// ใช่สำหรับการส่ง type File ที่ต้องการเพิ่มเติม นอกจาก ค่าเริ่มต้น { "JPEG", "JPG", "PNG", "PDF" }
        /// </summary>
        [Parameter] public string[]? AddOnTypeFile { get; set; } = null;

        [Parameter] public decimal AttachmentTypeId { get; set; } = 0;

        #endregion

        #region Inject
        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] private IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        #endregion

        /// <summary>
        /// ขนาดไฟล์สูงสุด (MB)
        /// </summary>
        private long MaxFileSize { get; set; }

        /// <summary>
        /// DirToSave
        /// </summary>
        private string? DirToSave { get; set; } = null;

        protected override void OnInitialized()
        {
            if (ResetTypeFile != null)
            {
                FileTypes = ResetTypeFile;
            }

            if (AddOnTypeFile != null)
            {
                foreach (var type in AddOnTypeFile)
                {
                    FileTypes.Add(type);
                }
            }
        }

        public async Task LoadFiles(InputFileChangeEventArgs e)
        {
            try
            {
                DirToSave = (Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.TempDir : fileUploadSetting.Value.Linux.TempDir);

                var selectedFiles = e.GetMultipleFiles();
                await OnSubmitAsync(selectedFiles);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("maximum"))
                {
                    await notificationService.Error("อัพโหลดไฟล์เกินจำนวนที่กำหนด (" + MaxAllowedFiles.ToString() + " ไฟล์)");
                    return;
                }
                await notificationService.Error(notificationService.ExceptionLog(ex));
            }
        }

        private async Task OnSubmitAsync(IReadOnlyList<IBrowserFile>? browserFiles)
        {
            if (browserFiles != null)
            {
                MaxFileSize = fileUploadSetting.Value.FileMaxSize * (1024 * 1024);
                foreach (var file in browserFiles)
                {
                    try
                    {
                        if (file.Size <= MaxFileSize)
                        {
                            string? tempName = await CreateFileCombine(file);
                            if (!string.IsNullOrEmpty(tempName))
                            {
                                var url = await SaveFileAsync(file, tempName, MaxFileSize);
                                await AddDataAsync(file.Name, url, tempName, file.Size);
                            }
                        }
                        else
                        {
                            await notificationService.Error("ขนาดไฟล์เกินกำหนด กรุณาอัพโหลดไฟล์ที่น้อยกว่า " + ConvertFileSize(MaxFileSize));
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                StateHasChanged();
            }
            else
            {
                await notificationService.Warning("กรูณาเลือกไฟล์เพื่อดำเนินการต่อไป");
            }
        }

        public async Task AddDataAsync(string name, string url, string tempName, long fileSize)
        {
            SaveFileAndImgService.AutoDeleteFileInFolderTemp();

            UploadModel upload = new()
            {
                GuId = Guid.NewGuid().ToString(),
                Name = name,
                Url = url,
                TempImgName = tempName,
                FileSize = fileSize,
                AttachmentTypeId = AttachmentTypeId,
            };

            if (ValidateExtension(name))
            {
                await OnUploadCallback.InvokeAsync(upload);
            }
            else
            {
                var mess = ConcatenateWithCommas(FileTypes);
                string alert = $"ไม่สามารถอัปโหลดไฟล์ได้เนื่องจากระบบจะรับไฟล์ นามสกุล {mess} เท่านั้น";
                await notificationService.Error($"{alert}", "นามสกุลไฟล์ไม่ถูกต้อง", false);
            }
        }

        private static string ConcatenateWithCommas(List<string> typeFile)
        {
            if (typeFile == null || typeFile.Count == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder(typeFile[0]);

            for (int i = 1; i < typeFile.Count; i++)
            {
                result.Append(", ").Append(typeFile[i]);
            }

            return result.ToString();
        }

        private async Task<string> SaveFileAsync(IBrowserFile file, string fileName, long maxFileSize)
        {
            var filePath = Path.Combine(DirToSave!, fileName);

            using (var stream = file.OpenReadStream(maxFileSize))
            {
                using (var mstream = new MemoryStream())
                {
                    using (Stream streamToWriteTo = File.Open(filePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(streamToWriteTo);
                    }
                }
            }
            return filePath;
        }

        private async Task<string?> CreateFileCombine(IBrowserFile file)
        {
            string? fileName = null;
            if (!string.IsNullOrEmpty(DirToSave))
            {
                Utility.CheckFolder(DirToSave);
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.Name);
            }
            else
            {
                await notificationService.Error("ระบบเกิดข้อผิดพลาด");
            }
            return fileName;
        }

        /// <summary>
        /// ตรวจสอบประเภทไฟล์
        /// </summary>
        /// <param name="extension">ประเภทไฟล์ (รวม .) เช่น .Doc, .docx</param>
        /// <returns>bool</returns>
        private bool ValidateExtension(string extension)
        {
            // Get the file extension
            //string fileExtension = Path.GetExtension(extension)

            int lastIndex = extension.LastIndexOf('.');
            var ext = extension.Substring(lastIndex + 1);

            FileTypes = FileTypes.ConvertAll(type => type.ToUpper());

            if (FileTypes.Contains(ext.ToUpper()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// แปลงขนาดของไฟล์ byte เป็น string และ หน่วย
        /// </summary>
        /// <param name="fileSize">ขนาดของไฟล์</param>
        /// <returns>string</returns>
        private string ConvertFileSize(long fileSize)
        {
            var units = new[] { "B", "KB", "MB", "GB", "TB" };
            var index = 0;
            double size = fileSize;
            while (size > 1024)
            {
                size /= 1024;
                index++;
            }
            return string.Format("{0:0.00} {1}", size, units[index]);
        }
    }
}
