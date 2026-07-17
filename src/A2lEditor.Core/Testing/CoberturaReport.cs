using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace A2lEditor.Core.Testing;

public sealed record CoverageStats(
    double LineRate,
    double BranchRate,
    int LinesCovered,
    int LinesValid,
    int BranchesCovered,
    int BranchesValid);

public static class CoberturaReport
{
    public static CoverageStats Parse(string xmlPath)
    {
        if (!File.Exists(xmlPath))
            throw new InvalidOperationException($"Coverage report not found: {xmlPath}");

        var doc = XDocument.Load(xmlPath);
        var root = doc.Root ?? throw new InvalidOperationException("Empty coverage XML");

        double lineRate = ParseDoubleAttr(root, "line-rate");
        double branchRate = ParseDoubleAttr(root, "branch-rate");
        int linesCovered = ParseIntAttr(root, "lines-covered");
        int linesValid = ParseIntAttr(root, "lines-valid");
        int branchesCovered = ParseIntAttr(root, "branches-covered");
        int branchesValid = ParseIntAttr(root, "branches-valid");

        return new CoverageStats(lineRate, branchRate, linesCovered, linesValid, branchesCovered, branchesValid);
    }

    public static bool MeetsThreshold(CoverageStats stats, double lineThreshold, double branchThreshold)
        => stats.LineRate >= lineThreshold && stats.BranchRate >= branchThreshold;

    private static double ParseDoubleAttr(XElement el, string name)
    {
        var attr = el.Attribute(name);
        return attr != null && double.TryParse(attr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0.0;
    }

    private static int ParseIntAttr(XElement el, string name)
    {
        var attr = el.Attribute(name);
        return attr != null && int.TryParse(attr.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
