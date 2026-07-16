# Architecture

This document is a high-level pointer for the v0.1.1 implementation. The complete design and the Task 19 architecture details are in the [Plan v0.1.1](superpowers/plans/2026-07-16-a2l-editor-v0-1-1.md#task-19-readme-docs-tag--revised-honest-scope).

## Layers

- `A2lEditor.Core` (`netstandard2.1`): parser, model, serializer, and validator.
- `A2lEditor.App` (`net8.0-windows`): WPF GUI shell using AvalonEdit and CommunityToolkit.Mvvm.
- `A2lEditor.Cli` (`net8.0`): CLI validation command.

## Data flow

- Open: file dialog -> Core parser -> ViewModel raw text and parsed tree.
- Save: editor text -> Core parser/writer -> output file.
- Validate: CLI path -> Core parser and validator -> exit code.

The v0.1.1 GUI tree is populated on open. The deferred editor navigation, syntax highlighting, and other v0.2 interactions are not part of this release.

## Verification

Run the complete solution test suite with:

```powershell
dotnet test a2l-editor.sln --nologo
```
