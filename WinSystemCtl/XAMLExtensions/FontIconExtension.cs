using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.XAMLExtensions
{
    public class FontIconExtension : MarkupExtension
    {
        public string Glyph { get; set; }
        public int FontSize { get; set; } = 18;

        public FontIconExtension()
        {
        }
        public FontIconExtension(string glyph)
            : this()
        {
            Glyph = glyph;
        }

        public FontIconExtension(string glyph, int fontSize)
            : this(glyph)
        {
            FontSize = fontSize;
        }

        protected override object ProvideValue()
        {
            return new FontIcon()
            {
                Glyph = this.Glyph,
                FontSize = this.FontSize,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
            };
        }
    }
}
