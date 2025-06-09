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
    public partial class UploadTypePDF
    {
        #region Parameter
        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public string Detail { get; set; } = string.Empty;
        [Parameter] public string GenId { get; set; } = string.Empty;
        [Parameter] public string myFile { get; set; } = string.Empty;
        [Parameter] public decimal AttTypeId { get; set; } = 0;
        [Parameter] public int ModelId { get; set; } = 99;
        [Parameter] public EventCallback<UploadModel> SetChildData { get; set; }

        #endregion

        private UploadModel ModelUploadPDF { get; set; } = new();

        protected override void OnInitialized()
        {
            ModelUploadPDF = new();
        }

        public async Task SetCurrentDataAsync(DTEventArgs value)
        {
            ModelUploadPDF = new();
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
                bool pdfPass = await CheckPDFFileAsync(Upload.Name!);
                if (pdfPass)
                {
                    ModelUploadPDF.Id = Upload.Id;
                    ModelUploadPDF.Name = Upload.Name;
                    ModelUploadPDF.Url = Upload.Url;
                    ModelUploadPDF.TempImgName = Upload.TempImgName;
                    ModelUploadPDF.AttachmentTypeId = Upload.AttachmentTypeId;
                }
            }
            await SetChildData.InvokeAsync(ModelUploadPDF);
        }

        private async Task<bool> CheckPDFFileAsync(string fileName)
        {
            bool pass = true;
            string extension;
            extension = Path.GetExtension(fileName);
            if (extension != ".pdf")
            {
                string alert = $"ทางระบบสามารถรับได้เฉพาะไฟล์สกุล .pdf เท่านั้น";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
                pass = false;
            }
            return pass;
        }
    }
}
