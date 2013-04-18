using System;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace TextComposing.IO.Pdf
{
    public interface IPdfFontFamily
    {
        /// <summary>
        /// PDF用のフォント作成。
        /// </summary>
        /// <param name="dir">縦書き横書きの指定。和文フォントのみ</param>
        /// <param name="HalfWidth">アルファベットの半角利用。和文フォントのみ</param>
        /// <returns></returns>
        BaseFont CreateBaseFont(RunDirection dir, bool halfWidth);
        bool PsuedoVertical { get; }
    }

    public class PdfFontFamily : IPdfFontFamily
    {
        public delegate string EncodingSelector(RunDirection dir, bool halfWidth);

        public string Name { get; private set; }
        public EncodingSelector Encoding { get; private set; }
        public bool Embedded { get; private set; }
        public bool PsuedoVertical { get; private set; }
        

        static PdfFontFamily()
        {
            ResourceLoader.LoadFontResource();
        }

        public BaseFont CreateBaseFont(RunDirection dir, bool halfWidth)
        {
            return BaseFont.CreateFont(Name, Encoding(dir, halfWidth), Embedded);
        }

        #region Builtin
        public static PdfFontFamily Helvetica = new PdfFontFamily{ Name = BaseFont.HELVETICA, Encoding = Latin3, Embedded = false, PsuedoVertical = false };
        public static PdfFontFamily Courier = new PdfFontFamily{ Name = BaseFont.COURIER, Encoding = Latin3, Embedded = false, PsuedoVertical = false };
        public static PdfFontFamily TimesRoman = new PdfFontFamily{ Name = BaseFont.TIMES_ROMAN, Encoding = Latin3, Embedded = false, PsuedoVertical = false };
        public static PdfFontFamily HeiseiMincho = new PdfFontFamily{ Name = "HeiseiMin-W3", Encoding = UniJIS_UCS2, Embedded = false, PsuedoVertical = false };
        public static PdfFontFamily KozukaMincho = new PdfFontFamily{ Name = "KozMinPro-Regular", Encoding = UniJIS_UCS2, Embedded = false, PsuedoVertical = false };
        public static PdfFontFamily HeiseiKakuGothic = new PdfFontFamily{ Name = "HeiseiKakuGo-W5", Encoding = UniJIS_UCS2, Embedded = false, PsuedoVertical = false };
        #endregion

        #region Installed in system of author's, to embedded
        public static PdfFontFamily EBGaramond = new PdfFontFamily{ Name = InFontFolder("EBGaramond.otf"), Encoding = Latin3, Embedded = true, PsuedoVertical = false };
        public static PdfFontFamily MSMincho = new PdfFontFamily { Name = InFontFolder("msmincho.ttc,0"), Encoding = Identity, Embedded = true, PsuedoVertical = false };
        public static PdfFontFamily IwataMinchoOld = new PdfFontFamily { Name = InFontFolder("IwaOMinStd-Th.otf"), Encoding = Identity, Embedded = true, PsuedoVertical = true };
        public static PdfFontFamily HiraginoMincho = new PdfFontFamily { Name = InFontFolder("ヒラギノ明朝 ProN W3.otf"), Encoding = Identity, Embedded = true, PsuedoVertical = true };

        #endregion
        
        private static string InFontFolder(string filename)
        {
            return System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Fonts), filename);
        }

        #region Encodings
        /// <summary>
        /// ラテン文字基本
        /// </summary>
        private static string LatinBasic(RunDirection dir) { return BaseFont.CP1252; }
        
        ///<summary>
        ///ISO-8859-3(Latin 3)エスペラントのサーカムフレックスつき g に必要。
        /// </summary>
        private static string Latin3(RunDirection dir, bool halfWidth) { return "ISO-8859-3"; }

        private static string UniJIS_UCS2(RunDirection dir, bool halfWidth)
        {
            if (halfWidth)
                return dir == RunDirection.Vertical ? "UniJIS-UCS2-HW-V" : "UniJIS-UCS2-HW-H";
            else
                return dir == RunDirection.Vertical ? "UniJIS-UCS2-V" : "UniJIS-UCS2-H";
        }

        private static string Identity(RunDirection dir, bool halfWidth)
        {
            return dir == RunDirection.Vertical ? BaseFont.IDENTITY_V : BaseFont.IDENTITY_H;
        }

        #endregion
    }

    /// <summary>
    /// PDFフォント設定
    /// </summary>
    public class PdfFontSetting
    {
        /// <summary>
        /// 和文フォント（縦書き、横書き）
        /// </summary>
        public Pdf.IPdfFontFamily Font { get; set; }

        /// <summary>
        /// 欧文フォント（横書き）
        /// </summary>
        public Pdf.IPdfFontFamily LatinFont { get; set; }

        /// <summary>
        /// 欧文フォント使用時をベースラインから上にフォントサイズの何倍ずらすか
        /// </summary>
        public float LatinBaselineOffsetRatio { get; set; }

        /// <summary>
        /// 和文特殊記号用フォント
        /// </summary>
        /// <remarks>圏点類について、AcrobatReader のデフォルトである小塚明朝・ゴシックには含まれていないため、指定フォントを埋め込む。</remarks>
        public Pdf.IPdfFontFamily SymbolFont { get; set; }
    }

}
