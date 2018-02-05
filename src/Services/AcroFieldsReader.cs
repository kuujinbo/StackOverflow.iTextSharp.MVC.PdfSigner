/*
 * ------------------------------------------------------------------------
 * Work with PDF forms and iTextsharp AcroFields class
 * ------------------------------------------------------------------------
*/
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace kuujinbo.StackOverflow.iTextSharp.MVC.PdfSigner.Services
{
    public class AcroFieldsReader
    {
        public AcroFields AcroFields { get; private set; }

        public AcroFieldsReader(AcroFields acroFields) 
        {
            AcroFields = acroFields;
        }
        
        /// <summary>
        /// Determine whether a named form field exists on the PDF
        /// </summary>
        /// <param name="name">PDF form field name</param>
        /// <returns></returns>
        public bool HasField(string name)
        {
            return AcroFields.Fields.ContainsKey(name);
        }

        /// <summary>
        /// Determine if the PDF has the named signature field
        /// </summary>
        /// <param name="name">PDF form field name</param>
        /// <returns></returns>
        public bool HasSignatureField(string name)
        {
            return AcroFields.DoesSignatureFieldExist(name);
        }

        /// <summary>
        /// Get the number of signature fields from the PDF
        /// </summary>
        /// <returns></returns>
        public int SignedFields()
        {
            return AcroFields.GetSignatureNames().Count; 
        }

        /// <summary>
        /// Get PDF form field position information by name.
        /// </summary>
        /// <param name="name">PDF form field name</param>
        /// <returns>
        /// PDF form field page number and postion (iText) Rectangle, or
        /// null if the named field does not exist.
        /// </returns>
        public FormFieldLocation FieldLocationByName(string name)
        {
            var p = AcroFields.GetFieldPositions(name)[0];
            return p != null
                ? new FormFieldLocation {
                    Page = p.page,
                    Rectangle = new Rectangle(
                        p.position.Left, p.position.Bottom,
                        p.position.Right, p.position.Top)
                  }
                : null;
        }

    }
}