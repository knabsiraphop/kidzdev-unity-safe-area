using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.SafeArea
{
    /// <summary>
    /// Fills the screen region OUTSIDE the device safe area (notch / home-bar / rounded corners) with image
    /// bars, so content underneath does not bleed into the cutout zones. The visual complement of
    /// <see cref="SafeArea"/>: that one shrinks content inward, this one paints over what's left.
    ///
    /// Usage:
    ///  (1) Put this on a full-screen RectTransform stretched edge-to-edge (anchors 0,0–1,1, offsets 0).
    ///      Do NOT also put a <see cref="SafeArea"/> on this object — it must cover the whole screen.
    ///  (2) Make it the top-most sibling so the bars render above the masked content.
    ///  (3) Assign <see cref="BarColor"/> (and optionally a bar sprite), or leave the sprite null for a
    ///      flat-colour fill.
    ///
    /// <see cref="ConformX"/> / <see cref="ConformY"/> mirror <see cref="SafeArea"/>: ConformX masks the
    /// left/right cutouts, ConformY masks the top/bottom cutouts. A disabled axis spawns no bars for it,
    /// so a portrait-only mask (ConformY) creates two images instead of four.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Layout/Safe Area Outside Mask")]
    public sealed class SafeAreaOutsideMask : SafeAreaTracker
    {
        const float Epsilon = 0.0001f;

        // Bar slots. 0/1 belong to the X axis, 2/3 to the Y axis.
        const int Left = 0;
        const int Right = 1;
        const int Bottom = 2;
        const int Top = 3;
        const int BarCount = 4;

        static readonly string[] BarNames = { "MaskBar_Left", "MaskBar_Right", "MaskBar_Bottom", "MaskBar_Top" };

        [Tooltip("Mask the left/right cutouts (landscape notch, rounded side corners). Off = no L/R bars.")]
        [SerializeField] bool conformX = true;

        [Tooltip("Mask the top/bottom cutouts (status-bar notch, home-bar indicator). Off = no T/B bars.")]
        [SerializeField] bool conformY = true;

        [Tooltip("Optional fill sprite. Null = a flat colour fill.")]
        [SerializeField] Sprite barSprite = null;

        [Tooltip("Bar colour (alpha respected).")]
        [SerializeField] Color barColor = Color.black;

        [Tooltip("Let the bars block touches that land on the cutout zones.")]
        [SerializeField] bool raycastTarget = true;

        readonly RectTransform[] _bars = new RectTransform[BarCount];

        /// <summary>Mask the left/right cutouts. Assigning rebuilds the bars on the next tick.</summary>
        public bool ConformX
        {
            get => conformX;
            set { conformX = value; SetDirty(); }
        }

        /// <summary>Mask the top/bottom cutouts. Assigning rebuilds the bars on the next tick.</summary>
        public bool ConformY
        {
            get => conformY;
            set { conformY = value; SetDirty(); }
        }

        /// <summary>Bar fill colour. Assigning re-styles the bars on the next tick.</summary>
        public Color BarColor
        {
            get => barColor;
            set { barColor = value; SetDirty(); }
        }

        protected override void Apply(Rect safeArea)
        {
            SyncBars();
            ApplyAnchors(safeArea);
        }

        bool IsAxisEnabled(int index)
        {
            if (index == Left || index == Right)
                return conformX;
            return conformY;
        }

        // Creates the bars the current conform flags need, destroys the ones they don't, restyles the rest.
        void SyncBars()
        {
            for (int i = 0; i < BarCount; i++)
            {
                bool needed = IsAxisEnabled(i);

                if (_bars[i] == null)
                {
                    Transform existing = Panel.Find(BarNames[i]); // reconnect after a domain reload
                    if (existing != null)
                        _bars[i] = existing.GetComponent<RectTransform>();
                }

                if (needed && _bars[i] == null)
                {
                    _bars[i] = CreateBar(BarNames[i]);
                }
                else if (!needed && _bars[i] != null)
                {
                    DestroyBar(_bars[i].gameObject);
                    _bars[i] = null;
                }
                else if (_bars[i] != null)
                {
                    StyleBar(_bars[i].GetComponent<Image>());
                }
            }
        }

        RectTransform CreateBar(string barName)
        {
            GameObject go = new GameObject(barName, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(Panel, false);
            StyleBar(go.GetComponent<Image>());
            return rt;
        }

        void StyleBar(Image img)
        {
            if (img == null)
                return;

            img.sprite = barSprite;
            img.color = barColor;
            img.raycastTarget = raycastTarget;
            img.type = barSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        }

        void DestroyBar(GameObject go)
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        void ApplyAnchors(Rect safe)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            // Normalised safe-area corners (0..1).
            float sx = safe.x / Screen.width;
            float sy = safe.y / Screen.height;
            float mx = (safe.x + safe.width) / Screen.width;
            float my = (safe.y + safe.height) / Screen.height;

            // NaN guard (some Samsung devices return NaN on the first frame — see SafeArea).
            if (float.IsNaN(sx) || float.IsNaN(sy) || float.IsNaN(mx) || float.IsNaN(my))
                return;

            // When X is not conformed there are no L/R bars, so the T/B bars must span the full width to
            // still cover the corners; otherwise they sit between the L/R bars to avoid overlap.
            float loX = conformX ? sx : 0f;
            float hiX = conformX ? mx : 1f;

            SetBar(Left, new Vector2(0f, 0f), new Vector2(sx, 1f));
            SetBar(Right, new Vector2(mx, 0f), new Vector2(1f, 1f));
            SetBar(Bottom, new Vector2(loX, 0f), new Vector2(hiX, sy));
            SetBar(Top, new Vector2(loX, my), new Vector2(hiX, 1f));
        }

        void SetBar(int index, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform bar = _bars[index];
            if (bar == null)
                return;

            // Zero-area bar (no cutout on that edge) -> disable to skip layout, raycast and the draw call.
            bool hasArea = (anchorMax.x - anchorMin.x) > Epsilon && (anchorMax.y - anchorMin.y) > Epsilon;
            if (bar.gameObject.activeSelf != hasArea)
                bar.gameObject.SetActive(hasArea);

            if (!hasArea)
                return;

            bar.anchorMin = anchorMin;
            bar.anchorMax = anchorMax;
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
        }
    }
}
