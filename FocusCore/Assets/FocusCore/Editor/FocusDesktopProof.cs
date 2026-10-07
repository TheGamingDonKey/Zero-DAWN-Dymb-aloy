using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FocusCore.Editor
{
    // Verification harness only: this Editor assembly and diagnostic scene are excluded from the Core APK.
    [InitializeOnLoad]
    public static class FocusDesktopProof
    {
        const string Requested = "FocusCore.DesktopProof.Requested";
        static double started, lastScan;
        static int completed;
        static FocusController focus;
        static Text label;
        static FocusDesktopProof() { EditorApplication.playModeStateChanged += StateChanged; }

        [MenuItem("Focus/Run Desktop Setup Proof (Simulated Input)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop PlayMode before starting setup proof.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name = "Desktop Setup Proof - Simulated Input";
            var root = new GameObject("DesktopProofActualFocusComponents");
            var controller = root.AddComponent<FocusController>(); controller.enabled = false;
            var head = new GameObject("SimulatedHead"); head.transform.SetParent(root.transform,false);
            controller.head = head.transform;
            var pulse = new GameObject("ActualFocusScanLattice"); pulse.transform.SetParent(root.transform,false);
            controller.visuals = pulse.AddComponent<FocusVisuals>();
            controller.visuals.pulseMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/FocusCore/Art/FocusPulse.mat");
            controller.audioSource = root.AddComponent<AudioSource>(); controller.audioSource.playOnAwake = false;
            controller.chirp = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FocusCore/Art/FocusChirp.wav");
            if (controller.chirp == null || controller.visuals.pulseMaterial == null)
                throw new InvalidOperationException("Proof cannot run: imported Focus audio/material missing.");
            if (ShaderUtil.ShaderHasError(controller.visuals.pulseMaterial.shader))
                throw new InvalidOperationException("Focus shader has compilation errors.");
            var camera = new GameObject("DesktopProofCamera",typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0,0,-2.4f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.035f,.07f);
            camera.gameObject.AddComponent<AudioListener>();
            var canvas = new GameObject("ProofOverlay",typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var textObject = new GameObject("ProofStatus",typeof(RectTransform),typeof(Text));
            textObject.transform.SetParent(canvas.transform,false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0,1); rect.anchorMax = new Vector2(1,1); rect.pivot = new Vector2(.5f,1);
            rect.sizeDelta = new Vector2(-40,155); rect.anchoredPosition = new Vector2(0,-20);
            var text = textObject.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22; text.color = Color.white;
            text.text = "DESKTOP SETUP PROOF / SIMULATED INPUT\nActual Focus lattice, shader, audio and scan controller\nNo headset / no passthrough / no real hand tracking";
            if (!EditorSceneManager.SaveScene(scene,"Assets/FocusCore/Scenes/DesktopSetupProof.unity"))
                throw new IOException("Could not save the diagnostic scene in the real project.");
            SessionState.SetBool(Requested,true);
            EditorApplication.isPlaying = true;
        }

        static void StateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested,false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                focus = UnityEngine.Object.FindFirstObjectByType<FocusController>();
                label = GameObject.Find("ProofStatus").GetComponent<Text>();
                started = EditorApplication.timeSinceStartup; lastScan = -100; completed = 0;
                EditorApplication.update += Tick;
                var gameView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                EditorWindow.GetWindow(gameView).Focus();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Requested,false);
                EditorSceneManager.OpenScene(FocusDemoSetup.CoreScene);
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
            }
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || focus == null) return;
            double elapsed = EditorApplication.timeSinceStartup - started;
            focus.SetAvailability(true,true,false);
            if (completed < 6 && elapsed-lastScan >= 3.0)
            {
                focus.Input.ObserveSource(9001,true,true);
                if (!focus.TryScan(9001)) throw new InvalidOperationException("Synthetic neutral scan was rejected.");
                focus.Input.ObserveSource(9002,true,true);
                if (focus.TryScan(9002)) throw new InvalidOperationException("Duplicate scan was accepted.");
                completed++; lastScan = elapsed;
            }
            focus.Advance(Time.unscaledDeltaTime);
            label.text = "DESKTOP SETUP PROOF / SIMULATED INPUT\nActual Focus lattice, shader, audio and controller\nScans: " + completed + "/6 | Chirps: " + focus.PlayedChirps + " | " + focus.Input.Mode
                + "\nNo headset / passthrough / hand-tracking proof";
            if (elapsed >= 20)
            {
                EditorApplication.update -= Tick;
                if (focus.Input.AcceptedScans != 6 || focus.PlayedChirps != 6 || focus.visuals.Active)
                    throw new InvalidOperationException("Desktop proof did not finish with six scans/chirps and no active pulse.");
                label.text += "\nPASS: six finite pulses; duplicates rejected; six chirps.";
                string evidence = Path.GetFullPath(Path.Combine(Application.dataPath,"../../.artifacts/research/desktop-proof.json"));
                Directory.CreateDirectory(Path.GetDirectoryName(evidence));
                File.WriteAllText(evidence,"{\"passed\":true,\"scans\":6,\"chirps\":6,\"pulse_stopped\":true,\"input_simulated\":true,\"headset_verified\":false}");
                Debug.Log("Desktop Focus proof PASS: six finite scans/chirps; duplicate requests rejected. Input simulated.");
            }
        }
    }
}
