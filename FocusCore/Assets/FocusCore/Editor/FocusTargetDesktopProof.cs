using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FocusCore.Editor
{
    // Editor-only scripted interaction with the actual generated Core scene.
    [InitializeOnLoad]
    public static class FocusTargetDesktopProof
    {
        const string Requested = "FocusCore.TargetProof.Requested";
        static FocusController focus;
        static Text label;
        static double started, lastTick;
        static int stage;
        static string folder;
        static FocusTargetDesktopProof() { EditorApplication.playModeStateChanged += StateChanged; }

        [MenuItem("Focus/Run Target Interaction Demo (Simulated Input)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop PlayMode before this demo.");
            EditorSceneManager.OpenScene(FocusDemoSetup.CoreScene);
            var controller = UnityEngine.Object.FindFirstObjectByType<FocusController>();
            if (controller == null || controller.targets == null) throw new InvalidOperationException("Generate the new Core scene first.");
            var platform = controller.GetComponentInChildren<OVRCameraRig>(true).transform;
            while (platform.parent != controller.transform) platform = platform.parent;
            platform.gameObject.SetActive(false);
            controller.enabled = false;
            var camera = new GameObject("SimulatedHeadCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.035f,.07f);
            camera.fieldOfView = 72;
            controller.head = camera.transform; controller.targets.head = camera.transform;
            controller.panel.GetComponent<Canvas>().worldCamera = camera;
            var overlay = new GameObject("SimulatedInputNotice",typeof(Canvas)).GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            var notice = new GameObject("TargetProofStatus",typeof(RectTransform),typeof(Text));
            notice.transform.SetParent(overlay.transform,false);
            var rect = notice.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0,0); rect.anchorMax = new Vector2(1,0); rect.pivot = new Vector2(.5f,0);
            rect.anchoredPosition = new Vector2(0,15); rect.sizeDelta = new Vector2(-40,90);
            var text = notice.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20; text.color = Color.white;
            text.text = "TARGET INTERACTION DEMO / SIMULATED INPUT\nActual Core scene; no headset, passthrough or physical hand proof.";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/FocusCore/Scenes/DesktopTargetProof.unity");
            SessionState.SetBool(Requested,true); EditorApplication.isPlaying = true;
        }

        static void StateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested,false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                focus = UnityEngine.Object.FindFirstObjectByType<FocusController>();
                label = GameObject.Find("TargetProofStatus").GetComponent<Text>();
                started = lastTick = EditorApplication.timeSinceStartup; stage = 0;
                folder = Path.GetFullPath(Path.Combine(Application.dataPath,"../../.artifacts/research/targets-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
                Directory.CreateDirectory(folder); EditorApplication.update += Tick;
                EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= Tick;
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Requested,false); EditorSceneManager.OpenScene(FocusDemoSetup.CoreScene);
            }
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || focus == null) return;
            try
            {
                double now = EditorApplication.timeSinceStartup, elapsed = now-started;
                focus.SetAvailability(true,true,false); focus.Advance((float)(now-lastTick)); lastTick = now;
                if (stage == 0 && elapsed >= .5)
                {
                    focus.Input.ObserveSource(9001,true,true);
                    Require(focus.TryScan(9001),"Scan rejected."); stage++;
                }
                else if (stage == 1 && elapsed >= 2)
                {
                    Require(focus.targets.RevealedCount == 3,"Three targets were not revealed.");
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,"targets-revealed.png")); stage++;
                }
                else if (stage == 2 && elapsed >= 3)
                {
                    focus.head.LookAt(focus.targets.GetTargetPosition(0));
                    Require(!focus.TryInspect(9001),"Held Scan also inspected.");
                    focus.Input.ObserveSource(9001,true,true);
                    Require(focus.TryInspect(9001) && focus.targets.SelectedIndex == 0,"Relay inspection failed."); stage++;
                }
                else if (stage == 3 && elapsed >= 4)
                { ScreenCapture.CaptureScreenshot(Path.Combine(folder,"relay-information.png")); stage++; }
                else if (stage == 4 && elapsed >= 6)
                {
                    focus.head.LookAt(focus.targets.GetTargetPosition(2)); focus.Input.ObserveSource(9001,true,true);
                    Require(focus.TryInspect(9001) && focus.targets.SelectedIndex == 2,"Cache inspection failed."); stage++;
                }
                else if (stage == 5 && elapsed >= 7)
                { ScreenCapture.CaptureScreenshot(Path.Combine(folder,"cache-information.png")); stage++; }
                else if (stage == 6 && elapsed >= 9)
                {
                    Require(!focus.TryDismiss(9001),"Held Inspect also dismissed.");
                    focus.Input.ObserveSource(9001,true,true);
                    Require(focus.TryDismiss(9001) && !focus.targets.InformationVisible,"Dismissal failed."); stage++;
                }
                else if (stage == 7 && elapsed >= 10)
                {
                    focus.SetAvailability(true,false,false);
                    Require(focus.targets.RevealedCount == 0 && !focus.targets.InformationVisible,"Tracking loss retained targets.");
                    focus.SetAvailability(true,true,false); focus.Input.ObserveSource(9001,true,false);
                    Require(!focus.TryInspect(9001),"Held recovery inspected."); stage++;
                }
                else if (stage == 8 && elapsed >= 12)
                {
                    EditorApplication.update -= Tick;
                    File.WriteAllText(Path.Combine(folder,"result.json"),"{\"passed\":true,\"revealed_targets\":3,\"correct_inspections\":2,\"dismissal_verified\":true,\"held_selection_rejected\":true,\"tracking_reset_verified\":true,\"input_simulated\":true,\"headset_verified\":false}");
                    Debug.Log("Focus target interaction demo PASS: reveal, two correct records, dismissal and tracking reset. Input simulated.");
                    label.text += "\nPASS / three targets, two records, dismissal, tracking reset."; stage++;
                }
                if (stage < 9) label.text = "TARGET INTERACTION DEMO / SIMULATED INPUT\nStage " + stage + "/9 | Revealed " + focus.targets.RevealedCount + " | Selected " + focus.targets.SelectedIndex
                    + "\nNo headset / passthrough / physical hand proof.";
            }
            catch (Exception ex)
            {
                EditorApplication.update -= Tick;
                File.WriteAllText(Path.Combine(folder,"failure.txt"),ex.ToString()); Debug.LogException(ex);
            }
        }
        static void Require(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
