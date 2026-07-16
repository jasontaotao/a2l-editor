namespace A2lEditor.Core.Model;

public sealed record A2lDocument(
    A2lVersion Version,
    string ProjectName,
    string ProjectComment,
    string HeaderComment,
    IReadOnlyList<A2lModule> Modules,
    string RawText,
    int SourceLineCount)
{
    public int TotalMeasurementCount =>
        Modules.Sum(m => m.Measurements.Count);

    public int TotalCharacteristicCount =>
        Modules.Sum(m => m.Characteristics.Count);
}
