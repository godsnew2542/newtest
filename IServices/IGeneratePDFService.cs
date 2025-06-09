namespace LoanApp.IServices
{
    interface IGeneratePdfService
    {
        public byte[] GeneratePDF(string htmlString, float leftMargin = 5, float rightMargin = 25, float topMargin = 5, float bottomMargin = 25);
        public Task SaveFilePDFAsync(byte[] pdfBuffer, string fileName);
        public Task OpenTypeFileAsync(string GenId, string MyFile);
        public string DownloadPdf(decimal reqID);
    }
}
