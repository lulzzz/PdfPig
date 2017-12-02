﻿namespace UglyToad.Pdf.Text
{
    public enum TextObjectComponentType
    {
        BeginText,
        EndText,
        TextFont,
        SetTextMatrix,
        MoveTextPosition,
        MoveTextPositionAndSetLeading,
        ShowText,
        ShowTextWithIndividualGlyphPositioning,
        SetTextLeading,
        SetTextRenderingMode,
        SetTextRise,
        SetWordSpacing,
        SetHorizontalTextScaling,
        MoveToNextLineStart,
        SetCharacterSpacing,
        Numeric,
        String,
        Font,
        Array,
        SetGrayNonStroking,
        SetGrayStroking,
        SetLineWidth,
        SetClippingPathNonZeroWinding,
        SetClippingPathEvenOdd,
        MoveNextLineAndShowText
    }
}