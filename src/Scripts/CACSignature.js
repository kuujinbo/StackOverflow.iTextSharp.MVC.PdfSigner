/* CAPICOM reference
https://msdn.microsoft.com/en-us/library/windows/desktop/aa375732(v=vs.85).aspx
*/
var pdfSigner = function () {
    return {
        store: null
        , certificates: null
        , errorObject: null
        , canSign: true
        , CAPICOM_STORE_OPEN_READ_ONLY: 0
        , CAPICOM_CURRENT_USER_STORE: 2
        , CAPICOM_CERTIFICATE_FIND_KEY_USAGE: 12
        , CAPICOM_DIGITAL_SIGNATURE_KEY_USAGE: 0x00000080
        , CAPICOM_VERIFY_SIGNATURE_ONLY: 0
        , CAPICOM_ENCODE_BASE64: 0
        , CAPICOM_E_CANCELLED: -2138568446
        , signText: function (base64String) {
            if (window.event) {
                window.event.cancelBubble = true;
            }
            try {
                var signedData = new ActiveXObject("CAPICOM.SignedData");
                var utilities = new ActiveXObject("CAPICOM.Utilities");
                signedData.Content = utilities.Base64Decode(base64String);
                var signer = this.getSigner();
                if (!this.canSign) { return ""; }
                /*
                https://msdn.microsoft.com/en-us/library/windows/desktop/aa387726(v=vs.85).aspx
                    -- The Sign method creates a digital signature on the content to be signed. 
                       A digital signature consists of a hash of the content to be signed that 
                       is encrypted by using the private key of the signer. 
                
                    -- last parameter is for signed data **OUTPUT**
                */
                var signedMessage = signedData.Sign(signer, true, this.CAPICOM_ENCODE_BASE64);
                signedData.Verify(signedMessage, true, this.CAPICOM_VERIFY_SIGNATURE_ONLY);

                return signedMessage;
            } catch (e) {
                if (e.number != this.CAPICOM_E_CANCELLED) {
                    this.errorObject = e;
                }
            }
            return "";
        }

        , getSigner: function () {
            try {
                this.store = new ActiveXObject("CAPICOM.Store");
                this.store.Open(
                    this.CAPICOM_CURRENT_USER_STORE
                    , "My"
                    , this.CAPICOM_STORE_OPEN_READ_ONLY
                );
                this.certificates = this.store.Certificates.Find(
                    this.CAPICOM_CERTIFICATE_FIND_KEY_USAGE
                    , this.CAPICOM_DIGITAL_SIGNATURE_KEY_USAGE
                    , true
                );
                var signer = new ActiveXObject("CAPICOM.Signer");
                signer.Certificate = this.certificates.Item(1);
                this.canSign = true;

                return signer;
            } catch (e) {
                this.canSign = false;
                this.errorObject = e;
            }
            finally {
                this.store = null;
                this.certificates = null;
            }
        }

    };
}();