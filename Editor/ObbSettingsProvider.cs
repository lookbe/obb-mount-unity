using UnityEditor;
using System.Collections.Generic;

namespace AndroidObbMount
{
    public class ObbSettingsProvider : SettingsProvider
    {
        private const string SettingKey = "ObbMountAutomation_Enabled";

        public ObbSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        { }

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(SettingKey, true);
            set => EditorPrefs.SetBool(SettingKey, value);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("OBB Mount Automation Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("Enable OBB Automation", IsEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                IsEnabled = enabled;
            }

            EditorGUILayout.HelpBox("When enabled, APK builds will automatically exclude StreamingAssets and pack them into an OBB file.", MessageType.Info);
        }

        [SettingsProvider]
        public static SettingsProvider CreateObbSettingsProvider()
        {
            var provider = new ObbSettingsProvider("Project/OBB Mount", SettingsScope.Project);
            provider.keywords = new HashSet<string>(new[] { "OBB", "Mount", "StreamingAssets" });
            return provider;
        }
    }
}
