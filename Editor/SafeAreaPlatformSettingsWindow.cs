using UnityEditor;
using UnityEngine;

namespace KidzDev.Unity.SafeArea.Editor
{
    internal sealed class SafeAreaPlatformSettingsWindow : EditorWindow
    {
        AndroidManifestHelper.Status _androidStatus;
        string _androidCurrentValue;
        bool _pendingRefresh = true;

        bool _hideHomeButton;

        GUIStyle _sectionTitleStyle;
        GUIStyle _boldLabelStyle;
        GUIStyle _pathStyle;

        static readonly Color ColorCorrect = new Color(0.35f, 0.80f, 0.40f);
        static readonly Color ColorWarn    = new Color(0.95f, 0.78f, 0.20f);
        static readonly Color ColorGrey    = new Color(0.60f, 0.60f, 0.60f);

        const string PathAndroidManifest      = "Assets/Plugins/Android/AndroidManifest.xml";
        const string PathAndroidPlayerSetting = "Project Settings > Player > Android > Render Outside Safe Area";
        const string PathIosPlayerSetting     = "Project Settings > Player > iOS > Hide Home Button";

        [MenuItem("Tools/KidzDev/Safe Area/Platform Settings")]
        static void Open()
        {
            var w = GetWindow<SafeAreaPlatformSettingsWindow>("Safe Area Platform Settings");
            w.minSize = new Vector2(420, 300);
            w.Show();
        }

        void OnEnable() => _pendingRefresh = true;

        void Refresh()
        {
            (_androidStatus, _androidCurrentValue) = AndroidManifestHelper.Read();
            _hideHomeButton = PlayerSettings.iOS.hideHomeButton;
            _pendingRefresh = false;
        }

        void OnGUI()
        {
            EnsureStyles();
            if (_pendingRefresh) Refresh();

            EditorGUILayout.Space(10);
            DrawAndroidSection();
            EditorGUILayout.Space(6);
            DrawDivider();
            EditorGUILayout.Space(6);
            DrawIosSection();
            EditorGUILayout.Space(10);
        }

        void DrawAndroidSection()
        {
            EditorGUILayout.LabelField("ANDROID", _sectionTitleStyle);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(4);

                DrawRow("Display Cutout Mode", () =>
                {
                    string display = _androidCurrentValue ?? (_androidStatus == AndroidManifestHelper.Status.NoManifest ? "—" : "not set");
                    EditorGUILayout.LabelField(display, _boldLabelStyle);
                });

                DrawRow("Status", () =>
                {
                    var (label, color) = _androidStatus switch
                    {
                        AndroidManifestHelper.Status.Correct     => ("Correct",     ColorCorrect),
                        AndroidManifestHelper.Status.NeedsChange => ("Needs Fix",   ColorWarn),
                        _                                        => ("No Manifest", ColorGrey),
                    };
                    var prev = GUI.color;
                    GUI.color = color;
                    EditorGUILayout.LabelField(label, _boldLabelStyle);
                    GUI.color = prev;
                });

                DrawPath(PathAndroidManifest, clickable: _androidStatus != AndroidManifestHelper.Status.NoManifest);

                EditorGUILayout.Space(4);

                DrawRow("Unity renderOutsideSafeArea", () =>
                {
                    bool ros = PlayerSettings.Android.renderOutsideSafeArea;
                    var prev = GUI.color;
                    GUI.color = ros ? ColorCorrect : ColorWarn;
                    EditorGUILayout.LabelField(ros ? "Enabled" : "Disabled", _boldLabelStyle);
                    GUI.color = prev;
                });

                DrawPath(PathAndroidPlayerSetting);

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_androidStatus == AndroidManifestHelper.Status.Correct))
                    {
                        if (GUILayout.Button("Apply Fix", GUILayout.Width(100)))
                            ApplyAndroidFix();
                    }
                }

                EditorGUILayout.Space(4);
            }
        }

        void DrawIosSection()
        {
            EditorGUILayout.LabelField("iOS", _sectionTitleStyle);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(4);

                DrawRow("Hide Home Indicator", () =>
                {
                    _hideHomeButton = EditorGUILayout.Toggle(_hideHomeButton);
                });

                DrawRow("Status", () =>
                {
                    bool saved = PlayerSettings.iOS.hideHomeButton;
                    var prev = GUI.color;
                    GUI.color = saved ? ColorCorrect : ColorWarn;
                    EditorGUILayout.LabelField(saved ? "Enabled" : "Disabled", _boldLabelStyle);
                    GUI.color = prev;
                });

                DrawPath(PathIosPlayerSetting);

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Apply", GUILayout.Width(100)))
                        ApplyIos();
                }

                EditorGUILayout.Space(4);
            }
        }

        void ApplyAndroidFix()
        {
            if (!AndroidManifestHelper.ApplyFix(out string error))
            {
                EditorUtility.DisplayDialog("Safe Area – Android Fix Failed", error, "OK");
                return;
            }

            AssetDatabase.Refresh();
            _pendingRefresh = true;

            var asset = AssetDatabase.LoadAssetAtPath<Object>(AndroidManifestHelper.AssetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        void ApplyIos()
        {
            PlayerSettings.iOS.hideHomeButton = _hideHomeButton;
            _pendingRefresh = true;
        }

        static void DrawRow(string label, System.Action valueDrawer)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(200));
                valueDrawer();
            }
        }

        void DrawPath(string path, bool clickable = false)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16);

                bool isAssetPath = path.StartsWith("Assets/");

                if (clickable && isAssetPath)
                {
                    if (GUILayout.Button(path, _pathStyle))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }
                }
                else
                {
                    var prev = GUI.color;
                    GUI.color = ColorGrey;
                    GUILayout.Label(path, _pathStyle);
                    GUI.color = prev;
                }
            }

            EditorGUILayout.Space(2);
        }

        static void DrawDivider()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        void EnsureStyles()
        {
            _sectionTitleStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _boldLabelStyle    ??= new GUIStyle(EditorStyles.boldLabel);
            _pathStyle         ??= new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = false,
                normal   = { textColor = ColorGrey },
                hover    = { textColor = new Color(0.55f, 0.75f, 1.0f) },
            };
        }
    }
}
