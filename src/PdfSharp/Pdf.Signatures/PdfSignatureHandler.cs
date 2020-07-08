using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.Annotations;
using PdfSharp.X509;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static PdfSharp.Pdf.AcroForms.PdfAcroField;

namespace PdfSharp.Signatures
{
    /// <summary>
    /// int数据事件
    /// </summary>
    public class IntEventArgs : EventArgs {
        /// <summary>
        /// int 值
        /// </summary>
        public int Value { get; set; } }

    /// <summary>
    /// Handles the signature
    /// </summary>
    public class PdfSignatureHandler
    {

        private PositionTracker contentsTraker;
        private PositionTracker rangeTracker;
        private int? maximumSignatureLength;
        private const int byteRangePaddingLength = 36; // the place big enough required to replace [0 0 0 0] with the correct value
        /// <summary>
        /// 计算签名大小
        /// </summary>
        public event EventHandler<IntEventArgs> SignatureSizeComputed = (s, e) => { };
        /// <summary>
        /// pdfdocument
        /// </summary>
        public PdfDocument Document { get; private set; }
        /// <summary>
        /// 证书
        /// </summary>
        public X509Certificate2 Certificate { get; private set; }
        /// <summary>
        /// 选项
        /// </summary>
        public PdfSignatureOptions Options { get; private set; }

        /// <summary>
        /// 附件到文档
        /// </summary>
        /// <param name="documentToSign"></param>
        public void AttachToDocument(PdfDocument documentToSign)
        {
            this.Document = documentToSign;
            this.Document.BeforeSave += AddSignatureComponents;
            this.Document.AfterSave += ComputeSignatureAndRange;

            if (!maximumSignatureLength.HasValue)
            {
                //maximumSignatureLength = 10 * 1024;
                maximumSignatureLength = GetSignature(new byte[] { 0 }).Length;
                SignatureSizeComputed(this, new IntEventArgs() { Value = maximumSignatureLength.Value });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="signatureMaximumLength"></param>
        /// <param name="options"></param>
        public PdfSignatureHandler(X509Certificate2 certificate, int? signatureMaximumLength, PdfSignatureOptions options)
        {
            this.Certificate = certificate;

            this.maximumSignatureLength = signatureMaximumLength;
            this.Options = options;
        }

        private void ComputeSignatureAndRange(object sender, PdfDocumentEventArgs e)
        {
            var writer = e.Writer;
            writer.Stream.Position = rangeTracker.Start;
            var rangeArray = new PdfArray(new PdfInteger(0),
                new PdfInteger(contentsTraker.Start),
                new PdfInteger(contentsTraker.End),
                new PdfInteger((int)writer.Stream.Length - contentsTraker.End));
            rangeArray.Write(writer);

            var rangeToSign = GetRangeToSign(writer.Stream);
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            var signature = GetSignature(rangeToSign);
            if (signature.Length > maximumSignatureLength)
                throw new Exception("The signature length is bigger that the approximation made.");
#if DEBUG
            sw.Stop();
            Console.WriteLine("GetSignature usetime:" + sw.ElapsedMilliseconds);
#endif
            var hexFormated = Encoding.UTF8.GetBytes(FormatHex(signature));

            writer.Stream.Position = contentsTraker.Start + 1;
            writer.Write(hexFormated);
        }


        private byte[] GetSignature(byte[] range)
        {
            var contentInfo = new ContentInfo(range);

            SignedCmsX signedCms = new SignedCmsX(contentInfo, true);
            CmsSigner signer = new CmsSigner(Certificate);
            signer.UnsignedAttributes.Add(new Pkcs9SigningTime());

            signedCms.ComputeSignature(signer, true);
            var bytes = signedCms.Encode();

            return bytes;
        }

        string FormatHex(byte[] bytes)
        {
            var retval = new StringBuilder();

            for (int idx = 0; idx < bytes.Length; idx++)
                retval.AppendFormat("{0:x2}", bytes[idx]);

            return retval.ToString();
        }

        private byte[] GetRangeToSign(Stream stream)
        {
            stream.Position = 0;

            var size = contentsTraker.Start + stream.Length - contentsTraker.End;
            var signRange = new byte[size];

            for (int i = 0; i < signRange.Length; i++)
            {
                if (i == contentsTraker.Start)
                    stream.Position = contentsTraker.End;
                signRange[i] = (byte)stream.ReadByte();

            }

            return signRange;
        }

        private void AddSignatureComponents(object sender, EventArgs e)
        {
            var catalog = Document.Catalog;

            if (catalog.AcroForm == null)
                catalog.AcroForm = new PdfAcroForm(Document);

            catalog.AcroForm.Elements.Add(PdfAcroForm.Keys.SigFlags, new PdfInteger(3));
            // catalog.AcroForm.Elements.Add(PdfAcroForm.Keys.SigFlags, new PdfImage(Document,Drawing.XImage.FromFile("")));
            var signature = new PdfSignatureField(Document);
            Console.WriteLine("maximumSignatureLength = " + maximumSignatureLength);
            var paddedContents = //new PdfString("", PdfStringFlags.HexLiteral, 1503);
                new PdfString("", PdfStringFlags.HexLiteral, maximumSignatureLength.Value);
            var paddedRange = new PdfArray(Document, byteRangePaddingLength, new PdfInteger(0), new PdfInteger(0), new PdfInteger(0), new PdfInteger(0));

            signature.Contents = paddedContents;
            signature.Contact = Options.ContactInfo;
            signature.ByteRange = paddedRange;
            signature.Reason = Options.Reason;
            signature.Location = Options.Location;
            signature.Rect = new PdfRectangle(Options.Rectangle);
            signature.AppearanceHandler = Options.AppearanceHandler ?? new DefaultAppearanceHandler()
            {
                Location = Options.Location,
                Contact=Options.ContactInfo,
                Reason = Options.Reason,
                Signer = Certificate.GetNameInfo(X509NameType.SimpleName, false),
                Image = Options.Image,
                Model = Options.Model
            };
            signature.PrepareForSave();

            this.contentsTraker = new PositionTracker(paddedContents);
            this.rangeTracker = new PositionTracker(paddedRange);
            int signPage = Document.Pages.Count - 1;
            if (!Document.Pages[signPage].Elements.ContainsKey(PdfPage.Keys.Annots))
                Document.Pages[signPage].Elements.Add(PdfPage.Keys.Annots, new PdfArray(Document));

            (Document.Pages[signPage].Elements[PdfPage.Keys.Annots] as PdfArray).Elements.Add(signature);

            catalog.AcroForm.Fields.Elements.Add(signature);

        }
    }
}
