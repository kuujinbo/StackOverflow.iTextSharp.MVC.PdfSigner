using iTextSharp.text;

namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Services
{
    public class FormFieldLocation
    {
        public int Page { get; set; }
        public Rectangle Rectangle { get; set; }
    }
}