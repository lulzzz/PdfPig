﻿namespace UglyToad.PdfPig.Tests
{
    using System.Text;
    using PdfPig.IO;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Util;

    internal static class StringBytesTestConverter
    {
        public static Result Convert(string s, bool readFirst = true)
        {
            var input = new ByteArrayInputBytes(Encoding.UTF8.GetBytes(s));

            byte initialByte = 0;
            if (readFirst)
            {
                input.MoveNext();
                initialByte = input.CurrentByte;
            }
            
            return new Result
            {
                First = initialByte,
                Bytes = input
            };
        }

        public class Result
        {
            public byte First { get; set; }

            public IInputBytes Bytes { get; set; }
        }

        internal static CoreTokenScanner Scanner(string s)
        {
            var result = new CoreTokenScanner(new ByteArrayInputBytes(OtherEncodings.StringAsLatin1Bytes(s)));

            return result;
        }
    }
}
