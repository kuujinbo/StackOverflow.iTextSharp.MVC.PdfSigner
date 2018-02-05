/*
 * ------------------------------------------------------------------------
 * Web context client-side digital signing. Implementation specific, and
 * **REQUIRES** the following:
 *      -- Internet Explorer web browser (tested IE >= version 10)
 *      -- CAPICOM is installed on client machines. CAPICOM support was 
 *         officially abandoned with the release of Windows 7, but
 *         is still available in many standard corporate desktop builds.
 * 
 * Background reference here:
 * http://stackoverflow.com/questions/28949243
 * ------------------------------------------------------------------------
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Services
{
    public class WebSigner : ISigner, IWebSigner
    {
        /// <summary>
        /// Byte array buffer size used throughout class
        /// </summary>
        const int EXCLUSION_BUFFER = 8192;

        /// <summary>
        /// Named key used to retrieve first instance created by class, which
        /// stores state per-user required by iTextSharp to sign PDF.
        /// </summary>
        /// <see cref="_memoryStream"/>
        /// <see cref="_signatureAppearance"/>
        private static readonly string InstanceLookupKey = typeof(WebSigner).ToString();

        /// <summary>
        /// Stream object required by iTextSharp to sign PDF.
        /// </summary>
        private MemoryStream _memoryStream;

        /// <summary>
        /// Used to create external signature - for both preparing the PDF,
        /// and also for signing the PDF
        /// </summary>
        /// <seealso cref="PreSign(byte[])"/>
        /// <seealso cref="Sign(string)"/>
        private PdfSignatureAppearance _signatureAppearance;

        /// <summary>
        /// Reads PDF form fields during pre-signing stages.
        /// </summary>
        private AcroFieldsReader _acroFieldsWorker;

        /// <summary>
        /// Apply the client signature to this PDF signature form field.
        /// </summary>
        public string SignatureFieldName { get; set; }

        public Rectangle SignatureBox { get; private set; }
        public void SetSignatureBox(float llx, float lly, float urx, float ury)
        {
            SignatureBox = new Rectangle(llx, lly, urx, ury) 
            { 
                BorderColor = BaseColor.BLACK, Border = Rectangle.BOX, BorderWidth = 1
            };

        }

        public long DataSize { get; private set; }

        /// <summary>
        /// Number if times the PDF file is read to fix CAPICOM's broken 
        /// signing implementation. CAPICOM expects a utf16 string (.NET 
        /// Encoding.Unicode) as input to create a digital signature.
        /// Then it either pads or truncates (depending on what online 
        /// source you find) whatever data it receives if the length is an 
        /// **ODD** number, which invalidates the signed hash, and results
        /// in the infamous "Document has been altered or corrupted since
        /// it was signed" invalid signature message when opening the PDF.
        /// </summary>
        /// <seealso cref="Reason"/>
        public int DataReadCount { get; private set; }

        /// <summary>
        /// This field is padded when needed to always make the presigned
        /// PDF content length passed to the client an even number.
        /// </summary>
        /// <seealso cref="DataReadCount"/>
        /// <seealso cref="Sign(string)"/>
        public string Reason { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WebSigner()
        {
            Reason = "";
        }

        /// <summary>
        /// Call **after** pre-signing, but **before** signing PDF. 
        /// </summary>
        /// <returns>
        /// Existing instance that was used to presign PDF for client-side
        /// signature. That instance is cached to access the backing Stream
        /// iTextSharp uses to sign the PDF.
        /// </returns>
        /// <seealso cref="Sign(string)"/>
        public static WebSigner GetCachedInstance()
        {
            var w = HttpContext.Current.Session[InstanceLookupKey] as WebSigner;
            if (w != null) return w;
            throw new InvalidOperationException("WebSigner");
        }

        /// <summary>
        /// Initialize the PDF signature field.
        /// </summary>
        private void InitSignatureField(PdfStamper stamper)
        {
            if (_acroFieldsWorker.HasSignatureField(SignatureFieldName))
            {
                _signatureAppearance.SetVisibleSignature(SignatureFieldName);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(SignatureFieldName)
                    && _acroFieldsWorker.HasField(SignatureFieldName))
                {
                    var textField = _acroFieldsWorker.FieldLocationByName(SignatureFieldName);
                    _signatureAppearance.SetVisibleSignature(
                        textField.Rectangle, textField.Page,
                        _signatureAppearance.GetNewSigName()
                    );
                    stamper.FormFlattening = true;
                    stamper.PartialFormFlattening(SignatureFieldName);
                }
                else if (SignatureBox != null)
                {
                    _signatureAppearance.SetVisibleSignature(
                        SignatureBox,
                        // reader.NumberOfPages,
                        1,
                        _signatureAppearance.GetNewSigName()
                    );
                    stamper.FormFlattening = true;
                    // stamper.PartialFormFlattening(SignatureFieldName);
                }
                else
                {
                    throw new InvalidOperationException("field does not exist");
                }
            }
        }

        /// <summary>
        /// See PreSign(byte[] pdfIn)
        /// </summary>
        /// <param name="pdfPath">Full path to PDF file</param>
        /// <returns>
        /// Base64 encoded PDF content bytes client will sign.
        /// </returns>
        public string PreSign(string pdfPath)
        {
            return PreSign(File.ReadAllBytes(pdfPath));
        }
        /// <summary>
        /// Prepare the data needed for digital signature. Unfortunately
        /// CAPICOM's client-side implementation both hashes **AND** signs
        /// passed in data instead of signing data already hashed, so the 
        /// **entire** PDF content bytes are needed.
        /// </summary>
        /// <param name="pdfIn">PDF file contents</param>
        /// <returns>
        /// Base64 encoded PDF content bytes client will sign.
        /// </returns>
        public string PreSign(byte[] pdfIn)
        {
            byte[] pdfRawContent = null;
            bool isOdd = true;
            var timeStamp = DateTime.Now;
            var pdfSignature = new PdfSignature(
                PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED
            );
            pdfSignature.Date = new PdfDate(timeStamp);
            var exclusionSizes = new Dictionary<PdfName, int>();
            exclusionSizes.Add(PdfName.CONTENTS, EXCLUSION_BUFFER * 2 + 2);
            PdfReader reader = null;
            int? signedFields = null;
            try
            {
                var cert = new WebCertificateReader().GetSigningCertificate();
                do
                {
                    ++DataReadCount;
                    reader = new PdfReader(pdfIn);
                    _acroFieldsWorker = new AcroFieldsReader(reader.AcroFields);
                    signedFields = signedFields ?? _acroFieldsWorker.SignedFields();
                    _memoryStream = new MemoryStream();
                    var stamper = signedFields == 0
                        ? PdfStamper.CreateSignature(reader, _memoryStream, '\0')
                        : PdfStamper.CreateSignature(reader, _memoryStream, '\0', null, true)
                    ;
                    _signatureAppearance = stamper.SignatureAppearance;
                    InitSignatureField(stamper);
                    pdfSignature.Reason = Reason;
                    _signatureAppearance.Certificate = cert;
                    _signatureAppearance.SignDate = timeStamp;
                    _signatureAppearance.CryptoDictionary = pdfSignature;
                    _signatureAppearance.PreClose(exclusionSizes);
                    using (Stream sapStream = _signatureAppearance.GetRangeStream())
                    {
                        using (var ms = new MemoryStream())
                        {
                            sapStream.CopyTo(ms);
                            pdfRawContent = ms.ToArray();
                        }

                        // pdfRawContent = StreamHandler.ReadAllBytes(sapStream);
                        // fix CAPICOM's broken implemetation: signature
                        // invalid if sapStream.Length is **ODD**
                        if ((pdfRawContent.Length % 2) == 0)
                        {
                            isOdd = false;
                        }
                        else
                        {
                            // Reason += '\0';
                            Reason += " ";
                        }
                        DataSize = sapStream.Length;
                    }
                    // sanity check
                    if (DataReadCount > 200) throw new InvalidOperationException("DataReadCount");
                } while (isOdd);
            }
            catch { throw; }
            finally
            {
                HttpContext.Current.Session[InstanceLookupKey] = this;
                if (reader != null) { reader.Dispose(); }
            }
            return Convert.ToBase64String(pdfRawContent);
        }

        /// <summary>
        /// Sign the PDF content
        /// </summary>
        /// <param name="signedValue"></param>
        /// <returns>Signed PDF document byte array</returns>
        public byte[] Sign(string signedValue)
        {
            try
            {
                byte[] encodedSignature = Convert.FromBase64String(signedValue);
                byte[] paddedSignature = new byte[EXCLUSION_BUFFER];
                Array.Copy(encodedSignature, 0, paddedSignature, 0, encodedSignature.Length);
                var pdfDictionary = new PdfDictionary();
                pdfDictionary.Put(
                    PdfName.CONTENTS,
                    new PdfString(paddedSignature).SetHexWriting(true)
                );
                _signatureAppearance.Close(pdfDictionary);
                return _memoryStream.ToArray();
            }
            catch (FormatException fe)
            {
                throw new InvalidDataException("invalid signature format");
            }
            catch { throw; }
            finally
            {
                _memoryStream.Dispose();
                HttpContext.Current.Session.Remove(InstanceLookupKey);
            }
        }
    }
}