# Build Success - WinMemoryCleaner Release Build

## Status: ✅ BUILD SUCCESSFUL

**Build Output:** `C:\Data\WinMemoryCleaner\src\bin\Release\WinMemoryCleaner.exe`
**Build Command:** `build_release.bat` (VS 2022 MSBuild, Release)
**Last verified:** 2026-09-29 — 0 errors, 0 warnings (`TreatWarningsAsErrors` enabled)

## Build Toolchain

- **Use:** Visual Studio 2022 MSBuild (`build_release.bat`) or open the `.sln` in VS 2022
- **Do NOT use:** `dotnet build` — the project is an old-style .NET Framework 4.0 WPF
  project; the SDK CLI cannot compile its XAML pipeline (`InitializeComponent`,
  `static Main` errors). Earlier reports of "build blockers" were a toolchain choice
  issue, not a code issue.

## Output Files

```
C:\Data\WinMemoryCleaner\src\bin\Release\
├── WinMemoryCleaner.exe          ~477 KB - Main executable
├── WinMemoryCleaner.pdb                  - Debug symbols
├── WinMemoryCleaner.xml                  - XML documentation
└── nunit.framework.dll                   - Test framework (test classes compile in)
```

## Included Changes (branch `performance-fixes`)

- `bda9c6a` feat(auto-optimization): minute-granularity interval with legacy-hours migration
- `43d1574` perf: reduce idle allocations and redundant work across tick paths

See `PERFORMANCE_FIXES_SUMMARY.md` for the full change list and `CHANGELOG.md`
for the version history.

## Quick Test Checklist

1. `test_release.bat` (builds then launches the app)
2. Rapidly toggle checkboxes — UI stays responsive; settings persist after ~500 ms
3. Close & reopen — verify persisted settings (incl. migrated auto-optimization interval)
4. Auto-optimization slider — 10 min to 168 h range, 10-min ticks
5. Tray icon — rotation during optimization, color transitions at danger/warning levels
6. Process dropdown — reflects exclusion list edits immediately (cache invalidation)
