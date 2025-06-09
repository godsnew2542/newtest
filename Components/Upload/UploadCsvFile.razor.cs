using LoanApp.Model.Models;
using LoanApp.Model.Models.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.Upload
{
    public partial class UploadCsvFile
    {
        #region Parameter
        [Parameter] public string GenId { get; set; } = string.Empty;
        [Parameter] public string myFile { get; set; } = string.Empty;
        [Parameter] public int ModelId { get; set; } = 99;
        [Parameter] public decimal AttTypeId { get; set; } = 0;
        [Parameter] public EventCallback<UploadModel> SetChildData { get; set; }

        #endregion

        private UploadModel ModelUploadCSV { get; set; } = new();

        public async Task SetCurrentDataAsync(DTEventArgs value)
        {
            ModelUploadCSV = new();

            UploadModel Upload = new()
            {
                Name = value.Params[0].ToString(),
                Url = value.Params[1].ToString(),
                TempImgName = value.Params[2].ToString(),
                AttachmentTypeId = (decimal)value.Params[3]
            };

            if (ModelId != 99)
            {
                Upload.Id = ModelId + 1;
            }
            else
            {
                Upload.Id = ModelId;
            }

            if (Upload.AttachmentTypeId == 0)
            {
                string[] FileType = new[] { ".xlsx" };
                bool pdfPass = await CheckPDFFileAsync(Upload.Name!, FileType);
                if (pdfPass)
                {
                    ModelUploadCSV.Id = Upload.Id;
                    ModelUploadCSV.Name = Upload.Name;
                    ModelUploadCSV.Url = Upload.Url;
                    ModelUploadCSV.TempImgName = Upload.TempImgName;
                    ModelUploadCSV.AttachmentTypeId = Upload.AttachmentTypeId;
                }
            }
            await SetChildData.InvokeAsync(ModelUploadCSV);
        }

        private async Task<bool> CheckPDFFileAsync(string fileName, string[] FileType)
        {
            bool pass = true;
            string extension;
            extension = Path.GetExtension(fileName);
            if (!FileType.Contains(extension))
            {
                string alert = $"ทางระบบสามารถรับได้เฉพาะไฟล์สกุล {FileType} เท่านั้น";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                pass = false;
            }
            return pass;
        }
    }
}
