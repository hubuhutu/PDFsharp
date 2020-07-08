using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharp.Signatures
{
    public class PdfSignatureOptions
    {
        public ISignatureAppearanceHandler AppearanceHandler { get; set; }
        public string ContactInfo { get; set; }
        public string Location { get; set; }
        public string Reason { get; set; }
        public XRect Rectangle { get; set; }
        public XImage Image { get; set; }
        public SignRenderingMode Model { get; set; }
    }
    /// <summary>
    /// 渲染模式
    /// </summary>
    public enum SignRenderingMode
    {
        Text = 1,
        Image = 2,
        Image_And_Text = 3
    }
}
