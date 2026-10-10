using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MRWorkshop.Editor
{
    // Exercise the real Update input driver, rather than calling its grab callbacks.
    [InitializeOnLoad]
    public static class WorkshopDesktopInputProof
    {
        const string Requested = "MRWorkshop.DesktopInputProofRequested";
        static WorkshopDesktopInput driver;
        static Mouse mouse, previousMouse;
        static Keyboard keyboard, previousKeyboard;
        static InputSettings.BackgroundBehavior previousBackground;
        static bool settingsChanged;
        static double started, next;
        static double warmupDeadline;
        static bool warmedUp;
        static int stage;
        static string folder;
        static Quaternion rotorAtPower;
        static WorkshopDesktopInputProof() { EditorApplication.playModeStateChanged += StateChanged; }

        [MenuItem("Workshop/Run Finite Desktop Input Diagnostic (Injected Devices)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before running the input diagnostic.");
            SessionState.SetBool(Requested, true);
            try { WorkshopSceneSetup.OpenDesktop(); }
            catch { SessionState.SetBool(Requested, false); throw; }
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.artifacts/workshop/input-proof-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
                try
                {
                Directory.CreateDirectory(folder);
                driver = UnityEngine.Object.FindFirstObjectByType<WorkshopDesktopInput>();
                previousMouse = Mouse.current; previousKeyboard = Keyboard.current;
                previousBackground = InputSystem.settings.backgroundBehavior;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settingsChanged = true;
                mouse = InputSystem.AddDevice<Mouse>("WorkshopDiagnosticMouse");
                keyboard = InputSystem.AddDevice<Keyboard>("WorkshopDiagnosticKeyboard");
                started = EditorApplication.timeSinceStartup; next = started + 1; stage = 0;
                warmupDeadline = started + 90; warmedUp = false;
                EditorApplication.update += Tick;
                }
                catch (Exception error) { Finish(false, error.ToString()); }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Requested, false); WorkshopBootstrap.OpenScene();
            }
        }
        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            try
            {
                // First GUI rendering/import can block the Editor after EnteredPlayMode.
                // Keep startup separate from the eighteen-second interaction budget.
                if (!warmedUp || EditorApplication.isUpdating || EditorApplication.isCompiling)
                {
                    if (now > warmupDeadline) throw new TimeoutException("Desktop input warmup exceeded ninety seconds.");
                    started = now; next = now + .75; warmedUp = true; return;
                }
                if (now - started > 18) throw new TimeoutException("Desktop input diagnostic exceeded eighteen seconds.");
                if (now < next) return;
                Require(driver != null && driver.workshop.Available, "Desktop driver unavailable.");
                var workshop = driver.workshop;
                var cell = workshop.parts[0];
                Vector2 home = driver.view.WorldToScreenPoint(cell.transform.position);
                var destination = workshop.socket.position;
                destination.y = cell.transform.position.y;
                Vector2 socket = driver.view.WorldToScreenPoint(destination);
                switch (stage)
                {
                    case 0:
                        Require(home.y > 94 && home.y < Screen.height - 105, "Cell is obscured by the desktop GUI.");
                        QueueMouse(home, false); break;
                    case 1: QueueMouse(home, true); break;
                    case 2:
                        Require(cell.IsHeld && driver.DragStarts == 1, "Mouse press did not reach the real driver.");
                        QueueMouse(socket, true); break;
                    case 3: QueueMouse(socket, false); break;
                    case 4:
                        Require(cell.IsDocked && driver.DragReleases == 1, "Mouse drag/release failed to dock the cell.");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); break;
                    case 5: InputSystem.QueueStateEvent(keyboard, new KeyboardState()); break;
                    case 6:
                        Require(workshop.State.Running && driver.KeyboardActions == 1, "Space did not reach the real driver.");
                        rotorAtPower = workshop.rotor.localRotation; break;
                    case 7:
                        Require(Quaternion.Angle(rotorAtPower, workshop.rotor.localRotation) > 1, "Powered rotor did not move.");
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "mouse-keyboard-workshop.png")); break;
                    case 8: Finish(true, "Real Update driver consumed injected Mouse/Keyboard state; cell dragged, released, docked; Space powered moving rotor."); return;
                }
                stage++; next = now + (stage == 7 || stage == 8 ? 1 : .35);
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }
        static void QueueMouse(Vector2 position, bool pressed)
        {
            var state = new MouseState { position = position };
            if (pressed) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state);
        }
        static void Finish(bool passed, string message)
        {
            try
            {
                File.WriteAllText(Path.Combine(folder, "result.json"), JsonUtility.ToJson(new Result
                {
                passed = passed, stages = stage, injectedDeviceEvents = true, humanInputVerified = false,
                physicalHeadsetVerified = false, dragStarts = driver != null ? driver.DragStarts : 0,
                dragReleases = driver != null ? driver.DragReleases : 0,
                keyboardActions = driver != null ? driver.KeyboardActions : 0, checks = message
                }, true));
            if (passed) Debug.Log("Workshop desktop input diagnostic passed: " + folder);
            else Debug.LogError("Workshop desktop input diagnostic failed: " + message);
            }
            catch (Exception reportError) { Debug.LogError("Could not save desktop input evidence: " + reportError); }
            finally { try { Cleanup(); } finally { EditorApplication.isPlaying = false; } }
        }
        static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            mouse = null; keyboard = null;
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (settingsChanged) InputSystem.settings.backgroundBehavior = previousBackground;
            settingsChanged = false;
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        [Serializable] sealed class Result
        {
            public bool passed, injectedDeviceEvents, humanInputVerified, physicalHeadsetVerified;
            public int stages, dragStarts, dragReleases, keyboardActions;
            public string checks;
        }
    }
}
