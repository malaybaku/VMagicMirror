using System;
using System.Globalization;

namespace Baku.VMagicMirror
{
    public static class ParseUtil
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        
        public static bool FloatParse(string input, out float result) 
            => float.TryParse(input, NumberStyles.Float, Culture, out result);
        public static bool FloatParse(ReadOnlySpan<char> input, out float result) 
            => float.TryParse(input, NumberStyles.Float, Culture, out result);
    }
}
