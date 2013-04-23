using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CC = TextComposing.CharacterClasses;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextPageSize = iTextSharp.text.PageSize;
using StringInfo = System.Globalization.StringInfo;

namespace TextComposing.IO.Pdf
{
    public class PdfPrinter : Printing.IPrinter, IDisposable
    {
        private PdfOutlineBuilder _outlineBuilder = new PdfOutlineBuilder();
        private EmdashRenderer _emdashRenderer = new EmdashRenderer();


        //ごま
        private static readonly UChar[] symbols = new UChar[] { UChar.FromCodePoint(0xFE45) };

        private Document _doc;
        private PdfWriter _writer;
        private BaseFont _font;
        bool _isPsuedoVertical;
        private BaseFont _latinFont;
        private float _latinBaselineOffsetRatio;
        private BaseFont _nombreFont;
        private BaseFont _symbolFont;
        private readonly float _initialX;
        private readonly float _initialXMirrored;
        private readonly float _initialY;
        private float _fontSize;
        private float _xtlm;
        private float _deltaY;
        private bool _fontSizeChanged;

        private bool _isMirrorEnabled;
        private int _pageNumber;
        private readonly float _pageX;
        private readonly float _pageY;
        private BaseFont _headerFont;
        private float _pageFontSize;
        private UString _headerStringLeft;
        private float _pageHeaderOffset;
        
        public PdfPrinter(string path, Layout layout, Pdf.PdfFontSetting fontSetting)
        {
            Rectangle pdfPageSize;
            switch (layout.PageSize)
            {
                case PageSize.A5Portrait:
                    pdfPageSize = iTextPageSize.A5;
                    break;
                case PageSize.A4Landscape:
                    pdfPageSize = iTextPageSize.A4.Rotate();
                    break;
                default:
                    throw new NotSupportedException();
            }
            _font = fontSetting.Font.CreateBaseFont(RunDirection.Vertical, false);
            _isPsuedoVertical = fontSetting.Font.PsuedoVertical;
            _headerFont = fontSetting.Font.CreateBaseFont(RunDirection.Horizontal, true);
            _latinFont = fontSetting.LatinFont.CreateBaseFont(RunDirection.Horizontal, true);
            _latinBaselineOffsetRatio = fontSetting.LatinBaselineOffsetRatio;
            _nombreFont = fontSetting.LatinFont.CreateBaseFont(RunDirection.Horizontal, true);
            _symbolFont = fontSetting.SymbolFont.CreateBaseFont(RunDirection.Vertical, false);
            _doc = new Document(pdfPageSize);
            _isMirrorEnabled = layout.Mirroring;
            _initialX = pdfPageSize.Width - layout.RightMargin - layout.FontSize / 2;
            _initialXMirrored = layout.RightMargin + layout.FontSize * 2 + layout.Leading * (layout.NumberOfLines - 2);
            _initialY = pdfPageSize.Height - layout.TopMargin;
            _pageX = pdfPageSize.Width - layout.PageNumberRightMargin;
            _pageY = pdfPageSize.Height - layout.PageNumberTopMargin;
            _pageFontSize = 10.5F;
            _pageHeaderOffset = layout.PageHeaderOffset;
            _writer = PdfWriter.GetInstance(_doc, new FileStream(path, FileMode.Create));
            _writer.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
            _writer.ViewerPreferences = PdfWriter.DirectionR2L | (layout.Mirroring ? PdfWriter.PageLayoutTwoPageRight : 0);
            _writer.SetFullCompression();
            _fontSizeChanged = true;
            _pageNumber = 0;
        }

        public float FontSize
        {
            get { return _fontSize; }
            set { _fontSize = value; _fontSizeChanged = true; }
        }

        internal BaseFont LatinFont
        {
            get { return _latinFont; }
        }

        public UString Header
        {
            get { return _headerStringLeft; }
            set { _headerStringLeft = value; }
        }

        public void Connect()
        {
            _doc.Open();
            BeginPage();
        }

        private void BeginPage()
        {
            var cb = _writer.DirectContent;

            ++_pageNumber;
            {

                //page number
                var pageNumberString = _pageNumber.ToString();
                cb.SetFontAndSize(_nombreFont, _pageFontSize);
                cb.BeginText();
                if (!_isMirrorEnabled || IsLeftPage)
                {
                    cb.SetTextMatrix(_writer.PageSize.Width - _pageX, _pageY);
                }
                else
                {
                    var width = _headerFont.GetWidthPoint(pageNumberString, _pageFontSize);
                    cb.SetTextMatrix(_pageX - width, _pageY);
                }

                cb.ShowText(pageNumberString);
                cb.EndText();
                
                //page header
                if (_headerStringLeft != null && _headerStringLeft.Length > 0)
                {
                    if (!_isMirrorEnabled || IsLeftPage)
                    {
                        cb.BeginText();
                        cb.SetTextMatrix(_writer.PageSize.Width - _pageX + _pageHeaderOffset, _pageY);
                        cb.SetFontAndSize(_headerFont, _pageFontSize);
                        var latinMode = new LatinMode();
                        latinMode.Flush += (sender, e) =>
                        {
                            cb.SetFontAndSize(_latinFont, _pageFontSize);
                            //TODO: _latinBaselineOffsetRatio;
                            cb.ShowText(e.LatinText.String);
                            cb.SetFontAndSize(_headerFont, _pageFontSize);
                        };

                        foreach (var ch in _headerStringLeft)
                        {
                            latinMode.Send(ch);
                            if (latinMode.CurrentLang != Lang.Latin)
                            {
                                cb.ShowText(ch.ToString()); //TODO: こちらもバッファリング latinMode と呼ばれてるオブジェクトで
                            }
                        }
                        latinMode.ForceFlush();
                        cb.EndText();
                    }
                }
            }

            ApplyFontForce();
            cb.BeginText();
            _xtlm = (_isMirrorEnabled && IsLeftPage ? _initialXMirrored : _initialX);
            cb.SetTextMatrix(_xtlm, _initialY);
            _deltaY = 0;
        }
        
        private bool IsLeftPage
        {
            get { return _pageNumber % 2 == 1; }
        }

        public void CarriageReturn()
        {
            var cb = _writer.DirectContent;
            cb.SetTextMatrix(_xtlm, _initialY);
            _deltaY = 0;
        }

        private void EndPage()
        {
            var cb = _writer.DirectContent;
            cb.EndText();
        }

        private void ApplyFont()
        {
            if (_fontSizeChanged)
            {
                ApplyFontForce();
            }
        }

        private void ApplyFontForce()
        {
            var cb = _writer.DirectContent;
            cb.SetFontAndSize(_font, _fontSize);
            _fontSizeChanged = false;
        }

        public void Disconnect()
        {
            if (_doc.IsOpen())
            {
                EndPage();

                PdfContentByte cb = _writer.DirectContent;
                PdfOutline root = cb.RootOutline;
                _outlineBuilder.GenerateTo(root);
                _outlineBuilder.Clear();
                _doc.Close();
            }
        }

        void IDisposable.Dispose()
        {
            Disconnect();
        }

        const float mmPerPoint = 0.353F;
                
        public void PrintLatinText(UString text, float length)
        {
            //UNDONE: latin 単語中のサイズ変更は未対応
            var cb = _writer.DirectContent;
            _emdashRenderer.Close(cb, _fontSize, _xtlm, MyYTLM);
            cb.SetFontAndSize(_latinFont, _fontSize);
            cb.SetTextMatrix(0, -1, 1, 0, _xtlm - _fontSize * 0.5F + _fontSize * _latinBaselineOffsetRatio, MyYTLM);
            cb.ShowText(text.String);
            //フォントを復帰。場合によっては後に重ねて設定されるが構わない
            cb.SetFontAndSize(_font, _fontSize);
            _deltaY += length;
        }

        public void PrintJapaneseLetter(UChar letter, float length)
        {
            var cb = _writer.DirectContent;

            //実際にフォントを適用
            ApplyFont();

            //印字。ただし一部の文字についてはフォント変更。
            BaseFont specialFont = null;
            if (Array.IndexOf(symbols, letter) != -1)
            {
                specialFont = _symbolFont;
            }
            if (specialFont != null) cb.SetFontAndSize(specialFont, _fontSize);

            var voffset = +new GlyphMetric(letter, length).VerticalOffset;
            if (!_emdashRenderer.Send(cb, _fontSize, _xtlm, MyYTLM + voffset, letter, length))
            {
                string letterAsString = letter.ToString();
                SetAppropriateTextMatrix(letter, voffset, cb);
                cb.ShowText(letterAsString);
            }
            if (specialFont != null) cb.SetFontAndSize(_font, _fontSize);

            _deltaY += length;
        }

        private void SetAppropriateTextMatrix(UChar letter, float voffset, PdfContentByte cb)
        {
            var ytlm = MyYTLM;
            if (_isPsuedoVertical)
            {
                if (CC.Cl06(letter) ||
                    CC.Cl07(letter))
                {
                    //句読点を平行移動。
                    cb.SetTextMatrix(_xtlm + _fontSize * (1F / 2 + 1F / 8), ytlm + _fontSize * (1F / 2 + 1F / 8) + voffset);
                }
                else if (CC.Cl11(letter))
                {
                    //小書きの仮名を平行移動。
                    cb.SetTextMatrix(_xtlm + _fontSize / 8, ytlm + _fontSize / 8 + voffset);
                }
                else if (CC.Cl01(letter))
                {
                    //始め括弧を回転、平行移動
                    cb.SetTextMatrix(0F, -1F, 1F, 0F, _xtlm + _fontSize / 2, ytlm);
                }
                else if (letter.CodePoint == char.ConvertToUtf32("ー", 0))
                {
                    //音引きを回転、かつ左右反転
                    cb.SetTextMatrix(0F, -1F, -1F, 0F, _xtlm - _fontSize / 2, ytlm - _fontSize / 2);
                }
                else if (letter.CodePoint == char.ConvertToUtf32("—", 0)) //part of Cl08)
                {
                    //エムダッシュを回転
                    cb.SetTextMatrix(0F, -1F, 1F, 0F, _xtlm + _fontSize * (1F / 2F + 1 / 8F), ytlm - _fontSize / 2);
                }
                //TODO: 毎回 UString 作らない UChar[] で持つ。
                else if (
                    CC.Cl02(letter) ||
                    (new UString("―…‥").Contains(letter)) || //part of Cl08
                    CC.Cl10(letter) ||
                    (new UString("～＋±＝－÷≠：；‘’“”＜＞≦≧＿｜→↓←↑⇒⇔").Contains(letter))) //その他転置すべき記号。よく使いそうなものだけ
                {
                    //それ以外の記号を回転
                    cb.SetTextMatrix(0F, -1F, 1F, 0F, _xtlm + _fontSize / 2, ytlm - _fontSize / 2);
                }
                else
                {
                    cb.SetTextMatrix(_xtlm, ytlm + voffset);
                }
            }
            else
            {
                cb.SetTextMatrix(_xtlm, ytlm + voffset);
            }
        }

        private float MyYTLM
        {
            get { return _initialY - _deltaY; }
        }

        public void Space(float length)
        {
            _deltaY += length;
        }

        public void LineFeed(float leading)
        {
            CloseEmDashRendering();
            var cb = _writer.DirectContent;
            cb.MoveText(- leading, 0);
            _xtlm -= leading;
        }

        public Printing.PrinterMemento StorePositionAndFont()
        {
            var cb = _writer.DirectContent;
            var storedFontSize = FontSize;
            var storedXTLM = _xtlm;
            var storedDeltaY = _deltaY;
            var storedYTLM = cb.YTLM;

            return () =>
                {
                    FontSize = storedFontSize;
                    _deltaY = storedDeltaY;
                    _xtlm = storedXTLM;
                    cb.SetTextMatrix(storedXTLM, storedYTLM);
                };
        }

        public void PageBreak()
        {
            CloseEmDashRendering();
            //if (_writer.NewPage()) return;

            EndPage();
            _doc.NewPage();
            BeginPage();

            //TODO: ページ先頭なら改ページなし
            //TODO: 改丁（両開き時のみ）
        }

        public void SetOutlineHere(int level, UString title)
        {
            _outlineBuilder.AppendOutline(level, title, _writer.DirectContent);
        }

        private void CloseEmDashRendering()
        {
            _emdashRenderer.Close(_writer.DirectContent, _fontSize, _xtlm, MyYTLM);
        }
        //TODO: 改段
    }

    /// <summary>
    /// 全角ダッシュを罫線で描く。隙間も回避。
    /// </summary>
    internal class EmdashRenderer
    {
        private readonly float _lineWidth = 0.55F;
        private readonly float _emDashPaddingRatio = 0.06F;
        private bool _isPrevEmDash = false;

        public bool Send(PdfContentByte cb, float fontSize, float xtlm, float ytlm, UChar letter, float length)
        {
            if (IsEmDash(letter))
            {
                Open(cb, fontSize, xtlm, ytlm);
                return true;
            }
            else
            {
                Close(cb, fontSize, xtlm, ytlm);
                return false;
            }
        }

        private static bool IsEmDash(UChar letter)
        {
            return letter.CodePoint == 0x2014;
        }

        public bool Open(PdfContentByte cb, float fontSize, float xtlm, float ytlm)
        {
            if (!_isPrevEmDash)
            {
                var padding = fontSize * _emDashPaddingRatio;
                cb.EndText();
                cb.SetLineWidth(_lineWidth);
                cb.MoveTo(xtlm, ytlm - padding);
                _isPrevEmDash = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Close(PdfContentByte cb, float fontSize, float xtlm, float ytlm)
        {
            if (_isPrevEmDash)
            {
                var padding = fontSize * _emDashPaddingRatio;
                cb.LineTo(xtlm, ytlm + padding);
                cb.Stroke();
                cb.BeginText();
                _isPrevEmDash = false;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
