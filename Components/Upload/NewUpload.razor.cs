using LoanApp.Model.Helper;
using LoanApp.Model.Models.Data;
using LoanApp.Model.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Text;

namespace LoanApp.Components.Upload
{
    public partial class NewUpload
    {
        #region Parameter
        [Parameter] public EventCallback<DTEventArgs> SetChildData { get; set; }
        [Parameter] public decimal AttachmentTypeId { get; set; } = 0;
        [Parameter] public string MyFile { get; set; } = "my_file0";
        [Parameter] public string[] ArrayTypeFileImg { get; set; } = new[] { "JPEG", "JPG", "PNG", "PDF" };

        #endregion

        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;

        IReadOnlyList<IBrowserFile>? selectedFiles;
        private int maxFileSize = 10485760 * 2; //20 MB

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("emptyFile");
            }
        }

        private async Task OnInputFileChangeAsync(InputFileChangeEventArgs e)
        {
            selectedFiles = e.GetMultipleFiles();
            this.StateHasChanged();
            await OnSubmitAsync();
        }

        async Task OnSubmitAsync()
        {
            if (selectedFiles != null)
            {
                foreach (var file in selectedFiles)
                {
                    try
                    {
                        if (file.Size <= maxFileSize)
                        {
                            var tempName = CreateFileCombine(file);
                            var url = await SaveFileAsync(file, tempName);
                            await AddDataAsync(file.Name, url, tempName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                selectedFiles = null;
                this.StateHasChanged();
            }
            else
            {
                string alert = "กรูณาเลือกไฟล์เพื่อดำเนินการต่อไป";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
            }
        }

        public async Task AddDataAsync(string name, string url, string tempName)
        {
            DTEventArgs arg = new();
            arg.Params.Add(name);
            arg.Params.Add(url);
            arg.Params.Add(tempName);
            arg.Params.Add(AttachmentTypeId);

            int LastIndex = name.LastIndexOf('.');
            var ext = name.Substring(LastIndex + 1);

            List<string> typeFile = ArrayTypeFileImg.ToList();

            if (typeFile.Contains(ext.ToUpper()))
            {
                await SetChildData.InvokeAsync(arg);
            }
            else
            {
                var mess = ConcatenateWithCommas(typeFile);
                string alert = $"ไม่สามารถอัปโหลดไฟล์ได้เนื่องจากระบบจะรับไฟล์ นามสกุล {mess} เท่านั้น";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
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

        private string CreateFileCombine(IBrowserFile file)
        {
            var dirToSave = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.TempDir : fileUploadSetting.Value.Linux.TempDir;
            Utility.CheckFolder(dirToSave);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.Name);

            return fileName;
        }

        private async Task<string> SaveFileAsync(IBrowserFile file, string fileName)
        {
            var dirToSave = Utility.CheckOSisWindows() ? fileUploadSetting.Value.Windows.TempDir : fileUploadSetting.Value.Linux.TempDir;
            var filePath = Path.Combine(dirToSave!, fileName);

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
    }
}
