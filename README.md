# a2l-editor v0.2

Desktop GUI + CLI for working with ASAP2 (`.a2l`) files.

## Verified functionality

The following functionality is implemented and covered by the current test suite:

- Open and save `.a2l` files.
- Parse the implemented ASAP2 1.31 subset.
- Validate files with the CLI command `a2l-editor validate <file>`.
- Launch the WPF GUI shell.
- **Custom A2L syntax highlighting** — keywords (block / structural / data type), identifiers, strings, numbers, comments — colored per ASAP2 1.31 grammar via `Asap131Lexer` reuse.
- **Error window with double-click navigation** — bottom-panel Expander; auto-expands once on first error batch; double-click an error row to jump to its line in the editor.
- **Recent files menu** — last 8 opened `.a2l` files; persisted to `%APPDATA%\a2l-editor\recent.json`; clicking a missing file shows an error and auto-removes it from the list.
- **Tree node click-to-jump** — click any `MEASUREMENT` or `CHARACTERISTIC` row in the left-hand tree to jump to its source line + 0.5 s background highlight.

## Tests

66 passing across 3 test projects:

- `A2lEditor.Core.Tests` — 49 (Parser / Lexer / Writer / Validator / TokenClassifier / RecentFilesStore)
- `A2lEditor.App.Tests` — 14 (ViewModel + A2lTextEditor navigation)
- `A2lEditor.IntegrationTests` — 3 (CLI validate exit codes 0/1/2)

## Deferred to v0.3+

The following are intentionally not claimed as v0.2 functionality:

- Drag-and-drop file open
- Full menu (Edit / View / Tools / Help)
- Custom `UtfUnknown` package integration
- Coverage threshold enforcement (parse coverage.cobertura.xml)
- Debounce tree rebuild on text change
- `MOD_COMMON` / `BYTE_ORDER` parsing (currently emitted as warnings)
- `SkipToMatchingEnd` residual block-leak fix (Plan v0.1.1 `verify-bug.md` Risks #1)
- MAP/ELF alignment (planned as v0.2 core feature in original spec; deferred)
- Excel import → A2L skeleton generation
- A2L merge / diff

## Tech stack

- .NET 8
- WPF
- AvalonEdit 6.x
- CommunityToolkit.Mvvm
- C# 12
- xUnit + FluentAssertions + Moq

## Quick start

```powershell
# Build
dotnet build a2l-editor.sln -c Release

# Run all tests (66 PASS expected)
dotnet test a2l-editor.sln --nologo

# Launch the WPF GUI
dotnet run --project src/A2lEditor.App

# CLI validate
dotnet src/A2lEditor.Cli/bin/Release/net8.0/a2l-editor.dll validate samples/BmsModel.a2l
```

A self-contained Windows x64 executable (~65 MB) is produced at `publish/a2l-editor.exe` by `scripts/package.ps1` (or `dotnet publish` directly — see `scripts/package.ps1` body).

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
