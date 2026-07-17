namespace A2lEditor.Core.Model;

public sealed record A2lModule(
    string Name,
    string Comment,
    IReadOnlyList<A2lMeasurement> Measurements,
    IReadOnlyList<A2lCharacteristic> Characteristics,
    IReadOnlyList<A2lAxisPts> AxisPts,
    IReadOnlyList<A2lCompuMethod> CompuMethods,
    IReadOnlyList<A2lRecordLayout> RecordLayouts,
    IReadOnlyList<A2lGroup> Groups,
    string? ModPar,
    IReadOnlyList<A2lAxisDescr> AxisDescr,
    IReadOnlyList<A2lUserRights> UserRights,
    IReadOnlyList<A2lVersionInfo> VersionInfo,
    LineRange SourceLines);