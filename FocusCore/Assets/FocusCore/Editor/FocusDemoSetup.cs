using System;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Editor.QuickActions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FocusCore.Editor
{
    public static class FocusDemoSetup
    {
        public const string CoreScene = "Assets/FocusCore/Scenes/FocusCore.unity";
        [MenuItem("Focus/Generate First Focus Demo")]
        public static void Generate()
        {
            // New isolated scene; baseline prefab and baseline scene remain unchanged.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FocusProjectSetup.PlatformPrefab);
            if (prefab == null) throw new InvalidOperationException("Generate the passthrough baseline first.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FocusCore");
            var platform = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            var rig = platform.GetComponentInChildren<OVRCameraRig>(true);
            if (rig == null) throw new InvalidOperationException("Baseline rig missing.");
            var focus = root.AddComponent<FocusController>();
            focus.head = rig.centerEyeAnchor;
            focus.audioSource = root.AddComponent<AudioSource>();
            focus.audioSource.playOnAwake = false;
            focus.audioSource.spatialBlend = 0;
            focus.chirp = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FocusCore/Art/FocusChirp.wav");
            if (focus.chirp == null) throw new InvalidOperationException("Original chirp asset missing.");

            var canvasObject = new GameObject("FocusPanel", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = rig.centerEyeAnchor.GetComponent<Camera>();
            var panel = (RectTransform)canvasObject.transform;
            panel.sizeDelta = new Vector2(640,560);
            panel.localScale = Vector3.one * 0.0008f;
            // Keep the saved scene readable before XR supplies the user's head pose.
            panel.localPosition = new Vector3(0,-0.12f,0.8f);
            focus.panel = panel;
            var background = canvasObject.AddComponent<Image>();
            background.color = new Color(0.047f,0.076f,0.14f,0.94f);
            background.raycastTarget = false;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            MakeText(panel, font, "FOCUS / VIRTUAL FIELD STUDY", new Vector2(-270,202), new Vector2(540,55),30);
            MakeText(panel, font, "Scan, look at a target, then inspect its record.\nAuthored demo objects / no real-world recognition", new Vector2(-270,148),new Vector2(540,50),20);
            var scan = new GameObject("Scan",typeof(RectTransform),typeof(Image),typeof(Button),typeof(FocusScanButton));
            var rect = (RectTransform)scan.transform;
            rect.SetParent(panel,false); rect.sizeDelta = new Vector2(156,64); rect.anchoredPosition = new Vector2(-190,83);
            scan.GetComponent<Image>().color = new Color(0.615f,0.388f,1,1);
            var button = scan.GetComponent<Button>(); button.targetGraphic = scan.GetComponent<Image>();
            scan.GetComponent<FocusScanButton>().focus = focus;
            MakeText(rect,font,"SCAN",new Vector2(-78,-32),new Vector2(156,64),25,TextAnchor.MiddleCenter);
            MakeAction(panel,font,focus,"INSPECT",new Vector2(0,83),false);
            MakeAction(panel,font,focus,"CLOSE",new Vector2(190,83),true);
            focus.status = MakeText(panel,font,"WAITING FOR XR",new Vector2(-270,-24),new Vector2(540,68),20);
            var divider = new GameObject("InformationDivider",typeof(RectTransform),typeof(Image));
            var dividerRect = (RectTransform)divider.transform; dividerRect.SetParent(panel,false);
            dividerRect.sizeDelta = new Vector2(540,2); dividerRect.anchoredPosition = new Vector2(0,-52);
            divider.GetComponent<Image>().color = new Color(.615f,.388f,1,.7f); divider.GetComponent<Image>().raycastTarget = false;
            focus.targets = root.AddComponent<FocusTargets>(); focus.targets.focus = focus;
            focus.targets.head = focus.head; focus.targets.panel = panel;
            const string targetMaterialPath = "Assets/FocusCore/Art/FocusTarget.mat";
            var targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetMaterialPath);
            if (targetMaterial == null)
            {
                var targetShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (targetShader == null) throw new InvalidOperationException("URP target shader missing.");
                targetMaterial = new Material(targetShader); AssetDatabase.CreateAsset(targetMaterial,targetMaterialPath);
            }
            focus.targets.targetMaterial = targetMaterial;
            QuickActionsAPI.AddRayCanvasInteraction(canvasObject);
            QuickActionsAPI.AddPokeCanvasInteraction(canvasObject);
            foreach (var pointable in canvasObject.GetComponentsInChildren<PointableCanvas>(true))
            {
                var cancel = pointable.gameObject.AddComponent<FocusCanvasCancellation>();
                cancel.focus = focus; cancel.pointable = pointable;
            }

            var pulseObject = new GameObject("ScanLattice"); pulseObject.transform.SetParent(root.transform,false);
            focus.visuals = pulseObject.AddComponent<FocusVisuals>();
            var shader = Shader.Find("Focus/UnlitPulse");
            if (shader == null) throw new InvalidOperationException("Pulse shader did not import.");
            const string materialPath = "Assets/FocusCore/Art/FocusPulse.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material,materialPath); }
            focus.visuals.pulseMaterial = material;
            // Use the authored FBX reticle as a small static demonstration next to the card.
            var reticle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FocusCore/Art/FocusReticle.fbx");
            if (reticle == null) throw new InvalidOperationException("Original reticle FBX did not import.");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(reticle,panel);
            model.transform.localPosition = new Vector3(252,221,-20);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 60;
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            if (modelRenderers.Length == 0) throw new InvalidOperationException("Reticle FBX contains no renderer.");
            var bounds = modelRenderers[0].bounds;
            foreach (var renderer in modelRenderers) bounds.Encapsulate(renderer.bounds);
            // Inspect imported geometry rather than guessing how the FBX importer converted Blender axes.
            if (bounds.size.y < bounds.size.x && bounds.size.y < bounds.size.z)
                model.transform.localRotation = Quaternion.Euler(90,0,0);
            else if (bounds.size.x < bounds.size.y && bounds.size.x < bounds.size.z)
                model.transform.localRotation = Quaternion.Euler(0,90,0);
            // Override imported Blender materials using URP; FBX's surface model is not our shader.
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i=0;i<mats.Length;i++)
                {
                    string path = "Assets/FocusCore/Art/ReticleColour" + i + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        mat = new Material(shader);
                        mat.SetColor("_BaseColor", mats[i] != null ? mats[i].color : new Color(0.615f,0.388f,1,1));
                        AssetDatabase.CreateAsset(mat,path);
                    }
                    mats[i] = mat;
                }
                renderer.sharedMaterials = mats;
            }

            var interactors = root.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(c => c is RayInteractor || c is PokeInteractor).ToArray();
            int configured = 0;
            foreach (var interactor in interactors)
            {
                var tracked = FindDevice(interactor.transform,rig.transform);
                if (tracked == null) throw new InvalidOperationException("No unambiguous device for " + interactor.name);
                var input = interactor.gameObject.AddComponent<MetaScanInput>();
                input.focus = focus; input.interactorComponent = interactor; input.trackedDevice = tracked;
                // All actions share one panel. Poke must withdraw from its plane or bounds,
                // not be rearmed just because a held finger moved to a neighbouring button.
                input.scanRect = panel; input.poke = interactor is PokeInteractor;
                configured++;
            }
            if (configured < 4) throw new InvalidOperationException("Hand/controller input sources were not generated.");
            if (!EditorSceneManager.SaveScene(scene,CoreScene)) throw new IOException("Could not save Core scene.");
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(CoreScene,true)};
            AssetDatabase.SaveAssets();
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(panel.position,Quaternion.identity,0.4f);
            Debug.Log("Focus Core scene generated: " + configured + " SDK input sources. Headset checks remain pending.");
        }

        static MonoBehaviour FindDevice(Transform start, Transform rig)
        {
            for (var node=start; node!=null && node!=rig; node=node.parent)
            {
                var candidates = node.GetComponents<MonoBehaviour>()
                    .Where(c=>c is IHand || c is IController).ToArray();
                if (candidates.Length == 1) return candidates[0];
                if (candidates.Length > 1) throw new InvalidOperationException("Ambiguous device binding on " + node.name);
            }
            return null;
        }

        static void MakeAction(RectTransform panel, Font font, FocusController focus, string title, Vector2 position, bool dismiss)
        {
            var action = new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button),typeof(FocusTargetButton));
            var rect = (RectTransform)action.transform; rect.SetParent(panel,false);
            rect.sizeDelta = new Vector2(156,64); rect.anchoredPosition = position;
            var image = action.GetComponent<Image>(); image.color = new Color(.20f,.16f,.34f,1);
            action.GetComponent<Button>().targetGraphic = image;
            var input = action.GetComponent<FocusTargetButton>(); input.focus = focus; input.dismiss = dismiss;
            MakeText(rect,font,title,new Vector2(-78,-32),new Vector2(156,64),25,TextAnchor.MiddleCenter);
        }

        static Text MakeText(RectTransform parent, Font font, string content, Vector2 position, Vector2 size,int points,TextAnchor alignment=TextAnchor.UpperLeft)
        {
            var go = new GameObject("Label",typeof(RectTransform),typeof(Text));
            var rect = (RectTransform)go.transform; rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0.5f); rect.pivot = Vector2.zero;
            rect.sizeDelta = size; rect.anchoredPosition = position;
            var text = go.GetComponent<Text>(); text.font = font; text.text = content; text.fontSize = points;
            text.color = new Color(0.91f,0.93f,0.96f,1); text.raycastTarget = false; text.alignment = alignment;
            return text;
        }
    }
}
