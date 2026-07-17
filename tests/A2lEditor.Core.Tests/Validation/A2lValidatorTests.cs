using A2lEditor.Core.Model;
using A2lEditor.Core.Validation;
using A2lEditor.Core.Parsing;
using FluentAssertions;

namespace A2lEditor.Core.Tests.Validation;

public class A2lValidatorTests
{
    [Fact]
    public void Validate_DuplicateMeasurementNames_ReturnsError()
    {
        var m = new A2lModule("M", "",
            new[] {
                new A2lMeasurement("dup", "", A2lDataType.UBYTE, "", "0", "0", "0", "255", 0, new LineRange(1, 1)),
                new A2lMeasurement("dup", "", A2lDataType.UBYTE, "", "0", "0", "0", "255", 0, new LineRange(2, 2))
            },
            Array.Empty<A2lCharacteristic>(),
            Array.Empty<A2lAxisPts>(),
            Array.Empty<A2lCompuMethod>(),
            Array.Empty<A2lRecordLayout>(),
            Array.Empty<A2lGroup>(),
            null,
            Array.Empty<A2lAxisDescr>(),
            Array.Empty<A2lUserRights>(),
            Array.Empty<A2lVersionInfo>(),
            new LineRange(1, 2));
        var doc = new A2lDocument(A2lVersion.V1_31, "P", "", "", null, new[] { m },
            "", 1);
        var v = new A2lValidator();
        var errors = v.Validate(doc);
        errors.Should().Contain(e => e.Message.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_EmptyDocument_ReturnsProjectNameError()
    {
        var doc = new A2lDocument(A2lVersion.V1_31, "", "", "", null, Array.Empty<A2lModule>(), "", 1);
        var v = new A2lValidator();
        var errors = v.Validate(doc);
        errors.Should().Contain(e => e.Message.Contains("PROJECT name"));
    }

    [Fact]
    public void Validate_DuplicateCharacteristicNames_ReturnsError()
    {
        var m = new A2lModule("M", "",
            Array.Empty<A2lMeasurement>(),
            new[] {
                new A2lCharacteristic("dup", "", "", 0, "0", "255", new LineRange(3, 3)),
                new A2lCharacteristic("dup", "", "", 0, "0", "255", new LineRange(4, 4))
            },
            Array.Empty<A2lAxisPts>(),
            Array.Empty<A2lCompuMethod>(),
            Array.Empty<A2lRecordLayout>(),
            Array.Empty<A2lGroup>(),
            null,
            Array.Empty<A2lAxisDescr>(),
            Array.Empty<A2lUserRights>(),
            Array.Empty<A2lVersionInfo>(),
            new LineRange(1, 4));
        var doc = new A2lDocument(A2lVersion.V1_31, "P", "", "", null, new[] { m },
            "", 1);
        var v = new A2lValidator();
        var errors = v.Validate(doc);
        errors.Should().Contain(e => e.Message.Contains("Duplicate CHARACTERISTIC"));
    }

    [Fact]
    public void Validate_UnknownCompuMethodReference_ReturnsWarning()
    {
        var m = new A2lModule("M", "",
            new[] {
                new A2lMeasurement("sig1", "", A2lDataType.UBYTE, "MISSING_CM", "0", "0", "0", "255", 0, new LineRange(5, 5))
            },
            Array.Empty<A2lCharacteristic>(),
            Array.Empty<A2lAxisPts>(),
            new[] {
                new A2lCompuMethod("EXISTING_CM", "", "IDENTICAL", "", "", 0, 1, 0, 0, 0, 0, new LineRange(1, 1))
            },
            Array.Empty<A2lRecordLayout>(),
            Array.Empty<A2lGroup>(),
            null,
            Array.Empty<A2lAxisDescr>(),
            Array.Empty<A2lUserRights>(),
            Array.Empty<A2lVersionInfo>(),
            new LineRange(1, 5));
        var doc = new A2lDocument(A2lVersion.V1_31, "P", "", "", null, new[] { m },
            "", 1);
        var v = new A2lValidator();
        var errors = v.Validate(doc);
        errors.Should().Contain(e => e.Severity == ErrorSeverity.Warning &&
            e.Message.Contains("unknown COMPU_METHOD"));
    }
}