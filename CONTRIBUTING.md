# Contributing

Thanks for taking an interest. This is a small solo project, but PRs are welcome and I'll review them.

## Quick start

```bash
git clone --recurse-submodules https://github.com/XeldarAlz/FFXIV-AutoPVPSeriesGrind.git
cd FFXIV-AutoPVPSeriesGrind
dotnet build AutoPvpSeriesGrind.sln -c Release
```

You need the .NET 10 SDK. The plugin requires Dalamud at runtime; CI pulls a Dalamud dev build automatically and that's enough to compile. See `.github/workflows/release.yml` if you want to reproduce CI locally.

Load the built plugin via `/xlsettings` -> **Experimental** -> **Dev Plugin Locations**, pointing at `AutoPvpSeriesGrind/bin/Release/AutoPvpSeriesGrind/AutoPvpSeriesGrind.dll`.

## Project layout

- `AutoPvpSeriesGrind/Core/`: match-loop state machine, game/duty operations, Limit Break catalog, IPC adapters.
- `AutoPvpSeriesGrind/Windows/`: ImGui main window, settings, dependencies.
- `AutoPvpSeriesGrind/`: plugin entry points, config, command wiring.
- `ECommons/`: submodule, shared Dalamud helpers. Don't patch this directly; upstream it.

Keep logic small and direct. This plugin has one job.

## Before you open a PR

1. `dotnet build -c Release` cleanly.
2. Test in-game across at least one full match for the area you touched. Crystalline Conflict maps differ (spawn sides, objective layout), so a fix that works on one map may not work on another.
3. Keep the diff focused. One concern per PR.
4. Match the existing style. No heavy abstractions "for later."
5. If your change affects what a user sees or types (commands, window layout, settings), update the README.

## Good first issues

Check the tracker for anything labeled `good first issue`. Map-specific quirks (a spawn anchor that doesn't line up, a Limit Break name that changed) are usually the lowest-friction way to help: pick a case that's misbehaving, attach a log of what the plugin did vs. what should have happened, and a fix is usually a small change.

## Security

Please don't file public issues for security problems; see [SECURITY.md](SECURITY.md).

## Code of conduct

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Be decent.

## License

By contributing, you agree your contributions are licensed under AGPL-3.0-or-later, the same as the project.
