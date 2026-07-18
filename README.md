# a2l-editor v0.20

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
- **Full menu (Edit / View / Tools / Help)** — 5 top-level menus with 20 sub-items (File: 6 + Edit: 7 + View: 4 + Tools: 2 + Help: 1). Undo/Redo via AvalonEdit UndoStack, Find via AvalonEdit SearchPanel. Format is stub-only ("Not implemented in v0.7"). Closes 1 v0.6 deferred item.
- **Debounce tree rebuild on text change** — `DispatcherTimer(200ms)` prevents excessive tree rebuilds when typing rapidly; tree rebuilds once after 200ms of no further text changes. Closes 1 v0.6 deferred item.
- **`UtfUnknown` BOM detection at file open** — `A2lDocument.LoadFromFile` auto-detects UTF-8 / UTF-16 LE/BE / UTF-32 via UtfUnknown NuGet; ASCII upgrades to UTF-8; BOM stripped before parser handoff to keep `RawText` BOM-free. Closes 1 v0.7 deferred item.
- **Coverage threshold enforcement** — `coverlet.collector` produces Cobertura XML; `CoberturaReport` parser + `verify-coverage.ps1` enforces 80% line / 70% branch gate. Pre-commit hook at `scripts/pre-commit` runs the gate before every commit. Closes 1 v0.7 deferred item.
- **Byte-for-byte round-trip fidelity** — `A2lDocumentWriter` prioritizes `doc.RawText` emit when the parser ran on the same file; 1:1 read→save preserves the original source byte-for-byte (no whitespace / comment / format drift). Closes 1 v0.7 deferred item.
- **`A2lEditor.Reuse` linked-source wrapper** — 3 `.cs` files linked from legacy `A2L_UpdateProj/` (`MapSymbolTable` + `MapFormatDetector` + ELF format delegation via ELFSharp 2.17.3); closes the 6-version "MAP/ELF alignment" zombie deferred from v0.1 spec 3.3. Legacy project frozen, zero modifications.
- **CLI `map <dump-symbols|update|validate>` commands** — batch ECU address updates from MAP/ELF files. Default dry-run, optional `--backup`, `--output`. Supports IAR / HighTech / GCC / ELF32 formats via legacy `MapFormatDetector`.
- **WPF `Tools > Apply MAP...` menu entry** — interactive MAP apply with coverage preview dialog (matched/missing/extra counts) and confirm prompt before write.
- **Structured A2L diff** — `a2l-editor diff compare <file1> <file2>` CLI command for structural comparison of two `.a2l` files. Uses C# record equality for block-level name-based matching; detects Added/Removed/Modified/Unchanged blocks with field-level detail via `A2lDiffService`. Supports `--brief` and `--unchanged` flags.
- **Parser bugfix (v0.11)** — two critical bugs fixed that blocked diff on real A2L files: `ECU_ADDRESS` hex number not consumed (skipped all subsequent blocks),
  and negative limit values (`-40`) not tokenized as `Number` (silently corrupted module parse state).
- **WPF diff viewer** — `Tools > Diff Files...` menu entry opens a modal dialog for file selection and structured diff report display. Provides Copy Report and status bar feedback. Covers all 11 block types with the same report format as the CLI.
- **Two-way A2L merge** — `merge apply <baseline> <modified>` CLI command with compared-wins strategy (Modified/Added → use compared version, Unchanged/Removed → keep baseline). Supports `--dry-run` and `--output`. WPF diff dialog includes `Save Merged...` button.
- **Excel → A2L skeleton generation** — `skeleton generate <excel.xlsx>` CLI command. Reads signal definitions from Excel (.xlsx via ClosedXML) and generates a complete .a2l file with MEASUREMENT, CHARACTERISTIC, and auto-generated COMPU_METHOD stubs. Supports `--sheet`, `--module`, `--comment`, `--output` options.
- **WPF skeleton import dialog** — `Tools > Import from Excel...` menu entry opens a modal dialog with interactive file selection, preview of measurement/characteristic counts, and Save As workflow. Follows the same pattern as the diff/merge dialog.
- **XML/JSON serialization** — `IA2lDocumentSerializer` / `A2lDocumentSerializer` with JSON (via `System.Text.Json`) and XML (via LINQ to `XDocument`) round-trip serialization. JSON supports `JsonStringEnumConverter` for enums. XML builds/parses hierarchical `XElement` trees for all 14 model types. 8 round-trip tests lock fidelity.
- **A2L→Excel export** — `skeleton export <file.a2l>` CLI command. Writes MEASUREMENT and CHARACTERISTIC definitions back to `.xlsx` using ClosedXML, in the same column format as the skeleton import spec. Supports `--output` and `--sheet` options.
- **AXIS_DESCR 属性匹配** — diff/merge 中的 AXIS_DESCR 从索引位置匹配改为按 `Attribute` 字段匹配，消除因插入/删除条目导致的索引错位。`CompareBlockCollectionByIndex` / `MergeBlocksByIndex` 已废弃，由通用 name-keyed 方法替代。
- **交互式合并冲突解决** — `IA2lMergeService.Merge()` 新增可选 `acceptedChanges` 参数（`HashSet<string>`，键格式 `"BlockType:Name"`），仅应用用户接受的变更。WPF `DiffReportDialog` 的 "Save Merged..." 按钮改为先弹出 `MergeConflictDialog` 审查窗，显示所有 Modified/Added/Removed 变更列表，用户可逐个勾选跳过后执行合并。2 个 Core 测试 + 4 个 ChangeItem 测试覆盖。

## Tests

200 passing + 0 skip across 4 test projects:

- `A2lEditor.Core.Tests` — 146 (v0.18's 144 + 2 new: `acceptedChanges` merge filter tests)
- `A2lEditor.Cli.Tests` — 14 (unchanged from v0.18)
- `A2lEditor.App.Tests` — 34 (v0.18's 30 + 4 new: `MergeConflictDialog` / ChangeItem tests)
- `A2lEditor.IntegrationTests` — 6 (unchanged from v0.18)

## Deferred to v1.0+

The following are intentionally not claimed as v0.20 functionality:

(none — all deferred items from v0.1–v0.19 are now implemented)

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

# Run all tests (126 PASS + 0 SKIP expected)
dotnet test a2l-editor.sln --nologo

# Launch the WPF GUI
dotnet run --project src/A2lEditor.App

# CLI validate
dotnet src/A2lEditor.Cli/bin/Release/net8.0/a2l-editor.dll validate samples/BmsModel.a2l
```

A self-contained Windows x64 executable (~65 MB) is produced at `publish/a2l-editor.exe` by `scripts/package.ps1` (or `dotnet publish` directly — see `scripts/package.ps1` body).

## Pre-commit hook (coverage gate)

Install the coverage-gate pre-commit hook to fail commits that drop line coverage below 80% or branch coverage below 70%:

```bash
cp scripts/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

The hook invokes `scripts/verify-coverage.ps1` against the most recent `coverage.cobertura.xml` produced by `dotnet test --collect:"XPlat Code Coverage"`. Skip with `git commit --no-verify` when needed.

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
- [Plan v0.8](docs/superpowers/plans/2026-07-17-a2l-editor-v0-8-tooling.md)

## License

Internal use only.
