﻿namespace UglyToad.PdfPig.Graphics.Operations
{
    using Content;

    internal class SetNonStrokeColorDeviceCmyk : IGraphicsStateOperation
    {
        public const string Symbol = "K";

        public string Operator => Symbol;

        public decimal C { get; }

        public decimal M { get; }

        public decimal Y { get; }

        public decimal K { get; }

        public SetNonStrokeColorDeviceCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            C = c;
            M = m;
            Y = y;
            K = k;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
        }

        public override string ToString()
        {
            return $"{C} {M} {Y} {K} {Symbol}";
        }
    }
}