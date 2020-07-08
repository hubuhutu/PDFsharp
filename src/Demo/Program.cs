using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Signatures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string dirPath = @"D:\Data\文通\picture\20200116145413";
                var files = Directory.GetFiles(dirPath, "*.jpg");
                XFont wmFont = new XFont("Times New Roman", 150, XFontStyle.BoldItalic);

                using (PdfDocument doc = new PdfDocument())
                {
                    for (int i = 0; i < files.Length && i < 3; i++)
                    {
                        var page = doc.Pages.Add();
                        page.Size = PdfSharp.PageSize.A4;
                        using (XImage img = XImage.FromFile(files[i]))
                        using (var gfx = XGraphics.FromPdfPage(page))
                        {

                            XRect srcRect = new XRect(0, 0, img.PixelWidth, img.PixelHeight);
                            double scale = 1f;
                            if (img.PixelWidth > gfx.PageSize.Width || img.PixelHeight > gfx.PageSize.Height)
                            {
                                scale = gfx.PageSize.Width / img.PixelWidth;
                                double scale2 = gfx.PageSize.Height / img.PixelHeight;
                                if (scale > scale2)
                                    scale = scale2;
                            }
                            XRect dstRect = new XRect(0, 0, img.PixelWidth * scale, img.PixelHeight * scale);
                            gfx.DrawImage(img, dstRect, srcRect, XGraphicsUnit.Point);
                        }
                        AddWatermark(page, "Watermark", wmFont, i);
                    }
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    try
                    {
                        PdfSign(doc);
                    }
                    catch (Exception ex)
                    {

                    }
                    sw.Stop();
                    Console.WriteLine("PdfSign usetime:" + sw.ElapsedMilliseconds);
                    doc.Save("temp.pdf");
                }
                Process.Start("temp.pdf");
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        private static void PdfSign(PdfDocument document)
        {
            PdfSignatureOptions signOptions = new PdfSharp.Signatures.PdfSignatureOptions()
            {
                Rectangle = new XRect(30, 50, 300, 50),
                ContactInfo = "联系人",
                Location = "北京市",
                Reason = "其他",
                Model = SignRenderingMode.Image_And_Text,
                Image = XImage.FromFile(@"E:\Data\水印\logo.png")
            };
            var cert = SelectCertificate();

            PdfSignatureHandler handler = new PdfSignatureHandler(cert, null, signOptions);
            handler.AttachToDocument(document);
        }
        /// <summary>
        /// 获取签名证书
        /// </summary>
        /// <param name="sn">证书sn码</param>
        /// <returns></returns>
        private static X509Certificate2 SelectCertificate()
        {
            FrmCertificateDialog frm = new FrmCertificateDialog();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return frm.SelectedCert;
            }

            return null;
        }

        /// <summary>
        /// 获取签名证书
        /// </summary>
        /// <param name="sn">证书sn码</param>
        /// <returns></returns>
        private static X509Certificate2 GetCertificate(string sn)
        {
            if (string.IsNullOrEmpty(sn))
                return null;
            X509Store store = new X509Store(StoreName.My);
            store.Open(OpenFlags.ReadOnly);
            foreach (var item in store.Certificates)
            {
                if (item.SerialNumber.ToLower() == sn)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// 添加水印
        /// </summary>
        /// <param name="page"></param>
        /// <param name="watermark"></param>
        /// <param name="font"></param>
        private static void AddWatermark(PdfPage page, string watermark, XFont font, int typeId = 0)
        {
            int type = typeId % 3;
            using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
            {
                switch (type)
                {
                    case 0:
                        {
                            // Get the size (in points) of the text.
                            var size = gfx.MeasureString(watermark, font);

                            // Define a rotation transformation at the center of the page.
                            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                            double angle = -Math.Atan(page.Height / page.Width) * 180 / Math.PI;
                            Console.WriteLine(angle);
                            gfx.RotateTransform(30.0);
                            gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

                            // Create a string format.
                            var format = new XStringFormat();
                            format.Alignment = XStringAlignment.Near;
                            format.LineAlignment = XLineAlignment.Near;

                            // Create a dimmed red brush.
                            XBrush brush = new XSolidBrush(XColor.FromArgb(50, 255, 0, 0));

                            // Draw the string.
                            gfx.DrawString(watermark, font, brush,
                                new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                                format);
                        }
                        break;

                    case 1:
                        {
                            // Get the size (in points) of the text.
                            var size = gfx.MeasureString(watermark, font);

                            // Define a rotation transformation at the center of the page.
                            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                            gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
                            gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

                            // Create a graphical path.
                            var path = new XGraphicsPath();

                            // Create a string format.
                            var format = new XStringFormat();
                            format.Alignment = XStringAlignment.Near;
                            format.LineAlignment = XLineAlignment.Near;

                            // Add the text to the path.
                            // AddString is not implemented in PDFsharp Core.
                            path.AddString(watermark, font.FontFamily, XFontStyle.BoldItalic, 150,
                            new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                                format);

                            // Create a dimmed red pen.
                            var pen = new XPen(XColor.FromArgb(128, 255, 0, 0), 2);

                            // Stroke the outline of the path.
                            gfx.DrawPath(pen, path);
                        }
                        break;

                    case 2:
                        {
                            // Get the size (in points) of the text.
                            var size = gfx.MeasureString(watermark, font);

                            // Define a rotation transformation at the center of the page.
                            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                            gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
                            gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

                            // Create a graphical path.
                            var path = new XGraphicsPath();

                            // Create a string format.
                            var format = new XStringFormat();
                            format.Alignment = XStringAlignment.Near;
                            format.LineAlignment = XLineAlignment.Near;

                            // Add the text to the path.
                            // AddString is not implemented in PDFsharp Core.
                            path.AddString(watermark, font.FontFamily, XFontStyle.BoldItalic, 150,
                                new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                                format);

                            // Create a dimmed red pen and brush.
                            var pen = new XPen(XColor.FromArgb(50, 75, 0, 130), 3);
                            XBrush brush = new XSolidBrush(XColor.FromArgb(50, 106, 90, 205));

                            // Stroke the outline of the path.
                            gfx.DrawPath(pen, brush, path);
                        }
                        break;
                }
                gfx.Dispose();
            }
        }
    }
}
