﻿namespace UglyToad.PdfPig.Fonts.Simple
{
    using Cmap;
    using Composite;
    using Core;
    using Encodings;
    using Geometry;
    using IO;
    using Tokenization.Tokens;

    /// <summary>
    /// A font based on the Adobe Type 1 font format.
    /// </summary>
    internal class Type1FontSimple : IFont
    {
        private readonly int firstChar;
        private readonly int lastChar;
        private readonly decimal[] widths;
        private readonly FontDescriptor fontDescriptor;
        private readonly Encoding encoding;
        private readonly ToUnicodeCMap toUnicodeCMap;
        private readonly TransformationMatrix fontMatrix = TransformationMatrix.FromValues(0.001m, 0, 0, 0.001m, 0, 0);

        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public Type1FontSimple(NameToken name, int firstChar, int lastChar, decimal[] widths, FontDescriptor fontDescriptor, Encoding encoding, CMap toUnicodeCMap)
        {
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.fontDescriptor = fontDescriptor;
            this.encoding = encoding;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);
            Name = name;
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            if (toUnicodeCMap.CanMapToUnicode)
            {
                return toUnicodeCMap.TryGet(characterCode, out value);
            }

            value = null;

            if (encoding == null)
            {
                try
                {
                    value = char.ConvertFromUtf32(characterCode);
                    return true;
                }
                catch
                {
                    // our quick hack has failed, we should decode the type 1 font!
                }

                return false;
            }

            var name = encoding.GetName(characterCode);

            try
            {
                value = GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public PdfVector GetDisplacement(int characterCode)
        {
            return fontMatrix.Transform(new PdfVector(GetWidth(characterCode), 0));
        }

        public decimal GetWidth(int characterCode)
        {
            if (characterCode < firstChar || characterCode > lastChar)
            {
                return 250;
            }

            return widths[characterCode - firstChar];
        }

        public PdfRectangle GetBoundingBox(int characterCode)
        {
            throw new System.NotImplementedException();
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }
    }
}
