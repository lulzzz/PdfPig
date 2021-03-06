﻿namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using Content;

    internal class SetCharacterSpacing : IGraphicsStateOperation
    {
        public const string Symbol = "Tc";

        public string Operator => Symbol;

        public decimal Spacing { get; }

        public SetCharacterSpacing(decimal spacing)
        {
            Spacing = spacing;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.CharacterSpacing = Spacing;
        }

        public override string ToString()
        {
            return $"{Spacing} {Symbol}";
        }
    }
}