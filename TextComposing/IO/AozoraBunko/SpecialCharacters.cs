using System;

namespace TextComposing.IO.AozoraBunko
{
    internal static class SpecialCharacters
    {
        public static readonly UChar ExternalCharacterPlaceholder = new UChar('※');
        public static readonly UChar BeforeRubyInitiater = new UChar('｜');
        public static readonly UChar RubyOpen = new UChar('《');
        public static readonly UChar RubyClose = new UChar('》');
        public static readonly UChar AnnotationOpenBracket = new UChar('［');
        public static readonly UChar AnnotationInitiatorChar = new UChar('＃');
        public static readonly UChar AnnotationCloseBracket = new UChar('］');
    }
}
