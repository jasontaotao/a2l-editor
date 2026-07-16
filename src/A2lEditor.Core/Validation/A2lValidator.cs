using A2lEditor.Core.Model;
using A2lEditor.Core.Parsing;

namespace A2lEditor.Core.Validation;

public sealed class A2lValidator
{
    public IReadOnlyList<ParseError> Validate(A2lDocument doc)
    {
        var errors = new List<ParseError>();
        if (string.IsNullOrEmpty(doc.ProjectName))
            errors.Add(new ParseError(1, 1, "PROJECT name is empty", ErrorSeverity.Error));

        foreach (var module in doc.Modules)
        {
            ValidateModule(module, errors);
        }
        return errors;
    }

    private void ValidateModule(A2lModule m, List<ParseError> errors)
    {
        if (string.IsNullOrEmpty(m.Name))
            errors.Add(new ParseError(0, 0, "MODULE name is empty", ErrorSeverity.Error));

        var signalNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var meas in m.Measurements)
        {
            if (!signalNames.Add(meas.Name))
                errors.Add(new ParseError(meas.SourceLines.Start, 1,
                    $"Duplicate MEASUREMENT name: {meas.Name}", ErrorSeverity.Error));
        }
        foreach (var ch in m.Characteristics)
        {
            if (!signalNames.Add(ch.Name))
                errors.Add(new ParseError(ch.SourceLines.Start, 1,
                    $"Duplicate CHARACTERISTIC name: {ch.Name}", ErrorSeverity.Error));
        }

        var compuMethods = new HashSet<string>(m.CompuMethods.Select(c => c.Name),
            StringComparer.Ordinal);
        foreach (var meas in m.Measurements)
        {
            if (!string.IsNullOrEmpty(meas.CompuMethod) && !compuMethods.Contains(meas.CompuMethod))
                errors.Add(new ParseError(meas.SourceLines.Start, 1,
                    $"MEASUREMENT {meas.Name} references unknown COMPU_METHOD {meas.CompuMethod}",
                    ErrorSeverity.Warning));
        }
    }
}