using HiQPdf;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.PdfPreview
{
    public partial class PreviewPdf
    {
        [Parameter] public string Url { get; set; } = null!;
        [Parameter] public string Height { get; set; } = "1200px";
        [Parameter] public PdfDocumentMargins? documentMargins { get; set; } = null;

        public string DocumentPath { get; set; } = null!;

        protected override void OnInitialized()
        {
            byte[] result;

            if (documentMargins != null)
            {
                result = GeneratePDFService.GeneratePDF(Url, documentMargins.Left, documentMargins.Right, documentMargins.Top, documentMargins.Bottom);
            }
            else
            {
                result = GeneratePDFService.GeneratePDF(Url);
            }

            DocumentPath = "data:application/pdf;base64," + Convert.ToBase64String(result);
        }
    }
}
