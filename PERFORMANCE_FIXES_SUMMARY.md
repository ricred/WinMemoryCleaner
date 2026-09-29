# Performance Fix Implementation Summary

> **Status: COMPLETE** — all findings implemented and committed
> (`bda9c6a` minute-granularity auto-optimization; `43d1574` performance quick wins).
> Branch: `performance-fixes`.

## Implemented Fixes

### 1. Settings.SaveAsync() Debounce (src/Core/Settings.cs)
Replaced synchronous `Settings.Save()` calls in 29 ViewModel property setters with a
debounced background save. **Final implementation** uses a re-armable
`System.Threading.Timer` (500 ms one-shot, re-armed on every call):

- Rapid changes collapse into a single registry write 500 ms after the last change
- Values are read at fire time (latest state persisted; no per-call value capture)
- Zero parked threadpool threads (the earlier CTS + `QueueUserWorkItem` draft parked
  one thread per click — replaced)
- Synchronous `Save()` unchanged for shutdown, tests, and static constructor

### 2. ComputerService Handle & Allocation Leaks
- **GCHandle leaks fixed**: `OptimizeWorkingSet` / `OptimizeSystemFileCache` allocated
  pinned `GCHandle.Alloc(0)` handles that were never freed; now properly released
- `Memory` getter reuses a single `MemoryStatusEx` instance instead of allocating on
  every 500 ms monitor tick

### 3. NotificationService Tray Icon
- 4 rotation animation frames pre-rendered once and reused; `IsRotationFrame`
  dispose-guard prevents icon swaps from freeing shared frames
- Per-update string icon-state key replaced with 12-field struct compare
  (`IsIconStateUnchanged` / `RememberIconState`); null fingerprint forces re-render
- `VirtualMemoryHeader` raise gated on actual value change

### 4. MainViewModel Binding Traffic
- `Computer` property raise reference-gated (in-place memory refresh keeps the same
  reference; `MemorySize` scalar bindings stay live via INPC)
- Removed unconditional `Bytes` PropertyChanged raise from `MemorySize` setters
  (verified no INPC subscribers; XAML binds to scalar properties OneWay)

### 5. Process List Caching (src/ViewModel/MainViewModel.cs)
- `Processes` cached with 10 s TTL; `RefreshProcesses()` invalidates at the three
  freshness sites: exclusion added, exclusion removed, dropdown opened
- Dropped redundant `OrdinalIgnoreCase` comparers (SortedSet already ignores case)
  and dead `.Replace(".exe", "")` call

### 6. Privilege Memoization + Handle Hygiene
- `SetIncreasePrivilege` memoized in a `HashSet` (was re-invoked per memory area per
  optimization)
- `GetCurrentProcess()` pseudo-handle usage in `App.ReleaseMemory` wrapped in `using`
  with `CloseHandle` on the duplicated real handle

## Explicitly Deferred (not implemented)

1. Remove `IsBusy` blocking from 25+ MainViewModel setters (separate UX decision)
2. ObservableCollection persistence — `Brushes` / `MemoryAreaItems` still recreated
   on access (measured cost acceptable)
3. LINQ HashSet lookups for exclusion checks (superseded by #5 process cache)
4. Recursion prevention in `ObservableItem.Value` setter (no repro observed)

## Verification

- MSBuild Release (VS 2022): 0 errors, 0 warnings (`TreatWarningsAsErrors` enabled)
- Placeholder/corruption scan across all .cs files: 0 hits
- Tests updated for INPC/cache behavior changes (NUnit; admin-gated where registry
  writes to HKLM are required)

## Build & Test

- `build_release.bat` — MSBuild Release via VS 2022 toolchain (use this; `dotnet build`
  does not work with this old-style .NET Framework 4.0 WPF project)
- `test_release.bat` — builds output path, launches exe with manual test checklist
