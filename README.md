# a2l-editor v0.1.1

Desktop GUI + CLI for working with ASAP2 (`.a2l`) files.

## Verified functionality

The following functionality is implemented and covered by the current test suite:

- Open and save `.a2l` files.
- Parse the implemented ASAP2 1.31 subset.
- Validate files with the CLI command `a2l-editor validate <file>`.
- Launch the WPF GUI shell.

The v0.1.1 parser fixes are verified by the parser and project tests. The GUI tree is populated when a file is opened; editing text does not provide the deferred v0.2 interaction features listed below.

## Deferred to v0.2

The following are intentionally not claimed as v0.1.1 functionality:

- Syntax highlighting.
- An error window or severity-badged error list.
- Recent files.
- Drag-and-drop opening.
- Tree-node click-to-jump/highlight in the editor.
- MAP/ELF alignment.
- Excel import.
- A2L merge.

See the [v0.2 backlog](docs/superpowers/plans/2026-07-16-a2l-editor-v0-1-1.md#v02-backlog-explicit-deferred-items) for the broader deferred scope.

## Tech stack

- .NET 8
- WPF
- AvalonEdit 6.x
- CommunityToolkit.Mvvm
- C# 12
- xUnit and Verify for tests

## Quick start

```powershell
# Build
dotnet build a2l-editor.sln -c Release

# Run all tests
dotnet test a2l-editor.sln --nologo

# Launch the WPF GUI
dotnet run --project src/A2lEditor.App

# Validate an A2L file with the CLI
dotnet run --project src/A2lEditor.Cli -- validate samples/BmsModel.a2l
```

CLI validation uses exit code `0` for a valid file, `1` when parse or validation errors are present, and `2` when the file cannot be found or read.

## Project layout

See [docs/architecture.md](docs/architecture.md) for the high-level architecture and the [Plan v0.1.1 architecture section](docs/superpowers/plans/2026-07-16-a2l-editor-v0-1-1.md#task-19-readme-docs-tag--revised-honest-scope).

- `src/A2lEditor.Core` — parser, model, serializer, and validator.
- `src/A2lEditor.App` — WPF GUI shell.
- `src/A2lEditor.Cli` — CLI validation command.
- `tests/` — unit and integration tests.
- `samples/` — sample A2L files.

## Plans

- [Plan v0.1](docs/superpowers/plans/2026-07-16-a2l-editor-v0-1.md)
- [Plan v0.1.1](docs/superpowers/plans/2026-07-16-a2l-editor-v0-1-1.md)

## License

Internal use only.
