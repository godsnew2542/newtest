using HiQPdf;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.IServices;
using LoanApp.Model.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace LoanApp.Services
{
    public class GeneratePdfService : IGeneratePdfService
    {
        private IOptions<AppSettings> AppSettings { get; set; }
        private IJSRuntime JS { get; set; }
        private NavigationManager NavigationManager { get; set; }
        private ModelContext Context { get; set; }

        public GeneratePdfService(IOptions<AppSettings> _AppSettings, IJSRuntime _JS, NavigationManager navigationManager, ModelContext _context)
        {
            AppSettings = _AppSettings;
            JS = _JS;
            NavigationManager = navigationManager;
            Context = _context;
        }

        public byte[] GeneratePDF(string htmlString, float leftMargin = 5, float rightMargin = 25, float topMargin = 5, float bottomMargin = 25)
        {
            // create an empty PDF document
            PdfDocument document = new PdfDocument();
            document.SerialNumber = AppSettings.Value.HiQSerialNumber;

            // add a page to document
            /*PdfPage page1 = document.AddPage(PdfPageSize.A4, new PdfDocumentMargins(85.0394f, 56.6929f, 70.8661f, 56.6929f),*/
            //PdfPage page1 = document.AddPage(PdfPageSize.A4, new PdfDocumentMargins(5, 25, 5, 25),
            PdfPage page1 = document.AddPage(PdfPageSize.A4, new PdfDocumentMargins(leftMargin, rightMargin, topMargin, bottomMargin),
            PdfPageOrientation.Portrait);

            // an object to be set with HTML layout info after conversion
            PdfLayoutInfo? htmlLayoutInfo = null;

            byte[] pdfBuffer = new byte[1];
            try
            {
                //string baseUrl = $"{_env}/wwwroot"
                //string baseUrl = $"https://localhost:44381"
                string baseUrl = NavigationManager.BaseUri;

                // create the HTML object from URL or HTML code
                PdfHtml htmlObject = new(htmlString, baseUrl);
                htmlObject.BrowserWidth = 794; //21cm

                // layout the HTML object in PDF
                htmlLayoutInfo = page1.Layout(htmlObject);
                pdfBuffer = document.WriteToMemory();
            }
            finally
            {
                document.Close();
            }
            return pdfBuffer;
        }

        public async Task SaveFilePDFAsync(byte[] pdfBuffer, string fileName)
        {
            try
            {
                if (pdfBuffer != default)
                {
                    await JS.InvokeVoidAsync("jsSaveAsFile", fileName, Convert.ToBase64String(pdfBuffer));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task OpenTypeFileAsync(string GenId, string MyFile)
        {
            await JS.InvokeVoidAsync("openTypeFile", GenId, MyFile);
        }

        public string DownloadPdf(decimal reqID)
        {
            var path = string.Empty;
            try
            {
                if (reqID != 0)
                {
                    LoanRequest? loanReq = Context.LoanRequests
                        .Where(c => c.LoanRequestId == reqID)
                        .FirstOrDefault();
                    if (loanReq != null)
                    {
                        var ConAttachment = Context.ContractAttachments
                            .Where(c => c.AttachmentId == loanReq.LoanAttachmentId)
                            .FirstOrDefault();

                        if (ConAttachment != null)
                        {
                            path = $"{AppSettings.Value.RequestFilePath}\\{ConAttachment.AttachmentAddr}";
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return path;
        }
    }
}
