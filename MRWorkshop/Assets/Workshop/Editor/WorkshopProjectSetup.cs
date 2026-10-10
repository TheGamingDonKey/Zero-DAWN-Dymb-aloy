using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oculus.Interaction.OVR.Editor.QuickActions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace MRWorkshop.Editor
{
    public static class WorkshopProjectSetup
    {
        public const string BaselineScene = "Assets/Workshop/Scenes/PassthroughBaseline.unity";
        public const string PlatformPrefab = "Assets/Workshop/Prefabs/WorkshopPlatform.prefab";
        [MenuItem("Workshop/Configure Android XR")]
        public static void ConfigureAndValidate()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android || EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Android)
                throw new InvalidOperationException("Select Android before configuring Focus (CLI: -buildTarget Android).");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.thegamingdonkey.mrworkshop");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.MTRendering = true;
            EditorUserBuildSettings.buildAppBundle = false;
            var mobilePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (mobilePipeline == null) throw new InvalidOperationException("Mobile URP asset missing.");
            GraphicsSettings.defaultRenderPipeline = mobilePipeline;
            QualitySettings.SetQualityLevel(0, false);
            QualitySettings.renderPipeline = mobilePipeline;

            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var perTarget))
            {
                Directory.CreateDirectory("Assets/XR");
                AssetDatabase.Refresh();
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }
            const BuildTargetGroup group = BuildTargetGroup.Android;
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var general = perTarget.SettingsForBuildTarget(group);
            general.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group))
                throw new InvalidOperationException("OpenXR loader assignment failed.");
            if (general.Manager.activeLoaders.OfType<OpenXRLoader>().Count() != 1)
                throw new InvalidOperationException("Android OpenXR loader missing or duplicated.");

            var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (xr == null) throw new InvalidOperationException("Android OpenXR settings were not created.");
            var requiredFeatures = new[] {
                "Meta.XR.MetaXRFeature",
                "UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature",
                "UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile",
                "UnityEngine.XR.Hands.OpenXR.HandTracking",
                "UnityEngine.XR.Hands.OpenXR.MetaHandTrackingAim"
            };
            var features = xr.GetFeatures<OpenXRFeature>();
            foreach (var typeName in requiredFeatures)
            {
                var feature = features.SingleOrDefault(f => f != null && f.GetType().FullName == typeName);
                if (feature == null) throw new InvalidOperationException("OpenXR feature missing: " + typeName);
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
            xr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            xr.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            EditorUtility.SetDirty(xr);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager);
            EditorUtility.SetDirty(perTarget);

            var config = OVRProjectConfig.CachedProjectConfig;
            if (config == null) throw new InvalidOperationException("Meta project configuration is not ready.");
            config.targetDeviceTypes = new List<OVRProjectConfig.DeviceType> { OVRProjectConfig.DeviceType.Quest3S };
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            config.systemLoadingScreenBackground = OVRProjectConfig.SystemLoadingScreenBackground.ContextualPassthrough;
            config.isPassthroughCameraAccessEnabled = false;
            OVRProjectConfig.CommitProjectConfig(config);
            var runtime = OVRRuntimeSettings.Instance;
            runtime.HandSkeletonVersion = OVRHandSkeletonVersion.OpenXR;
            OVRRuntimeSettings.CommitRuntimeSettings(runtime);
            // Meta's required GameActivity rule validates the local manifest too.
            // Preserve custom entries while creating/updating the Workshop manifest.
            OVRManifestPreprocessor.GenerateOrUpdateAndroidManifest(silentMode: true);
            var manifest = OVRManifestPreprocessor.GetAndroidManifestXmlDocument();
            var activities = manifest?.SelectNodes("/manifest/application/activity");
            const string androidNamespace = "http://schemas.android.com/apk/res/android";
            if (activities == null || activities.Count != 1 ||
                activities[0].Attributes?["name", androidNamespace]?.Value != "com.unity3d.player.UnityPlayerGameActivity")
                throw new InvalidOperationException("Workshop manifest must contain exactly one UnityPlayerGameActivity; Meta manifest generation failed.");
            AssetDatabase.SaveAssets();

            var issues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, group);
            foreach (var issue in issues) Debug.Log("Focus OpenXR validation " + (issue.error ? "ERROR: " : "recommendation: ") + issue.message);
            if (issues.Any(i => i.error)) throw new InvalidOperationException("Required OpenXR validation failed; see preceding log messages.");
            Debug.Log("Focus Android settings validated. Headset/passthrough runtime checks are still pending.");
        }

        [MenuItem("Workshop/Generate Passthrough Baseline")]
        public static void GenerateBaseline()
        {
            Directory.CreateDirectory("Assets/Workshop/Scenes");
            Directory.CreateDirectory("Assets/Workshop/Prefabs");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            OVRQuickActionsAPI.AddOVRInteractionRig(false);
            var rigs = UnityEngine.Object.FindObjectsByType<OVRCameraRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (rigs.Length != 1) throw new InvalidOperationException("SDK did not create exactly one camera rig.");
            var root = new GameObject("WorkshopPlatform");
            rigs[0].transform.SetParent(root.transform, false);
            var manager = rigs[0].GetComponent<OVRManager>();
            manager.isInsightPassthroughEnabled = true;
            var serializedManager = new SerializedObject(manager);
            var trackingOrigin = serializedManager.FindProperty("_trackingOriginType");
            if (trackingOrigin == null) throw new InvalidOperationException("SDK tracking-origin field missing.");
            trackingOrigin.intValue = (int)OVRManager.TrackingOrigin.FloorLevel;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            var camera = rigs[0].centerEyeAnchor.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.allowHDR = false;
            const string passthroughPath = "Packages/com.meta.xr.sdk.core/Editor/BuildingBlocks/BlockData/Passthrough/Prefabs/PassthroughUnderlay.prefab";
            var passthroughPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(passthroughPath);
            if (passthroughPrefab == null) throw new InvalidOperationException("SDK passthrough-underlay prefab missing.");
            var passthrough = (GameObject)PrefabUtility.InstantiatePrefab(passthroughPrefab, root.transform);
            var layer = passthrough.GetComponent<OVRPassthroughLayer>();
            if (layer == null) throw new InvalidOperationException("SDK passthrough prefab has no layer.");
            layer.hidden = false;
            layer.textureOpacity = 1;
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PlatformPrefab, InteractionMode.AutomatedAction);
            if (!EditorSceneManager.SaveScene(scene, BaselineScene)) throw new IOException("Could not save baseline scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BaselineScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Focus baseline generated from official SDK prefabs; hardware validation remains pending.");
        }
    }
}
