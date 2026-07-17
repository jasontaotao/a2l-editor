# a2l-editor v0.7

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
- **Full round-trip fidelity for all block types** — `A2lDocumentWriter` now emits MEASUREMENT, CHARACTERISTIC, AXIS_PTS, COMPU_METHOD, RECORD_LAYOUT, GROUP, MOD_PAR, MOD_COMMON with all fields. `BmsModel.a2l` round-trips through write → re-parse with semantic equality (45 RECORD_LAYOUTs preserved, sample entry fields exact).
- **String literal quote escaping** — `StringLiteralEscaper` mirrors Asap131Lexer.ReadString in reverse.
- **`AXIS_DESCR` / `USER_RIGHTS` / `VERSION` parsing** — parsed into 3 new `A2lModule` list fields. Writer emits all 3 with full content. Closes 1 v0.4 deferred item.
- **`MOD_COMMON` `DATA_SIZE` / `ALIGNMENT_BYTE_ORDER` sub-fields** — parsed into 2 new nullable `A2lModCommon` fields. Writer emits optional lines when non-null. Closes 1 v0.4 deferred item.
- **Multi-line string literal support** — `Asap131Lexer` L130 already supported multi-line; 1 regression test locks the invariant. Closes 1 v0.4 deferred item.
- **`BYTE_ORDER` validator warning** — `A2lValidator` emits Warning "Non-MSB_LAST byte order may not be supported by all ECUs" when `ModCommon.ByteOrder == MSB_FIRST`. Closes 1 v0.4 deferred item.
- **`AXIS_PTS_X` / `INDEX_INCR` / `INDEX_DECR` parsing** — parsed into `A2lModule.AxisPts` and `A2lModule.RecordLayouts[*].Entries`. Writer emits all 3 with full content. Closes 1 v0.5 deferred item.
- **`MOD_COMMON` `ALIGNMENT_OFFSET` sub-field** — parsed into a new nullable `A2lModCommon` field. Writer emits the optional line when non-null. Closes 1 v0.5 deferred item.
- **`VERSION` duplicate block detection** — `A2lValidator` errors on duplicate `VERSION` blocks in a module. Closes 1 v0.5 deferred item.
- **`MOD_PAR` / `MOD_COMMON` truly-multi-line output** — Writer now emits PROJECT / HEADER / MODULE / MOD_PAR / MOD_COMMON with the helper that emits strings verbatim when they contain newlines (escape-style when single-line). Lock test `BmsModel_RoundTrip_PreservesCommentsAndNoNewlines` confirms zero `\n` drift for BmsModel. Closes 1 v0.5 deferred item.
- **Drag-and-drop file open** — drag `.a2l` files from File Explorer onto the MainWindow to open them; visual feedback (DodgerBlue border) during drag-over. Closes 1 v0.6 deferred item.
- **Full menu (Edit / View / Tools / Help)** — 5 top-level menus with 20 sub-items (File: 6 + Edit: 7 + View: 4 + Tools: 2 + Help: 1). Undo/Redo/Find/Format are stub-only ("Not implemented in v0.7"). Closes 1 v0.6 deferred item.
- **Debounce tree rebuild on text change** — `DispatcherTimer(200ms)` prevents excessive tree rebuilds when typing rapidly; tree rebuilds once after 200ms of no further text changes. Closes 1 v0.6 deferred item.

## Tests

116 passing + 0 skip across 3 test projects:

- `A2lEditor.Core.Tests` — 87 (Parser / Lexer / Writer / Validator / TokenClassifier / RecentFilesStore + MOD_PAR/MOD_COMMON + StringLiteralEscaper + full Writer content + AXIS_DESCR/USER_RIGHTS/VERSION + MOD_COMMON sub-fields + multi-line verify + AXIS_PTS_X/INDEX_INCR/INDEX_DECR + ALIGNMENT_OFFSET + VERSION duplicate)
- `A2lEditor.App.Tests` — 23 (14 baseline from v0.2 + 9 new in v0.7: drag-and-drop 4 + menu 3 + debounce 2; note implementer deviations — drag-drop landed 4 tests not 3, no `[StaFact]` pattern used, `OpenRecent` stays synchronous not async). App tests run with `parallelizeTestCollections: false` (via `xunit.runner.json`) to avoid a WPF `PackagePart` resource-loader race.
- `A2lEditor.IntegrationTests` — 6 (CLI validate exit codes 0/1/2 + BmsModel 0-warnings acceptance + BmsModel full round-trip semantic equality + BmsModel multi-line lock)

## Deferred to v0.8+

The following are intentionally not claimed as v0.7 functionality:

- Custom `UtfUnknown` package integration
- Coverage threshold enforcement (parse coverage.cobertura.xml)
- Byte-for-byte round-trip fidelity (whitespace / comment / format order)
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

# Run all tests (116 PASS + 0 SKIP expected)
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
- [Plan v0.5](docs/superpowers/plans/2026-07-17-a2l-editor-v0-5-parser-followup.md)
- [Plan v0.6](docs/superpowers/plans/2026-07-17-a2l-editor-v0-6-parser-followup.md)
- [Plan v0.7](docs/superpowers/plans/2026-07-17-a2l-editor-v0-7-ui-trio.md)

## License

Internal use only.
