using System.Text;
using A2lEditor.Core.Model;

namespace A2lEditor.Core.Serialization;

public sealed class A2lDocumentWriter
{
    public void WriteToFile(A2lDocument doc, string path)
    {
        using var sw = new StreamWriter(path, append: false, Encoding.UTF8);
        WriteToString(doc, sw);
    }

    public void WriteToString(A2lDocument doc, TextWriter sw)
    {
        sw.WriteLine($"ASAP2_VERSION 1 {MinorVersion(doc.Version)}");

        sw.Write("/begin PROJECT ");
        sw.Write(doc.ProjectName);
        sw.Write(' ');
        WriteEscapedString(sw, doc.ProjectComment);
        sw.WriteLine();

        // v0.4: HEADER block (per project, if present)
        if (!string.IsNullOrEmpty(doc.HeaderComment))
        {
            sw.Write(" /begin HEADER ");
            WriteEscapedString(sw, doc.HeaderComment);
            sw.WriteLine(" /end HEADER");
        }

        // v0.3: MOD_COMMON block (per project, if present)
        if (doc.ModCommon is not null)
        {
            sw.Write("/begin MOD_COMMON ");
            WriteEscapedString(sw, doc.ModCommon.Comment);
            sw.Write(' ');
            sw.Write("BYTE_ORDER ");
            sw.WriteLine(doc.ModCommon.ByteOrder == A2lByteOrder.MSB_LAST ? "MSB_LAST" : "MSB_FIRST");
            sw.WriteLine("/end MOD_COMMON");
        }

        foreach (var module in doc.Modules)
        {
            WriteModule(sw, module);
        }

        sw.WriteLine("/end PROJECT");
    }

    private static void WriteEscapedString(TextWriter sw, string? s)
    {
        if (string.IsNullOrEmpty(s)) return;
        sw.Write('"');
        sw.Write(StringLiteralEscaper.Escape(s));
        sw.Write('"');
    }

    private static int MinorVersion(A2lVersion v) => v switch
    {
        A2lVersion.V1_31 => 31,
        A2lVersion.V1_6x => 61,
        _ => 31,
    };

    private static void WriteModule(TextWriter sw, A2lModule module)
    {
        sw.Write("/begin MODULE ");
        sw.Write(module.Name);
        sw.Write(' ');
        WriteEscapedString(sw, module.Comment);
        sw.WriteLine();

        // v0.3: MOD_PAR block (per module, if present)
        if (module.ModPar is not null)
        {
            sw.Write("/begin MOD_PAR ");
            WriteEscapedString(sw, module.ModPar);
            sw.WriteLine(" /end MOD_PAR");
        }

        // Existing per-block writes (MEASUREMENT, CHARACTERISTIC, etc.) preserved from v0.1.1
        foreach (var meas in module.Measurements) WriteMeasurement(sw, meas);
        foreach (var ch in module.Characteristics) WriteCharacteristic(sw, ch);
        foreach (var ap in module.AxisPts) WriteAxisPts(sw, ap);
        foreach (var cm in module.CompuMethods) WriteCompuMethod(sw, cm);
        foreach (var rl in module.RecordLayouts) WriteRecordLayout(sw, rl);
        foreach (var gr in module.Groups) WriteGroup(sw, gr);

        sw.WriteLine("/end MODULE");
    }

    private static void WriteMeasurement(TextWriter sw, A2lMeasurement m)
    {
        sw.Write("/begin MEASUREMENT ");
        sw.Write(m.Name);
        sw.Write(' ');
        WriteEscapedString(sw, m.LongIdentifier);
        sw.Write(' ');
        sw.Write(m.DataType);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(m.CompuMethod)) sw.Write(m.CompuMethod);
        sw.Write(' ');
        sw.Write(m.Resolution);
        sw.Write(' ');
        sw.Write(m.Accuracy);
        sw.Write(' ');
        sw.Write(m.LowerLimit);
        sw.Write(' ');
        sw.Write(m.UpperLimit);
        if (m.EcuAddress != 0)
        {
            sw.Write(" ECU_ADDRESS 0x");
            sw.Write(m.EcuAddress.ToString("X"));
        }
        sw.WriteLine();
        sw.WriteLine("/end MEASUREMENT");
    }

    private static void WriteCharacteristic(TextWriter sw, A2lCharacteristic c)
    {
        sw.Write("/begin CHARACTERISTIC ");
        sw.Write(c.Name);
        sw.Write(' ');
        WriteEscapedString(sw, c.LongIdentifier);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(c.RecordLayout)) sw.Write(c.RecordLayout);
        sw.Write(' ');
        if (c.EcuAddress != 0) sw.Write(c.EcuAddress.ToString("X"));
        sw.Write(' ');
        sw.Write(c.LowerLimit);
        sw.Write(' ');
        sw.Write(c.UpperLimit);
        sw.WriteLine();
        sw.WriteLine("/end CHARACTERISTIC");
    }

    private static void WriteAxisPts(TextWriter sw, A2lAxisPts a) =>
        sw.WriteLine($"/begin AXIS_PTS {a.Name} /end AXIS_PTS");

    private static void WriteCompuMethod(TextWriter sw, A2lCompuMethod c) =>
        sw.WriteLine($"/begin COMPU_METHOD {c.Name} /end COMPU_METHOD");

    private static void WriteRecordLayout(TextWriter sw, A2lRecordLayout r) =>
        sw.WriteLine($"/begin RECORD_LAYOUT {r.Name} /end RECORD_LAYOUT");

    private static void WriteGroup(TextWriter sw, A2lGroup g) =>
        sw.WriteLine($"/begin GROUP {g.Name} /end GROUP");
}
