using System.Linq;
using FocusCore.Editor;
using NUnit.Framework;
using Oculus.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace FocusCore.Tests
{
    public class PlatformSetupTests
    {
        [Test]
        public void AndroidBuildUsesArm64Il2CppAndSupportedGraphics()
        {
            FocusProjectSetup.ConfigureAndValidate();
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android), Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That((int)PlayerSettings.Android.minSdkVersion, Is.GreaterThanOrEqualTo(32));
            Assert.That(PlayerSettings.Android.applicationEntry, Is.EqualTo(AndroidApplicationEntry.GameActivity));
            Assert.That(PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android), Is.EqualTo("com.thegamingdonkey.focuscore"));
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), Is.EqualTo(new[] { GraphicsDeviceType.Vulkan }));
        }

        [Test]
        public void OpenXrHasLoaderHandsControllerAndMetaFeatures()
        {
            FocusProjectSetup.ConfigureAndValidate();
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            Assert.That(general, Is.Not.Null);
            Assert.That(general.InitManagerOnStart, Is.True);
            Assert.That(general.Manager.activeLoaders.OfType<OpenXRLoader>().Count(), Is.EqualTo(1));
            var enabledTypes = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android)
                .GetFeatures<OpenXRFeature>().Where(f => f.enabled).Select(f => f.GetType().FullName).ToArray();
            Assert.That(enabledTypes, Does.Contain("Meta.XR.MetaXRFeature"));
            Assert.That(enabledTypes, Does.Contain("UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature"));
            Assert.That(enabledTypes, Does.Contain("UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile"));
            Assert.That(enabledTypes, Does.Contain("UnityEngine.XR.Hands.OpenXR.HandTracking"));
            Assert.That(enabledTypes, Does.Contain("UnityEngine.XR.Hands.OpenXR.MetaHandTrackingAim"));
            var config = OVRProjectConfig.CachedProjectConfig;
            Assert.That(config.handTrackingSupport, Is.EqualTo(OVRProjectConfig.HandTrackingSupport.ControllersAndHands));
            Assert.That(config.insightPassthroughSupport, Is.EqualTo(OVRProjectConfig.FeatureSupport.Required));
            Assert.That(config.targetDeviceTypes, Does.Contain(OVRProjectConfig.DeviceType.Quest3S));
            Assert.That(config.isPassthroughCameraAccessEnabled, Is.False);
        }

        [Test]
        public void SavedBaselineContainsTransparentCameraPassthroughAndHandControllerInteractors()
        {
            FocusProjectSetup.ConfigureAndValidate();
            FocusProjectSetup.GenerateBaseline();
            EditorSceneManager.OpenScene(FocusProjectSetup.BaselineScene);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(FocusProjectSetup.PlatformPrefab), Is.Not.Null);
            var rigs = Object.FindObjectsByType<OVRCameraRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(rigs.Length, Is.EqualTo(1));
            Assert.That(rigs[0].GetComponent<OVRManager>().isInsightPassthroughEnabled, Is.True);
            var camera = rigs[0].centerEyeAnchor.GetComponent<Camera>();
            Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(camera.backgroundColor.a, Is.EqualTo(0));
            var layers = Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(layers.Length, Is.EqualTo(1));
            Assert.That(layers[0].textureOpacity, Is.EqualTo(1));
            Assert.That(layers[0].hidden, Is.False);
            Assert.That(Object.FindObjectsByType<RayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(Object.FindObjectsByType<PokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void RepeatedGenerationReplacesBaselineWithoutDuplicateRigs()
        {
            FocusProjectSetup.ConfigureAndValidate();
            FocusProjectSetup.GenerateBaseline();
            FocusProjectSetup.GenerateBaseline();
            Assert.That(Object.FindObjectsByType<OVRCameraRig>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
    }
}
