using System;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;

namespace PdfSharp.Signatures
{
    internal class DefaultAppearanceHandler : ISignatureAppearanceHandler
    {
        /// <summary>
        /// 联系人
        /// </summary>
        public string Contact { get; set; }
        /// <summary>
        /// 位置
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// 原因
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// 签发者
        /// </summary>
        public string Signer { get; set; }

        public XImage Image { get; set; }
        public SignRenderingMode Model { get; set; }


        public void DrawAppearance(XGraphics gfx, XRect rect)
        {
            if (Image == null || Model == SignRenderingMode.Text)
            {
                DrawText(gfx, rect);
            }
            else if (Model == SignRenderingMode.Image && Image != null)
            {
                DrawImage(gfx, rect);
            }
            else
            {
                double imgWidth = Image.PixelWidth * rect.Height / Image.PixelHeight;
                XRect rectImg = new XRect(rect.X, rect.Y, imgWidth, rect.Height);
                XRect rectTxt = new XRect(rectImg.Width, 0, rect.Width - imgWidth, rect.Height);
                DrawImage(gfx, rectImg);

                DrawText(gfx, rectTxt);
            }
        }
        /// <summary>
        /// 绘制文本
        /// </summary>
        /// <param name="gfx"></param>
        /// <param name="rect"></param>
        private void DrawText(XGraphics gfx, XRect rect)
        {
            var backColor = XColor.Empty;
            var defaultText = string.Format("Signed by: {0}\nLocation: {1}\nReason: {2}\nContact: {3}\nDate: {4}", Signer, Location, Reason, Contact, DateTime.Now);

            // XFont font = new XFont("Verdana", 7, XFontStyle.Regular);
            XFont font = new XFont("黑体", 7, XFontStyle.Regular);

            XTextFormatter txtFormat = new XTextFormatter(gfx);

            var currentPosition = new XPoint(rect.X, rect.Y);
            if (Model == SignRenderingMode.Text)
                currentPosition = new XPoint(0, 0);

            txtFormat.DrawString(defaultText,
                font,
                new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black)),
                new XRect(currentPosition.X, currentPosition.Y, rect.Width - currentPosition.X, rect.Height),
                XStringFormats.TopLeft);
        }

        private void DrawImage(XGraphics gfx, XRect rect)
        {
            if (Image == null)
                return;

            double imgWidth = Image.PixelWidth;
            double imgHeight = Image.PixelHeight;
            XRect srcRect = new XRect(0, 0, imgWidth, imgHeight);
            double scale = 1f;

            scale = rect.Width / imgWidth;
            double scale2 = rect.Height / imgHeight;
            if (scale > scale2)
                scale = scale2;

            XRect dstRect = new XRect(0, 0, imgWidth * scale, imgHeight * scale);
            gfx.DrawImage(Image, dstRect, srcRect, XGraphicsUnit.Point);
        }
    }
}
