using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MRWorkshop.Editor
{
    public sealed class WorkshopBuildSettings : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        // Core207 injects/restores local debugger credentials at order1.
        public int callbackOrder => 2;
        public void OnPreprocessBuild(BuildReport report) => SanitizeLocalDebugSettings();
        public void OnPostprocessBuild(BuildReport report) => SanitizeLocalDebugSettings();

        public static void SanitizeLocalDebugSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Resources/DevAgentSettings.asset");
            if (settings == null) return;
            Sanitize(settings);
            Debug.Log("Focus build settings: local SDK debugger connection disabled and cleared.");
        }

        public static void Sanitize(ScriptableObject settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var serialized = new SerializedObject(settings);
            var enabled = serialized.FindProperty("enabled");
            var address = serialized.FindProperty("serverAddress");
            var token = serialized.FindProperty("accessToken");
            if (enabled == null || enabled.propertyType != SerializedPropertyType.Boolean ||
                address == null || address.propertyType != SerializedPropertyType.String ||
                token == null || token.propertyType != SerializedPropertyType.String)
                throw new InvalidOperationException("Unexpected SDK debugger settings schema; build must not embed local connection details.");
            enabled.boolValue = false;
            address.stringValue = string.Empty;
            token.stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            serialized.Update();
            if (enabled.boolValue || address.stringValue.Length != 0 || token.stringValue.Length != 0)
                throw new InvalidOperationException("SDK debugger connection settings were not cleared.");
        }
    }
}
