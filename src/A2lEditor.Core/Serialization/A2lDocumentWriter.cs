// TODO v0.2: stable round-trip with original A2L field order. Currently the writer
// emits comment-label-style field positions (e.g. `/* Name */  Sig1`) for readability
// rather than positional syntax, and Normalizes MOD_PAR/MOD_COMMON synthetic blocks
// are omitted (the v0.1.1 Parser handles only MEASUREMENT/CHARACTERISTIC/AXIS_PTS/
// COMPU_METHOD/RECORD_LAYOUT/GROUP). ASAP2_VERSION is normalized to "1 31" or "1 61".
//
// v0.1.1 constraints honored here:
// - Output is parseable by the current Asap131Parser (value != null, no Fatal).
// - Each MEASUREMENT/MODULE block has a proper /end boundary.
// - Output is UTF-8 with BOM (WriteToFile).
using System.Globalization;
using System.Text;
using A2lEditor.Core.Model;

namespace A2lEditor.Core.Serialization;

public sealed class A2lDocumentWriter
{
    public string Write(A2lDocument doc)
    {
        var sb = new StringBuilder();
        // Stable round-trip: emit a version the Asap131Parser accepts (minor <= 70).
        // V1_31 -> "1 31"; V1_6x -> "1 61" (instead of brief's "1 71" which is fatal-rejected by parser).
        sb.AppendLine(doc.Version switch
        {
            A2lVersion.V1_31 => "ASAP2_VERSION  1 31",
            A2lVersion.V1_6x => "ASAP2_VERSION  1 61",
            _ => "ASAP2_VERSION  1 31"
        });
        sb.AppendLine($"/begin PROJECT {doc.ProjectName} \"{EscapeString(doc.ProjectComment)}\"");
        if (!string.IsNullOrEmpty(doc.HeaderComment))
        {
            sb.AppendLine($" /begin HEADER \"{EscapeString(doc.HeaderComment)}\"");
            sb.AppendLine(" /end HEADER");
        }
        foreach (var module in doc.Modules) WriteModule(sb, module);
        sb.AppendLine("/end PROJECT");
        return sb.ToString();
    }

    public void WriteToFile(A2lDocument doc, string path)
    {
        var content = Write(doc);
        File.WriteAllText(path, content, new UTF8Encoding(true));
    }

    private static void WriteModule(StringBuilder sb, A2lModule m)
    {
        sb.AppendLine($" /begin MODULE {m.Name} \"{EscapeString(m.Comment)}\"");
        sb.AppendLine();
        foreach (var rl in m.RecordLayouts) WriteRecordLayout(sb, rl);
        foreach (var cm in m.CompuMethods) WriteCompuMethod(sb, cm);
        foreach (var meas in m.Measurements) WriteMeasurement(sb, meas);
        foreach (var ch in m.Characteristics) WriteCharacteristic(sb, ch);
        foreach (var ax in m.AxisPts) WriteAxisPts(sb, ax);
        foreach (var gr in m.Groups) WriteGroup(sb, gr);
        sb.AppendLine(" /end MODULE");
    }

    private static void WriteMeasurement(StringBuilder sb, A2lMeasurement m)
    {
        sb.AppendLine("  /begin MEASUREMENT");
        sb.AppendLine($"  /* Name                   */      {m.Name}");
        sb.AppendLine($"  /* Long Identifier        */      \"{EscapeString(m.LongIdentifier)}\"");
        sb.AppendLine($"  /* Data type              */      {m.DataType}");
        sb.AppendLine($"  /* Conversion method      */      {m.CompuMethod}");
        sb.AppendLine($"  /* Resolution             */      {m.Resolution}");
        sb.AppendLine($"  /* Accuracy               */      {m.Accuracy}");
        sb.AppendLine($"  /* Lower Limit            */      {m.LowerLimit}");
        sb.AppendLine($"  /* Upper Limit            */      {m.UpperLimit}");
        sb.AppendLine($"   ECU_ADDRESS                       0x{m.EcuAddress:X}");
        sb.AppendLine("  /end MEASUREMENT");
        sb.AppendLine();
    }

    private static void WriteCharacteristic(StringBuilder sb, A2lCharacteristic c)
    {
        sb.AppendLine("  /begin CHARACTERISTIC");
        sb.AppendLine($"  /* Name                   */      {c.Name}");
        sb.AppendLine($"  /* Long Identifier        */      \"{EscapeString(c.LongIdentifier)}\"");
        sb.AppendLine($"  /* Record Layout          */      {c.RecordLayout}");
        sb.AppendLine($"  /* ECU Address            */      0x{c.EcuAddress:X}");
        sb.AppendLine($"  /* Lower Limit            */      {c.LowerLimit}");
        sb.AppendLine($"  /* Upper Limit            */      {c.UpperLimit}");
        sb.AppendLine("  /end CHARACTERISTIC");
        sb.AppendLine();
    }

    private static void WriteAxisPts(StringBuilder sb, A2lAxisPts a)
    {
        sb.AppendLine("  /begin AXIS_PTS");
        sb.AppendLine($"  /* Name                   */      {a.Name}");
        sb.AppendLine($"  /* Long Identifier        */      \"{EscapeString(a.LongIdentifier)}\"");
        sb.AppendLine($"  /* Record Layout          */      {a.RecordLayout}");
        sb.AppendLine($"  /* ECU Address            */      0x{a.EcuAddress:X}");
        sb.AppendLine($"  /* Input Quantity         */      {a.InputQuantity}");
        sb.AppendLine($"  /* Conversion Method      */      {a.CompuMethod}");
        sb.AppendLine($"  /* Number of Axis Pts     */      {a.NumberOfAxisPts}");
        sb.AppendLine($"  /* Lower Limit            */      {a.LowerLimit}");
        sb.AppendLine($"  /* Upper Limit            */      {a.UpperLimit}");
        sb.AppendLine("  /end AXIS_PTS");
        sb.AppendLine();
    }

    private static void WriteCompuMethod(StringBuilder sb, A2lCompuMethod cm)
    {
        sb.AppendLine("  /begin COMPU_METHOD");
        sb.AppendLine($"  /* Name                   */      {cm.Name}");
        sb.AppendLine($"  /* Long Identifier        */      \"{EscapeString(cm.LongIdentifier)}\"");
        sb.AppendLine($"  /* Conversion Type        */      {cm.ConversionType}");
        sb.AppendLine($"  /* Format                 */      \"{cm.Format}\"");
        sb.AppendLine($"  /* Units                  */      \"{EscapeString(cm.Unit)}\"");
        sb.AppendLine($"  /* Coefficients           */      COEFFS {cm.CoeffA.ToString(CultureInfo.InvariantCulture)} {cm.CoeffB.ToString(CultureInfo.InvariantCulture)} {cm.CoeffC.ToString(CultureInfo.InvariantCulture)} {cm.CoeffD.ToString(CultureInfo.InvariantCulture)} {cm.CoeffE.ToString(CultureInfo.InvariantCulture)} {cm.CoeffF.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine("  /end COMPU_METHOD");
        sb.AppendLine();
    }

    private static void WriteRecordLayout(StringBuilder sb, A2lRecordLayout rl)
    {
        sb.AppendLine($"  /begin RECORD_LAYOUT {rl.Name}");
        foreach (var e in rl.Entries)
            sb.AppendLine($"   {e.Keyword} {e.Position} {e.DataType} {e.IndexMode} {e.AddressingMode}");
        sb.AppendLine("  /end RECORD_LAYOUT");
        sb.AppendLine();
    }

    private static void WriteGroup(StringBuilder sb, A2lGroup g)
    {
        sb.AppendLine($"  /begin GROUP {g.Name} \"{EscapeString(g.LongIdentifier)}\" {(g.IsRoot ? "ROOT" : "")}");
        if (g.RefMeasurements.Count > 0)
        {
            sb.AppendLine("   /begin REF_MEASUREMENT");
            foreach (var r in g.RefMeasurements) sb.AppendLine($"    {r}");
            sb.AppendLine("   /end REF_MEASUREMENT");
        }
        if (g.RefCharacteristics.Count > 0)
        {
            sb.AppendLine("   /begin REF_CHARACTERISTIC");
            foreach (var r in g.RefCharacteristics) sb.AppendLine($"    {r}");
            sb.AppendLine("   /end REF_CHARACTERISTIC");
        }
        sb.AppendLine("  /end GROUP");
        sb.AppendLine();
    }

    private static string EscapeString(string s) => s.Replace("\"", "\\\"");
}
