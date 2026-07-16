using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace A2lEditor.IntegrationTests;

public class CliValidateTests
{
    // Repo root is four directories up from this test project's bin folder:
    //   tests/A2lEditor.IntegrationTests/bin/Debug/net8.0/  → ../../../../  → a2l-editor/
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static (int ExitCode, string StdOut, string StdErr) RunCli(string args)
    {
        var cliProject = Path.GetFullPath(Path.Combine(RepoRoot, "src", "A2lEditor.Cli"));
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{cliProject}\" --no-build -- {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot,
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return (p.ExitCode, p.StandardOutput.ReadToEnd(), p.StandardError.ReadToEnd());
    }

    [Fact]
    public void Validate_ValidFile_ExitsZero()
    {
        // Build the CLI once so --no-build is meaningful.
        BuildCli();

        var sample = Path.Combine(RepoRoot, "samples", "BmsModel.a2l");
        var result = RunCli($"validate \"{sample}\"");
        result.ExitCode.Should().Be(0, "valid BmsModel.a2l must produce exit code 0");
    }

    [Fact]
    public void Validate_InvalidFile_ExitsOne()
    {
        BuildCli();

        var sample = Path.Combine(RepoRoot, "samples", "invalid-sample.a2l");
        var result = RunCli($"validate \"{sample}\"");
        result.ExitCode.Should().Be(1, "invalid-sample.a2l has parser errors → exit 1");
    }

    [Fact]
    public void Validate_MissingFile_ExitsTwo()
    {
        BuildCli();

        var result = RunCli("validate \"nonexistent-a2l-fixture.a2l\"");
        result.ExitCode.Should().Be(2, "missing file → exit 2");
    }

    private static int _cliBuilt;
    private static readonly object _buildLock = new();

    private static void BuildCli()
    {
        lock (_buildLock)
        {
            if (_cliBuilt == 1) return;
            var cliProject = Path.GetFullPath(Path.Combine(RepoRoot, "src", "A2lEditor.Cli"));
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{cliProject}\" -c Debug",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = RepoRoot,
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new InvalidOperationException(
                    $"CLI build failed (exit {p.ExitCode}):\n{p.StandardOutput.ReadToEnd()}\n{p.StandardError.ReadToEnd()}");
            Interlocked.Exchange(ref _cliBuilt, 1);
        }
    }
}