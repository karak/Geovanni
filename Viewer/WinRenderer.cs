using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TextComposing.Printing
{
    internal class WinFormPrinter : IPrinter
    {
        private static string _fontFamilyName = "ＭＳ 明朝";//"ヒラギノ明朝 ProN W3"
        private static FontFamily _fontFamily = new FontFamily(_fontFamilyName);
        private static StringFormat format = new StringFormat(StringFormatFlags.DirectionVertical | StringFormatFlags.DirectionRightToLeft);
        
        private readonly float _pixelPerPoint;
        private float _fontSize;
        private Font _fontCache;

        private void ChangeFontSize(float newEmSize)
        {
            _fontCache = new Font(_fontFamily, newEmSize, FontStyle.Regular, GraphicsUnit.Point, 0, true);
        }

        private Brush _brush = new SolidBrush(Color.Black);

        private float _x;
        private float _y;
        private Graphics _graphics;

        public WinFormPrinter(Graphics g, float fontSizeByPoint)
        {
            if (g.PageUnit != GraphicsUnit.Pixel) throw new ArgumentException("g");

            var ppp = g.DpiY / 72F;
            var paperWidth = g.VisibleClipBounds.Width;
            var paddingRightByPixels = 20;
            var x0 = (paperWidth - fontSizeByPoint / 2) / ppp - paddingRightByPixels;
            _x = x0;
            _y = 0;

            _graphics = g;
            _pixelPerPoint = ppp;
            _fontSize = fontSizeByPoint;
            ChangeFontSize(fontSizeByPoint);
        }

        public float FontSize
        {
            get { return _fontSize; }

            set
            {
                _fontSize = value;
                ChangeFontSize(_fontSize);
            }
        }

        public void PrintJapaneseLetter(UChar letter, float length)
        {
            var deltaY = new GlyphMetric(letter, length).VerticalOffset;
            DrawString(letter.ToString(), _x, _y - deltaY, _graphics, _fontCache);
            _y += length;
        }

        public void PrintLatinText(UString text, float length)
        {
            DrawString(text.String, _x, _y, _graphics, _fontCache);
            _y += length;
        }

        public void Space(float length)
        {
            _y += length;
        }

        public void CarriageReturn()
        {
            _y = 0;
        }

        public void LineFeed(float leading)
        {
            _x -= leading;
        }

        public PrinterMemento StorePositionAndFont()
        {
            var currentFontSize = this.FontSize;
            var currentX = this._x;
            var currentY = this._y;
            return () =>
                {
                    _x = currentX; _y = currentY;
                    FontSize = currentFontSize;
                };
        }

        private void DrawString(String text, float x, float y, Graphics g, System.Drawing.Font font)
        {
            var centerX = (x + font.Size / 2);
            g.DrawString(text, font, _brush, centerX * _pixelPerPoint, y * _pixelPerPoint, format);
        }

        public void PageBreak()
        {
            //no op
            var rightXByPixel = (_x + FontSize) * _pixelPerPoint;
            _graphics.DrawLine(new Pen(_brush), rightXByPixel, 0F, rightXByPixel, _graphics.VisibleClipBounds.Height);
        }
    }
}
