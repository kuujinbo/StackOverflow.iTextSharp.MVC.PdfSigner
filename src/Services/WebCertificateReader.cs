/*
 * ------------------------------------------------------------------------
 * Access public key certificates in a web context to provide support
 * for PDF digital signatures.
 * ------------------------------------------------------------------------
*/
using System.Web;
using System.Security.Cryptography.X509Certificates;
using BouncyCastle = Org.BouncyCastle.X509;

namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Services
{
    public class WebCertificateReader
    {
        /// <summary>
        /// Retrieve user certificate from current web request to prepare
        /// digital PDF signature. 
        /// </summary>
        /// <returns>User's public X.509 certificate</returns>
        /// <remarks>
        /// Adobe Acrobat & Reader get the Online Certificate Status Protocol
        /// (OCSP) revocation status for us:
        /// https://wikidocs.adobe.com/wiki/pages/viewpage.action?pageId=69567422
        /// </remarks>
        private X509Certificate2 GetSignerCertificate()
        {
            return new X509Certificate2(
                HttpContext.Current.Request.ClientCertificate.Certificate
            );
        }

        /// <summary>
        /// Get the BouncyCastle X509Certificate needed by the iText 
        /// PdfSignatureAppearance.
        /// </summary>
        /// <returns>BouncyCastle X509Certificate</returns>
        public BouncyCastle.X509Certificate GetSigningCertificate()
        {
            X509Certificate2 cert = GetSignerCertificate();
            return new BouncyCastle.X509CertificateParser()
                .ReadCertificate(cert.RawData);
        }
    }
}