using FocusCore.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FocusCore.Tests
{
    public class BuildSettingsTests
    {
        [Test]
        public void BuildSettingsClearLocalSdkConnectionWithoutChangingItsPort()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Resources/DevAgentSettings.asset");
            Assert.That(asset, Is.Not.Null, "Core207 must provide its generated runtime settings.");
            var serialized = new SerializedObject(asset);
            var enabled = serialized.FindProperty("enabled");
            var address = serialized.FindProperty("serverAddress");
            var token = serialized.FindProperty("accessToken");
            var port = serialized.FindProperty("serverPort");
            var oldEnabled = enabled.boolValue;
            var oldAddress = address.stringValue;
            var oldToken = token.stringValue;
            var oldPort = port.intValue;
            try
            {
                enabled.boolValue = true;
                address.stringValue = "127.0.0.1";
                token.stringValue = "focus-test-connection-token";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                FocusBuildSettings.Sanitize(asset);
                serialized.Update();
                Assert.That(enabled.boolValue, Is.False);
                Assert.That(address.stringValue, Is.Empty);
                Assert.That(token.stringValue, Is.Empty);
                Assert.That(port.intValue, Is.EqualTo(oldPort));
            }
            finally
            {
                serialized.Update();
                enabled.boolValue = oldEnabled;
                address.stringValue = oldAddress;
                token.stringValue = oldToken;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }
    }
}
