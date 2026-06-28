using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace KidzDev.Unity.SafeArea.Editor
{
    internal static class AndroidManifestHelper
    {
        const string ManifestRelativePath = "Assets/Plugins/Android/AndroidManifest.xml";
        const string AndroidNs = "http://schemas.android.com/apk/res/android";
        const string CutoutAttrLocal = "windowLayoutInDisplayCutoutMode";
        const string CorrectValue = "always";
        const string LauncherAction = "android.intent.action.MAIN";

        internal enum Status { NoManifest, Correct, NeedsChange }

        static string FullPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ManifestRelativePath));

        internal static string AssetPath => ManifestRelativePath;

        internal static (Status status, string currentValue) Read()
        {
            if (!File.Exists(FullPath))
                return (Status.NoManifest, null);

            try
            {
                var doc = Load();
                var activity = FindLauncherActivity(doc);
                if (activity == null)
                    return (Status.NeedsChange, null);

                string value = activity.GetAttribute(CutoutAttrLocal, AndroidNs);
                if (string.IsNullOrEmpty(value))
                    return (Status.NeedsChange, null);

                return value == CorrectValue
                    ? (Status.Correct, value)
                    : (Status.NeedsChange, value);
            }
            catch
            {
                return (Status.NeedsChange, "parse error");
            }
        }

        internal static bool ApplyFix(out string error)
        {
            error = null;
            try
            {
                var dir = Path.GetDirectoryName(FullPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(FullPath))
                {
                    File.WriteAllText(FullPath, BuildMinimalManifest());
                    return true;
                }

                var doc = Load();
                var activity = FindLauncherActivity(doc);
                if (activity == null)
                {
                    error = "Could not find launcher <activity>. Add android:windowLayoutInDisplayCutoutMode=\"always\" to it manually.";
                    return false;
                }

                var existing = activity.GetAttributeNode(CutoutAttrLocal, AndroidNs);
                if (existing != null)
                {
                    existing.Value = CorrectValue;
                }
                else
                {
                    var attr = doc.CreateAttribute("android", CutoutAttrLocal, AndroidNs);
                    attr.Value = CorrectValue;
                    activity.Attributes.Append(attr);
                }

                doc.Save(FullPath);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        static XmlDocument Load()
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(FullPath);
            return doc;
        }

        static XmlElement FindLauncherActivity(XmlDocument doc)
        {
            var activities = doc.GetElementsByTagName("activity");

            foreach (XmlElement act in activities)
            {
                foreach (XmlElement action in act.GetElementsByTagName("action"))
                {
                    if (action.GetAttribute("name", AndroidNs) == LauncherAction)
                        return act;
                }
            }

            // Fallback: single activity present, use it.
            return activities.Count == 1 ? activities[0] as XmlElement : null;
        }

        static string BuildMinimalManifest() =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"\n" +
            "          package=\"com.unity3d.player\">\n" +
            "    <application>\n" +
            "        <activity\n" +
            "            android:name=\"com.unity3d.player.UnityPlayerGameActivity\"\n" +
            "            android:windowLayoutInDisplayCutoutMode=\"always\">\n" +
            "            <intent-filter>\n" +
            "                <action android:name=\"android.intent.action.MAIN\" />\n" +
            "                <category android:name=\"android.intent.category.LAUNCHER\" />\n" +
            "            </intent-filter>\n" +
            "        </activity>\n" +
            "    </application>\n" +
            "</manifest>\n";
    }
}
