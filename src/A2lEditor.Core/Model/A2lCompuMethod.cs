namespace A2lEditor.Core.Model;

public sealed record A2lCompuMethod(
    string Name,
    string LongIdentifier,
    string ConversionType,
    string Format,
    string Unit,
    double CoeffA,
    double CoeffB,
    double CoeffC,
    double CoeffD,
    double CoeffE,
    double CoeffF,
    LineRange SourceLines);
