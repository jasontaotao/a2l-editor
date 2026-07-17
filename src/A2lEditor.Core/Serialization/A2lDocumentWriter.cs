using System.Globalization;
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
            if (doc.ModCommon.DataSize.HasValue)
            {
                sw.Write(" DATA_SIZE ");
                sw.WriteLine(doc.ModCommon.DataSize.Value);
            }
            if (doc.ModCommon.AlignmentByteOrder.HasValue)
            {
                sw.Write(" ALIGNMENT_BYTE_ORDER ");
                sw.WriteLine(doc.ModCommon.AlignmentByteOrder.Value == A2lByteOrder.MSB_LAST ? "MSB_LAST" : "MSB_FIRST");
            }
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
        foreach (var ad in module.AxisDescr) WriteAxisDescr(sw, ad);
        foreach (var ur in module.UserRights) WriteUserRights(sw, ur);
        foreach (var vi in module.VersionInfo) WriteVersionInfo(sw, vi);

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

    private static void WriteAxisPts(TextWriter sw, A2lAxisPts a)
    {
        sw.Write("/begin AXIS_PTS ");
        sw.Write(a.Name);
        sw.Write(' ');
        WriteEscapedString(sw, a.LongIdentifier);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(a.RecordLayout)) sw.Write(a.RecordLayout);
        sw.Write(' ');
        if (a.EcuAddress != 0) sw.Write(a.EcuAddress.ToString("X"));
        sw.Write(' ');
        if (!string.IsNullOrEmpty(a.InputQuantity)) sw.Write(a.InputQuantity);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(a.CompuMethod)) sw.Write(a.CompuMethod);
        sw.Write(' ');
        sw.Write(a.NumberOfAxisPts);
        sw.Write(' ');
        sw.Write(a.LowerLimit);
        sw.Write(' ');
        sw.Write(a.UpperLimit);
        sw.WriteLine();
        sw.WriteLine("/end AXIS_PTS");
    }

    private static void WriteCompuMethod(TextWriter sw, A2lCompuMethod c)
    {
        sw.Write("/begin COMPU_METHOD ");
        sw.Write(c.Name);
        sw.Write(' ');
        WriteEscapedString(sw, c.LongIdentifier);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(c.ConversionType)) sw.Write(c.ConversionType);
        sw.Write(' ');
        WriteEscapedString(sw, c.Format);
        sw.Write(' ');
        WriteEscapedString(sw, c.Unit);
        sw.WriteLine();
        if (!string.IsNullOrEmpty(c.ConversionType) &&
            c.ConversionType != "IDENTICAL" &&
            c.ConversionType != "TAB_NOINTP" &&
            c.ConversionType != "TAB_VERB")
        {
            sw.Write(" COEFFS ");
            sw.Write(c.CoeffA.ToString(CultureInfo.InvariantCulture));
            sw.Write(' '); sw.Write(c.CoeffB.ToString(CultureInfo.InvariantCulture));
            sw.Write(' '); sw.Write(c.CoeffC.ToString(CultureInfo.InvariantCulture));
            sw.Write(' '); sw.Write(c.CoeffD.ToString(CultureInfo.InvariantCulture));
            sw.Write(' '); sw.Write(c.CoeffE.ToString(CultureInfo.InvariantCulture));
            sw.Write(' '); sw.Write(c.CoeffF.ToString(CultureInfo.InvariantCulture));
            sw.WriteLine();
        }
        sw.WriteLine("/end COMPU_METHOD");
    }

    private static void WriteRecordLayout(TextWriter sw, A2lRecordLayout r)
    {
        sw.Write("/begin RECORD_LAYOUT ");
        sw.WriteLine(r.Name);
        foreach (var entry in r.Entries)
        {
            sw.Write(' ');
            sw.Write(entry.Keyword);
            sw.Write(' ');
            sw.Write(entry.Position);
            sw.Write(' ');
            if (!string.IsNullOrEmpty(entry.DataType)) sw.Write(entry.DataType);
            sw.Write(' ');
            if (!string.IsNullOrEmpty(entry.IndexMode)) sw.Write(entry.IndexMode);
            sw.Write(' ');
            if (!string.IsNullOrEmpty(entry.AddressingMode)) sw.Write(entry.AddressingMode);
            sw.WriteLine();
        }
        sw.WriteLine("/end RECORD_LAYOUT");
    }

    private static void WriteGroup(TextWriter sw, A2lGroup g)
    {
        sw.Write("/begin GROUP ");
        sw.Write(g.Name);
        sw.Write(' ');
        WriteEscapedString(sw, g.LongIdentifier);
        if (g.IsRoot) sw.Write(" ROOT");
        sw.WriteLine();
        if (g.RefMeasurements.Count > 0)
        {
            sw.Write(" /begin REF_MEASUREMENT");
            foreach (var r in g.RefMeasurements) { sw.Write(' '); sw.Write(r); }
            sw.WriteLine(" /end REF_MEASUREMENT");
        }
        if (g.RefCharacteristics.Count > 0)
        {
            sw.Write(" /begin REF_CHARACTERISTIC");
            foreach (var r in g.RefCharacteristics) { sw.Write(' '); sw.Write(r); }
            sw.WriteLine(" /end REF_CHARACTERISTIC");
        }
        sw.WriteLine("/end GROUP");
    }

    private static void WriteAxisDescr(TextWriter sw, A2lAxisDescr a)
    {
        sw.Write("/begin AXIS_DESCR ");
        WriteEscapedString(sw, a.Attribute);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(a.InputQuantity)) sw.Write(a.InputQuantity);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(a.Conversion)) sw.Write(a.Conversion);
        sw.Write(' ');
        sw.Write(a.MaxNumberOfAxisPoints);
        sw.Write(' ');
        sw.Write(a.LowerLimit);
        sw.Write(' ');
        sw.Write(a.UpperLimit);
        sw.WriteLine();
        sw.WriteLine("/end AXIS_DESCR");
    }

    private static void WriteUserRights(TextWriter sw, A2lUserRights u)
    {
        sw.Write("/begin USER_RIGHTS ");
        WriteEscapedString(sw, u.UserId);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(u.ReadAccess)) sw.Write(u.ReadAccess);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(u.WriteAccess)) sw.Write(u.WriteAccess);
        sw.Write(' ');
        if (!string.IsNullOrEmpty(u.AccessMethod)) sw.Write(u.AccessMethod);
        sw.WriteLine();
        sw.WriteLine("/end USER_RIGHTS");
    }

    private static void WriteVersionInfo(TextWriter sw, A2lVersionInfo v)
    {
        sw.Write("/begin VERSION ");
        sw.Write(' ');
        if (!string.IsNullOrEmpty(v.VersionNo)) sw.Write(v.VersionNo);
        sw.Write(' ');
        if (v.Date != DateTime.MinValue) sw.Write(v.Date.ToString("yyyy-MM-dd"));
        sw.Write(' ');
        if (!string.IsNullOrEmpty(v.Vendor)) sw.Write(v.Vendor);
        sw.Write(' ');
        WriteEscapedString(sw, v.Description);
        sw.WriteLine();
        sw.WriteLine("/end VERSION");
    }
}
