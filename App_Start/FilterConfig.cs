using System.Web;
using System.Web.Mvc;

namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
