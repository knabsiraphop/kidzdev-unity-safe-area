# KidzDev Unity Safe Area

Two complementary notch / safe-area components for Unity uGUI:

| Component | What it does |
| --- | --- |
| `SafeArea` | **Shrinks** a `RectTransform` inward so content stays inside the device safe area. |
| `SafeAreaOutsideMask` | **Fills** the region *outside* the safe area with colour bars so content never bleeds into the cutouts. |

Both share one change-tracking core and an in-editor device **simulator**, so you
can preview notch layouts in the Game view without deploying to a phone.

## Install

Add via Package Manager → *Add package from git URL*, or edit
`Packages/manifest.json`:

```
https://github.com/knabsiraphop/kidzdev-unity-safe-area.git#v1.0.0
```

---

## Which component do I need?

- Use **`SafeArea`** on a content panel that must not be covered by the notch or
  home bar (buttons, HUD, headers). It moves the panel's anchors inward.
- Use **`SafeAreaOutsideMask`** when you have a full-bleed background (gameplay,
  video, a photo) and want solid bars drawn over the cutout zones instead of
  shrinking the content. It paints; it does not move your content.
- They compose: a full-screen background with a `SafeAreaOutsideMask` on top, and
  a `SafeArea` panel of UI in between.

---

## `SafeArea`

Drives the attached `RectTransform`'s `anchorMin` / `anchorMax` to match the safe
area each time it changes.

**Setup**

1. Add a `SafeArea` component (menu: *Layout → Safe Area*) to the top-level
   RectTransform of a UI panel.
2. Make sure that rect is stretched to its parent with **zero offsets** — only the
   anchors are driven, so the offsets must already be `0`.
3. If you have a full-screen background you want to keep *behind* the notch, put
   the `SafeArea` on an immediate child holding the foreground content instead, and
   leave the background full-bleed.

**Per-axis control**

`ConformX` / `ConformY` let you constrain a single axis — useful when a layout
mixes full-width and full-height background stripes. A disabled axis is left at
full screen.

```csharp
var safe = panel.GetComponent<SafeArea>();
safe.ConformX = false; // ignore left/right insets, keep top/bottom
safe.ConformY = true;  // re-applies automatically on the next tick
```

---

## `SafeAreaOutsideMask`

Spawns up to four `Image` bars (Left / Right / Bottom / Top) as children and
anchors them to cover exactly the region outside the safe area.

**Setup**

1. Add a `SafeAreaOutsideMask` (menu: *Layout → Safe Area Outside Mask*) to a
   full-screen RectTransform stretched edge-to-edge (anchors `0,0`–`1,1`,
   offsets `0`). Do **not** also add a `SafeArea` to this object.
2. Make it the top-most sibling so the bars render above the masked content.
3. Set `BarColor` (and optionally a bar sprite). Leave the sprite null for a flat
   colour fill.

**Notes**

- A disabled axis spawns no bars for it: a portrait-only mask (`ConformY` only)
  creates two images instead of four.
- Bars with no cutout on their edge have zero area and are deactivated, so they
  cost no draw call, layout, or raycast.
- `raycastTarget` (default on) lets the bars swallow touches that land on the
  cutout zones.

```csharp
var mask = root.GetComponent<SafeAreaOutsideMask>();
mask.BarColor = Color.black;
mask.ConformX = false; // only top/bottom bars (portrait)
```

---

## Sample

Import the **Demo** from the Package Manager (*Samples* tab). It contains a
full-bleed teal background, a `SafeArea` content card, and a
`SafeAreaOutsideMask`, plus a controller that cycles the device simulator. Press
**Play** and watch the card shrink and the mask bars appear as the simulated
device changes (it also auto-advances every few seconds).

---

## Editor device simulator

Both components resolve their safe area through `SafeAreaSimulator`. In the editor
you can force a device's safe area so the Game view shows the notch layout:

```csharp
using KidzDev.Unity.SafeArea;

// Flip every SafeArea / SafeAreaOutsideMask in the scene to an iPhone X layout.
SafeAreaSimulator.Sim = SimDevice.iPhoneX;

// Back to the real (full-screen-in-editor) safe area.
SafeAreaSimulator.Sim = SimDevice.None;
```

Available devices: `iPhoneX`, `iPhoneXsMax`, `Pixel3XL_LandscapeLeft`,
`Pixel3XL_LandscapeRight`. The simulation is **editor-only** — in a player build
the real `Screen.safeArea` is always used.

The resolver is also exposed as pure functions for tooling and tests:

```csharp
Rect normalized = SafeAreaSimulator.Normalized(SimDevice.iPhoneX, portrait: true);
Rect pixels     = SafeAreaSimulator.Simulated(SimDevice.iPhoneX, 1125, 2436);
```

---

## How it updates

`SafeAreaTracker` (the shared base) polls once per frame and re-applies only when
the resolved safe area, screen size, or orientation actually changes — so an idle
safe-area object does no work beyond a few comparisons. Inspector edits flag the
component dirty (via `OnValidate`) and re-apply on the next tick. Both components
run in edit mode (`[ExecuteAlways]`).

A first-frame `NaN` safe area (seen on some Samsung devices) is detected and
skipped until valid.

---

## License

MIT — see [LICENSE.md](LICENSE.md).
