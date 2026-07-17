# a2l-editor v0.4

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
- **MOD_PAR / MOD_COMMON / BYTE_ORDER parsing** — parsed into model (`A2lModule.ModPar`, `A2lDocument.ModCommon`); Writer round-trips both; `BmsModel.a2l` parses with **0 errors, 0 warnings**.
- **Full round-trip fidelity for all block types** — `A2lDocumentWriter` now emits MEASUREMENT, CHARACTERISTIC, AXIS_PTS, COMPU_METHOD, RECORD_LAYOUT, GROUP, MOD_PAR, MOD_COMMON with all fields. `BmsModel.a2l` round-trips through write → re-parse with semantic equality (45 RECORD_LAYOUTs preserved, sample entry fields exact). Closes v0.3 deferred item + 2 v0.3 Skip tests revived.
- **String literal quote escaping** — `StringLiteralEscaper` mirrors Asap131Lexer.ReadString in reverse; `Write_EscapesQuotesInStrings` Skip test revived.

## Tests

87 passing + 0 skip across 3 test projects:

- `A2lEditor.Core.Tests` — 68 (Parser / Lexer / Writer / Validator / TokenClassifier / RecentFilesStore + MOD_PAR/MOD_COMMON + StringLiteralEscaper + full Writer content for all 7 record types)
- `A2lEditor.App.Tests` — 14 (ViewModel + A2lTextEditor navigation, unchanged from v0.2)
- `A2lEditor.IntegrationTests` — 5 (CLI validate exit codes 0/1/2 + BmsModel 0-warnings acceptance + BmsModel full round-trip semantic equality)

## Deferred to v0.5+

The following are intentionally not claimed as v0.4 functionality:

- Drag-and-drop file open
- Full menu (Edit / View / Tools / Help)
- Custom `UtfUnknown` package integration
- Coverage threshold enforcement (parse coverage.cobertura.xml)
- Debounce tree rebuild on text change
- Byte-for-byte round-trip fidelity (whitespace / comment / format order)
- `AXIS_DESCR` / `USER_RIGHTS` / `VERSION` and other project-level blocks
- `MOD_COMMON` `DATA_SIZE` / `ALIGNMENT_BYTE_ORDER` sub-fields
- `MOD_PAR` / `MOD_COMMON` multi-line comments
- `BYTE_ORDER` in A2lValidator (currently parse + persist, no constraint check)
- `AXIS_PTS_X` / `INDEX_INCR` / `INDEX_DECR` parsing
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

# Run all tests (87 PASS + 0 SKIP expected)
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
- [Plan v0.2](docs/superpowers/plans/2026-07-16-a2l-editor-v0-2-ux.md)
- [Plan v0.3](docs/superpowers/plans/2026-07-17-a2l-editor-v0-3-parser.md)
- [Plan v0.4](docs/superpowers/plans/2026-07-17-a2l-editor-v0-4-roundtrip.md)

## License

Internal use only.
