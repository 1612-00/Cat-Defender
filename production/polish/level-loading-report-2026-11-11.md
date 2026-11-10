# Performance Profile: Level Loading

Generated: 2026-11-11

## Scope and evidence

Scope: tapping an unlocked level in the Menu and waiting for Gameplay to appear.

| Input | Status | Notes |
|---|---|---|
| Menu and Gameplay source/scenes | FOUND | Traced the click handler, scene dependencies, and initialization code. |
| User timing | FOUND | User observes approximately 3–4 seconds; this is not an instrumented measurement. |
| Unity Profiler capture | FOUND | Captured 300 Editor frames around a transition (`level-load-menu-to-gameplay.data`); the binary timeline was not available for detailed marker attribution in this run. |
| Editor transition timing | FOUND | A repeated/warm Play Mode transition took 69 ms from invoking `MenuController.Play` until `SceneManager.sceneLoaded`. This is an Editor-only sample, not the user's target device or a cold-start measurement. |
| Target device/build details | ABSENT | Cannot tell whether this is Editor, desktop, or a mobile device/build. |
| `project.yaml` performance budgets | ABSENT | The project config file has not been created. |
| Committed load-time, memory, or draw-call targets | ABSENT | No applicable target was found in the project docs/config. |

This combines static code/dependency inspection with one limited warm Editor timing sample; it is not a target-device runtime profile. The exact time split between scene deserialization, asset loading, and initialization remains **NOT ASSESSED**.

## Performance Budgets

| Metric | Budget | Estimated Current | Status |
|---|---|---|---|
| Frame time | No budget set | No load-window frame timing analyzed | NOT ASSESSED |
| Memory | No budget set | No runtime memory capture | NOT ASSESSED |
| Load time | No budget set | User: ~3–4 seconds; Editor warm sample: 69 ms to `sceneLoaded` | NOT ASSESSED |
| Draw calls | No budget set | No load-window rendering capture analyzed | NOT ASSESSED |

## Hotspots Identified

| # | Location | Issue | Estimated Impact | Fix Effort |
|---|---|---|---|---|
| 1 | `Assets/Game/Scripts/MenuController.cs:91-95` | Level selection calls synchronous `SceneManager.LoadScene("Gameplay")`; there is no loading UI. | Confirms the transition can visibly stall, but a repeated/warm Editor sample reached `sceneLoaded` in 69 ms, so this alone does not explain the reported 3–4 seconds. | S |
| 2 | `Assets/Game/Editor/GameBuilder.cs:157-165`; `Assets/Game/Scenes/Gameplay.unity` | The generated scene directly references all 30 levels and all 15 cat data assets. The level assets reference enemy data; those assets contain animation sprite arrays. Static inspection counted 480 sprite references in CatData and 1,800 in EnemyData (2,280 references total; not a unique-texture or runtime-memory measurement). | Plausible cold-load/device-specific candidate, not confirmed by the available Editor sample. | M/L |
| 3 | `Assets/Game/Scripts/GameManager.cs:92-109` | `Awake` initializes the selected level, builds that level's wave groups, initializes profile/wallet/perks/wall, and addon-use counts. The work is bounded by the selected level and addon list; no obvious expensive loop over all levels occurs here. | Low-to-medium candidate compared with synchronous loading of the scene dependency graph. | S |

## Likely Explanation

The level button does start a synchronous scene load, and Gameplay has a broad dependency graph: it references the complete campaign roster and every cat level, whose data references animation sprites. This is a plausible cold-load or slower-device candidate. However, the reported 3–4 seconds was not reproduced in the Editor: a repeated/warm Play Mode transition reached `sceneLoaded` in 69 ms. The available Editor Profiler capture was not parsed into a CPU timeline, and it does not represent a target mobile device, so the actual cause remains **NOT ASSESSED** rather than confirmed.

The project contains 4,253 enemy PNGs and 961 character PNGs in those source folders, but those folder totals are not equivalent to the assets loaded for Gameplay or their runtime memory footprint. They are not used as a loading-time estimate here.

## Optimization Recommendations

1. **Capture a load profile before changing asset architecture.**
   - Location: level selection through `MenuController.Play` and the Gameplay scene activation.
   - Expected gain: identifies the actual dominant stage; no direct runtime gain.
   - Risk: Low.
   - Approach: profile a Development Build on the device where the delay occurs. Record both first/cold entry and a second entry; inspect the main-thread timeline and scene/asset loading markers. Record the device, build type, and elapsed time.

2. **If asset loading dominates, reduce the scene's eager dependency set.**
   - Location: `GameBuilder` assignments for `levels`, `catsByLevel`, and the sprite arrays referenced from their data assets.
   - Expected gain: potentially meaningful on a cold load, but cannot be quantified until measured.
   - Risk: Medium/High; data availability and scene transitions must remain reliable.
   - Approach: load only the selected level's dependencies on demand, and keep later levels/unused cat animations out of the active scene's eager dependency graph. Consider asynchronous/bundled loading only if the capture confirms this is the bottleneck.

3. **Use an asynchronous transition/loading screen for responsiveness.**
   - Location: `MenuController.Play`.
   - Expected gain: can prevent a frozen-looking menu and show progress; does not guarantee a shorter total load time.
   - Risk: Medium; the transition must handle input, cancellation policy, and scene activation correctly.
   - Approach: use `SceneManager.LoadSceneAsync` and activate Gameplay when ready.

## Quick Wins

- Reproduce on the same target device and capture a Development Build in Unity Profiler before optimizing.
- Compare first entry with a subsequent entry to distinguish cold dependency loading from recurring scene setup.

## Requires Investigation

- Whether the measured 3–4 seconds is primarily scene asset/dependency loading, object deserialization, device I/O, or Editor overhead.
- Runtime memory and texture upload costs for the actual selected level and device.
- Whether a production build reproduces the delay seen during the current playtest.

## Verdict

**COMPLETE** — level-loading profile generated with an Editor warm-load measurement. The user's reported 3–4 second delay, target-device bottleneck attribution, and budget headroom are **NOT ASSESSED** because no target-device capture or committed performance budgets were available.
