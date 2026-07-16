using System.Windows;
using System.Windows.Media;
using A2lEditor.Core.Highlighting;

namespace A2lEditor.App.Highlighting;

public static class TokenCategoryToBrush
{
    // Colors from spec section 5.3 — Light theme only in v0.2.
    private static readonly Brush BlockKeywordBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x80)));  // Navy
    private static readonly Brush StructuralKeywordBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF)));  // Blue
    private static readonly Brush DataTypeKeywordBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x80, 0x00, 0x80)));  // Purple
    private static readonly Brush IdentifierBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)));  // Black
    private static readonly Brush StringLiteralBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xA3, 0x15, 0x15)));  // DarkRed
    private static readonly Brush NumberBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00)));  // OliveGreen
    private static readonly Brush CommentBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00)));  // Green
    private static readonly Brush SymbolBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)));  // Black
    private static readonly Brush UnknownBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)));  // Gray

    public static Brush ForCategory(TokenCategory category) => category switch
    {
        TokenCategory.BlockKeyword => BlockKeywordBrush,
        TokenCategory.StructuralKeyword => StructuralKeywordBrush,
        TokenCategory.DataTypeKeyword => DataTypeKeywordBrush,
        TokenCategory.Identifier => IdentifierBrush,
        TokenCategory.StringLiteral => StringLiteralBrush,
        TokenCategory.Number => NumberBrush,
        TokenCategory.Comment => CommentBrush,
        TokenCategory.Symbol => SymbolBrush,
        _ => UnknownBrush,
    };

    private static T Freeze<T>(T obj) where T : Freezable
    {
        if (obj.CanFreeze) obj.Freeze();
        return obj;
    }
}