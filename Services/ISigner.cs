namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Services
{
    public interface ISigner
    {
        byte[] Sign(string signedValue);
    }
}