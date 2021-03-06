﻿namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using Geometry;
    using IO;
    using Tokenization;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class Type1FontParser
    {
        public Type1Font Parse(IInputBytes inputBytes)
        {
            var scanner = new CoreTokenScanner(inputBytes);

            if (!scanner.TryReadToken(out CommentToken comment) || !comment.Data.StartsWith("!"))
            {
                throw new InvalidFontFormatException("The Type1 program did not start with '%!'.");
            }

            string name;
            var parts = comment.Data.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3)
            {
                name = parts[1];
            }
            else
            {
                name = "Unknown";
            }

            var comments = new List<string>();

            while (scanner.MoveNext() && scanner.CurrentToken is CommentToken commentToken)
            {
                comments.Add(commentToken.Data);
            }

            var dictionaries = new List<DictionaryToken>();

            // Override arrays and names since type 1 handles these differently.
            var arrayTokenizer = new Type1ArrayTokenizer();
            var nameTokenizer = new Type1NameTokenizer();
            scanner.RegisterCustomTokenizer((byte)'{', arrayTokenizer);
            scanner.RegisterCustomTokenizer((byte)'/', nameTokenizer);

            try
            {
                var tokenSet = new PreviousTokenSet();
                tokenSet.Add(scanner.CurrentToken);
                while (scanner.MoveNext())
                {
                    if (scanner.CurrentToken is OperatorToken operatorToken)
                    {
                        HandleOperator(operatorToken, inputBytes, scanner, tokenSet, dictionaries);
                    }

                    tokenSet.Add(scanner.CurrentToken);
                }
            }
            finally
            {
                scanner.DeregisterCustomTokenizer(arrayTokenizer);
                scanner.DeregisterCustomTokenizer(nameTokenizer);
            }

            var encoding = GetEncoding(dictionaries);
            var matrix = GetFontMatrix(dictionaries);
            var boundingBox = GetBoundingBox(dictionaries);

            return new Type1Font(name, encoding, matrix, boundingBox);
        }

        private void HandleOperator(OperatorToken token, IInputBytes bytes, ISeekableTokenScanner scanner, PreviousTokenSet set, List<DictionaryToken> dictionaries)
        {
            switch (token.Data)
            {
                case "dict":
                    var number = ((NumericToken)set[0]).Int;
                    var dictionary = ReadDictionary(number, scanner);

                    dictionaries.Add(dictionary);
                    break;
                case "currentfile":
                    if (!scanner.MoveNext() || scanner.CurrentToken != OperatorToken.Eexec)
                    {
                        return;
                    }

                    // For now we will not read this stuff.
                    SkipEncryptedContent(bytes);
                    break;
                default:
                    return;
            }
        }

        private void SkipEncryptedContent(IInputBytes bytes)
        {
            bytes.Seek(bytes.Length - 1);

            while (bytes.MoveNext())
            {
                // skip to end.
            }
        }

        private static DictionaryToken ReadDictionary(int keys, ISeekableTokenScanner scanner)
        {
            IToken previousToken = null;

            var dictionary = new Dictionary<IToken, IToken>();

            // Skip the operators "dup" etc to reach "begin".
            while (scanner.MoveNext() && (!(scanner.CurrentToken is OperatorToken operatorToken) || operatorToken.Data != "begin"))
            {
                // Skipping.
            }

            for (int i = 0; i < keys; i++)
            {
                if (!scanner.TryReadToken(out NameToken key))
                {
                    return new DictionaryToken(dictionary);
                }

                if (key.Data.Equals(NameToken.Encoding))
                {
                    dictionary[key] = ReadEncoding(scanner);
                    continue;
                }

                while (scanner.MoveNext())
                {
                    if (scanner.CurrentToken == OperatorToken.Def)
                    {
                        dictionary[key] = previousToken;

                        break;
                    }

                    if (scanner.CurrentToken == OperatorToken.Dict)
                    {
                        if (!(previousToken is NumericToken numeric))
                        {
                            return new DictionaryToken(dictionary);
                        }

                        var inner = ReadDictionary(numeric.Int, scanner);

                        previousToken = inner;
                    }
                    else if (scanner.CurrentToken == OperatorToken.Readonly)
                    {
                        // skip
                    }
                    else if (scanner.CurrentToken is OperatorToken op && op.Data == "end")
                    {
                        // skip
                    }
                    else
                    {
                        previousToken = scanner.CurrentToken;
                    }
                }
            }

            return new DictionaryToken(dictionary);
        }

        private static ArrayToken ReadEncoding(ISeekableTokenScanner scanner)
        {
            var result = new List<IToken>();

            // Treat encoding differently, it's what we came here for!
            if (!scanner.TryReadToken(out NumericToken _))
            {
                return new ArrayToken(result);
            }

            if (!scanner.TryReadToken(out OperatorToken arrayOperatorToken) || arrayOperatorToken.Data != "array")
            {
                return new ArrayToken(result);
            }

            while (scanner.MoveNext() && (!(scanner.CurrentToken is OperatorToken forOperator) || forOperator.Data != "for"))
            {
                // skip these operators for now, they're probably important...
            }

            if (scanner.CurrentToken != OperatorToken.For)
            {
                return new ArrayToken(result);
            }

            while (scanner.MoveNext() && scanner.CurrentToken != OperatorToken.Def && scanner.CurrentToken != OperatorToken.Readonly)
            {
                if (scanner.CurrentToken != OperatorToken.Dup)
                {
                    throw new InvalidFontFormatException("Expected the array for encoding to begin with 'dup'.");
                }

                scanner.MoveNext();
                var number = (NumericToken)scanner.CurrentToken;
                scanner.MoveNext();
                var name = (NameToken)scanner.CurrentToken;

                if (!scanner.TryReadToken(out OperatorToken put) || put != OperatorToken.Put)
                {
                    throw new InvalidFontFormatException("Expected the array entry to end with 'put'.");
                }

                result.Add(number);
                result.Add(name);
            }

            while (scanner.CurrentToken != OperatorToken.Def && scanner.MoveNext())
            {
                // skip
            }

            return new ArrayToken(result);
        }

        private static Dictionary<int, string> GetEncoding(IReadOnlyList<DictionaryToken> dictionaries)
        {
            var result = new Dictionary<int, string>();

            foreach (var dictionary in dictionaries)
            {
                if (dictionary.TryGet(NameToken.Encoding, out var token) && token is ArrayToken encodingArray)
                {
                    for (var i = 0; i < encodingArray.Data.Count; i += 2)
                    {
                        var code = (NumericToken) encodingArray.Data[i];
                        var name = (NameToken) encodingArray.Data[i + 1];

                        result[code.Int] = name.Data;
                    }

                    return result;
                }
            }

            return result;
        }

        private static ArrayToken GetFontMatrix(IReadOnlyList<DictionaryToken> dictionaries)
        {
            foreach (var dictionaryToken in dictionaries)
            {
                if (dictionaryToken.TryGet(NameToken.FontMatrix, out var token) && token is ArrayToken array)
                {
                    return array;
                }
            }

            return null;
        }

        private static PdfRectangle GetBoundingBox(IReadOnlyList<DictionaryToken> dictionaries)
        {
            foreach (var dictionary in dictionaries)
            {
                if (dictionary.TryGet(NameToken.FontBbox, out var token) && token is ArrayToken array && array.Data.Count == 4)
                {
                    var x1 = (NumericToken) array.Data[0];
                    var y1 = (NumericToken) array.Data[1];
                    var x2 = (NumericToken) array.Data[2];
                    var y2 = (NumericToken) array.Data[3];

                    return new PdfRectangle(x1.Data, y1.Data, x2.Data, y2.Data);
                }
            }

            return null;
        }

        private class PreviousTokenSet
        {
            private readonly IToken[] tokens = new IToken[3];

            public IToken this[int index] => tokens[2 - index];

            public void Add(IToken token)
            {
                tokens[0] = tokens[1];
                tokens[1] = tokens[2];
                tokens[2] = token;
            }
        }
    }
}



