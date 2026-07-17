using System.IO;
using System.Linq;
using System.Text;
using A2lEditor.Core.Model;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.BomDetection;

public class UtfUnknownIntegrationTests
{
    [Fact]
    public void LoadFromFile_Utf8Bom_DetectsUtf8()
    {
        var path = Path.GetTempFileName();
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }  // UTF-8 BOM
            .Concat(Encoding.UTF8.GetBytes("ASAP2_VERSION 1 61\n"))
            .ToArray();
        File.WriteAllBytes(path, bytes);
        try
        {
            var doc = A2lDocument.LoadFromFile(path);
            doc.Should().NotBeNull();
            doc.Version.Should().Be(A2lVersion.V1_6x);
            doc.RawText.Should().StartWith("ASAP2_VERSION");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_Utf16LeBom_DetectsUtf16Le()
    {
        var path = Path.GetTempFileName();
        var bytes = new byte[] { 0xFF, 0xFE }  // UTF-16 LE BOM
            .Concat(Encoding.Unicode.GetBytes("ASAP2_VERSION 1 61\n"))
            .ToArray();
        File.WriteAllBytes(path, bytes);
        try
        {
            var doc = A2lDocument.LoadFromFile(path);
            doc.RawText.Should().Contain("ASAP2_VERSION");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_NoBom_DefaultsToUtf8()
    {
        var path = Path.GetTempFileName();
        // ASCII file with no BOM
        File.WriteAllText(path, "ASAP2_VERSION 1 31\n", new UTF8Encoding(false));
        try
        {
            var doc = A2lDocument.LoadFromFile(path);
            doc.RawText.Should().StartWith("ASAP2_VERSION");
            doc.Version.Should().Be(A2lVersion.V1_31);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
