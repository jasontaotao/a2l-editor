using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using A2lEditor.Core.Parsing;
using A2lEditor.Core.Validation;

namespace A2lEditor.Cli.Commands;

public static class ValidateCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<string>("file", "Path to .a2l file");
        var jsonOpt = new Option<bool>("--json", "Output errors as JSON");
        var cmd = new Command("validate", "Validate an .a2l file") { fileArg, jsonOpt };

        // FIX 1 (Plan v0.1.1 amendment): System.CommandLine 2.0.0-beta4 does NOT
        // provide `SetHandler(Func<T1, T2, Task<int>>)`. The actual async overloads
        // only return `Func<T1, T2, Task>` (no exit code) or
        // `Func<InvocationContext, Task>` (gives access to `context.ExitCode`).
        //
        // The verbatim v0.1 brief used `Action<string, bool>` which silently
        // dropped the returned exit code (process always exited 0). The Plan
        // v0.1.1 step 3 fix proposed `async (string file, bool json) => { ...
        // return exitCode; }`, but the C# compiler infers that as
        // `Func<string, bool, Task>` (no int) because the 2.0 beta4 overload set
        // contains no `Func<T1, T2, Task<int>>`. To actually propagate the exit
        // code, we must take the InvocationContext and set context.ExitCode
        // explicitly — the official beta4 pattern.
        cmd.SetHandler((InvocationContext context) =>
        {
            var file = context.ParseResult.GetValueForArgument(fileArg);
            var json = context.ParseResult.GetValueForOption(jsonOpt);

            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"File not found: {file}");
                context.ExitCode = 2;
                return Task.CompletedTask;
            }

            var result = Asap131Parser.ParseFile(file);

            // FIX 3 (Plan v0.1.1): explicit null check before dereferencing
            // `result.Value`. The Parser returns `Failure` (Value = null) on
            // fatal errors. Calling `result.Value!` blindly would either NRE
            // here or — worse — leak null into the validator and surface as a
            // confusing NullReferenceException deep inside Validate().
            if (result.Value is null)
            {
                var msg = result.Errors.Count > 0
                    ? string.Join("\n", result.Errors.Select(e => $"L{e.Line}: {e.Message}"))
                    : "Empty or invalid A2L document.";
                Console.Error.WriteLine(msg);
                context.ExitCode = 1;
                return Task.CompletedTask;
            }

            var validation = new A2lValidator().Validate(result.Value);
            var allErrors = result.Errors.Concat(validation).ToList();

            if (json)
                Console.WriteLine(JsonSerializer.Serialize(allErrors));
            else
                foreach (var e in allErrors)
                    Console.WriteLine($"{e.Severity} L{e.Line}:C{e.Column} {e.Message}");

            context.ExitCode =
                allErrors.Any(e => e.Severity >= ErrorSeverity.Error) ? 1 : 0;
            return Task.CompletedTask;
        });

        return cmd;
    }
}

internal static class RootCommandHandler
{
    // Kept for backward reference; the actual --version option is provided by
    // System.CommandLine 2.0 beta4's built-in middleware. The csproj sets
    // `<Version>0.1.1</Version>` and `<InformationalVersion>0.1.1</...>` so the
    // built-in prints "0.1.1" when invoked.
    public const string VersionNumber = "0.1.1";

    // FIX 2 (Plan v0.1.1) marker: --version is a no-argument flag (Option<bool>
    // semantically) in System.CommandLine 2.0 beta4's built-in. The verbatim v0.1
    // brief used `Option<string>` which made `--version` demand a value — a
    // runtime misbehavior caught by the Plan v0.1.1 audit.
}