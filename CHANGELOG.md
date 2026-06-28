# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.1] - 2026-06-28

### Changed

- README: documented the **Safe Area Platform Settings** editor window and embedded
  a screenshot (`Documentation~/platform-settings.png`); removed a duplicate section.

## [1.1.0] - 2026-06-28

### Added

- **Editor — Safe Area Platform Settings window** (`Tools > KidzDev > Safe Area > Platform Settings`):
  a one-stop Editor window for the two platform settings that most affect safe-area behaviour.
  - **Android** panel: reads the project's `AndroidManifest.xml` and reports whether
    `android:windowLayoutInDisplayCutoutMode` is set to `"always"` on the launcher activity.
    Colour-coded status (`Correct` / `Needs Fix` / `No Manifest`). An **Apply Fix** button
    creates the manifest (or patches the existing one) so cutout content renders into the notch.
    Also shows the Unity `renderOutsideSafeArea` player-setting with a direct path hint.
  - **iOS** panel: exposes `PlayerSettings.iOS.hideHomeButton` as a toggle so the home indicator
    can be hidden without navigating to Project Settings.
- `Editor/KidzDev.Unity.SafeArea.Editor.asmdef` — new editor-only assembly for the above window
  and any future editor tooling.

### Changed

- Internal rename: `SetDirty()` → `RequestApply()`, `_dirty` → `_pendingApply`,
  `DriverTick()` → `OnPoll()`, and matching field names in `SafeAreaDriver`
  (`_lastSafeArea` → `_lastKnownSafeArea`, etc.). Public observable behaviour is unchanged.
- `SafeAreaOutsideMask`: bar anchors are now applied **before** the bar is activated
  (`SetActive(true)`) so the bar never appears at a stale size for one frame.

## [1.0.2] - 2026-06-28

### Added

- `SafeArea.ZeroOffsets` — when enabled, also clears `offsetMin`/`offsetMax` on
  apply so the panel fills the safe area exactly even if its offsets were non-zero.
- PlayMode lifecycle tests (`Tests/Runtime`): apply-on-enable, re-apply on conform
  change, `ZeroOffsets`, mask-bar generation, and driver resilience when a tracker
  is destroyed.
- README "Platform notes" (iOS, the Android "Render outside safe area" setting,
  the rotation transient, and correct RectTransform setup).

### Changed

- Replaced the per-component `Update` with a single internal `SafeAreaDriver` that
  polls once per frame for **all** trackers (one poll and one `Resolve()` per
  frame instead of one per component). Observable behaviour is unchanged.

## [1.0.1] - 2026-06-28

### Removed

- The `logging` serialized debug field from `SafeArea` and `SafeAreaOutsideMask`.
  Neither component exposes a per-instance console-logging toggle anymore.

### Changed

- Demo sample scene polish.

## [1.0.0] - 2026-06-28

### Added

- Initial release of `com.kidzdev.unity.safe-area`.
- `SafeArea` component — shrinks a `RectTransform`'s anchors to fit inside the
  device safe area (notch / status bar / home-bar / rounded corners), with
  per-axis `ConformX` / `ConformY` control and a Samsung first-frame NaN guard.
- `SafeAreaOutsideMask` component — fills the region *outside* the safe area with
  `Image` bars (Left / Right / Bottom / Top), spawning only the bars the enabled
  axes need, with `ConformX` / `ConformY`, `BarColor`, optional bar sprite,
  `raycastTarget`, and zero-area bar culling.
- `SafeAreaTracker` — shared abstract base that polls the resolved safe area,
  screen size, and orientation and only re-applies on change (or when forced
  dirty by an inspector edit).
- `SafeAreaSimulator` — editor device simulator (`SimDevice`: iPhone X, iPhone Xs
  Max, Pixel 3 XL landscape-left / landscape-right) honoured by **both**
  components, with pure `Resolve` / `Simulated` / `Normalized` helpers.
- `AddComponentMenu` entries under **Layout → Safe Area** and
  **Layout → Safe Area Outside Mask**; `[DisallowMultipleComponent]` prevents
  stacking a `SafeArea` and a `SafeAreaOutsideMask` on the same GameObject.
- Demo sample (`Samples~/Demo`) — a full-bleed background, a `SafeArea` content
  card, and a `SafeAreaOutsideMask`, with a controller that cycles the device
  simulator so the safe area reacts live in the Game view.
- Edit-mode tests (`Tests/Editor`) covering the simulator math.
