using System;
using iTextSharp.text.pdf;

namespace TextComposing.IO.Pdf
{
    static class ResourceLoader
    {
        public static void LoadFontResource()
        {
            var resourcePath = System.IO.Path.Combine(GetExecutingFolder(), "iTextAsian.dll");
            BaseFont.AddToResourceSearch(resourcePath);
        }

        private static string GetExecutingFolder()
        {
            return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

    }
}
