namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Services
{
    public interface IWebSigner
    {
        string PreSign(string pdfPath);
        string PreSign(byte[] pdfIn);
    }
}