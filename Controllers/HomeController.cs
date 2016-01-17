using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Mvc;
using kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Services;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Controllers
{
    public class HomeController : Controller
    {
        static DateTime _now;
        public ActionResult Index()
        {
            WebSigner ws = new WebSigner();
            ws.SetSignatureBox(50, 670, 280, 760);
            return View(model:GetPreSignedTestReader());
        }

        [HttpPost]
        public ActionResult Index(string signedValue)
        {
            WebSigner ws = WebSigner.GetCachedInstance();
            return File(
                ws.Sign(signedValue), "application/pdf",
                string.Format("PdfSigner-{0:yyyy-MM-dd_HH.mm.ss}.pdf", _now)
            );
        }

        private string GetPreSignedTestReader()
        {
            string paragraphText = "ASP.NET MVC PDF signing test @";
            _now = DateTime.Now;
            using (MemoryStream ms = new MemoryStream())
            {
                using (Document document = new Document())
                {
                    PdfWriter.GetInstance(document, ms);
                    document.Open();
                    document.Add(new Paragraph(
                        string.Format("{0}{1}", paragraphText, _now)
                    ));
                }

                WebSigner ws = new WebSigner();
                ws.SetSignatureBox(50, 670, 280, 760);
                return ws.PreSign(ms.ToArray());
            }
        }


    }
}