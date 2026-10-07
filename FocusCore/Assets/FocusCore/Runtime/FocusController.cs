using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;

namespace FocusCore
{
    public sealed class FocusController : MonoBehaviour
    {
        public Transform head;
        public Transform panel;
        public FocusVisuals visuals;
        public FocusTargets targets;
        public Text status;
        public AudioSource audioSource;
        public AudioClip chirp;
        public readonly FocusInputRouter Input = new FocusInputRouter();
        public int PlayedChirps { get; private set; }
        public Vector3 LastOrigin { get; private set; }
        bool paused, unfocused, placed, passthroughFailed;
        MetaScanInput[] sources;
        OVRDisplay subscribedDisplay;

        void Start() { sources = GetComponentsInChildren<MetaScanInput>(true); }
        void Update()
        {
            RefreshAvailability();
            if (sources != null) foreach (var source in sources) source.Sample();
            Advance(Time.unscaledDeltaTime);
        }

        public void RefreshAvailability()
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            bool xrInitialized = manager != null && manager.isInitializationComplete && OVRManager.isHmdPresent;
            if (xrInitialized && subscribedDisplay == null && OVRManager.display != null)
            {
                subscribedDisplay = OVRManager.display;
                subscribedDisplay.RecenteredPose += OnRecenter;
            }
            bool failed = xrInitialized && OVRManager.HasInsightPassthroughInitFailed();
            if (failed && !passthroughFailed) Debug.LogError("Focus passthrough initialization failed; Scan remains unavailable.");
            passthroughFailed = failed;
            bool ready = xrInitialized && OVRManager.IsInsightPassthroughInitialized() && !failed;
            bool tracked = ready && OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.Head)
                && OVRPlugin.GetNodeOrientationTracked(OVRPlugin.Node.Head);
            SetAvailability(ready, tracked, paused || unfocused || (ready && !OVRManager.hasVrFocus));
        }

        public void SetAvailability(bool ready, bool tracked, bool suspended)
        {
            Input.SetAvailability(ready, tracked, suspended);
            if (Input.Mode == FocusMode.Unavailable || Input.Mode == FocusMode.Suspended) { StopEffect(); if (targets != null) targets.Hide(); placed = false; }
            if (!placed && Input.Mode == FocusMode.Ready && panel != null && head != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
                panel.SetPositionAndRotation(head.position + forward * 0.8f - Vector3.up * 0.12f,
                    Quaternion.LookRotation(forward, Vector3.up));
                placed = true;
                if (targets != null) targets.Place(head.position,forward);
            }
            UpdateStatus();
        }

        public bool TryScan(int pointerId)
        {
            if (head == null || !Input.TryBeginScan(pointerId)) return false;
            LastOrigin = head.position;
            if (visuals != null) visuals.Begin(LastOrigin);
            if (targets != null) targets.BeginScan(LastOrigin);
            if (audioSource != null && chirp != null)
            {
                audioSource.PlayOneShot(chirp, 0.22f);
                PlayedChirps++;
            }
            UpdateStatus();
            return true;
        }

        public void Advance(float seconds)
        {
            bool wasScanning = Input.Mode == FocusMode.Scanning;
            Input.Tick(seconds);
            if (targets != null)
            {
                if (wasScanning) targets.Advance(Input.Mode == FocusMode.Scanning ? (float)Input.Progress : 1);
                if (Input.Mode == FocusMode.Ready || Input.Mode == FocusMode.Scanning) targets.UpdateGaze();
            }
            if (visuals != null)
            {
                if (Input.Mode == FocusMode.Scanning) visuals.ShowProgress((float)Input.Progress);
                else visuals.Stop();
            }
            UpdateStatus();
        }

        public bool TryInspect(int pointerId)
        {
            if (targets == null || head == null || !Input.TryConsumeSelection(pointerId)) return false;
            targets.UpdateGaze();
            bool opened = targets.InspectHovered(); UpdateStatus(); return opened;
        }
        public bool TryDismiss(int pointerId)
        {
            if (targets == null || !Input.TryConsumeSelection(pointerId)) return false;
            targets.Dismiss(); UpdateStatus(); return true;
        }

        void UpdateStatus()
        {
            if (status == null) return;
            status.text = Input.Mode == FocusMode.Scanning ? "SCAN " + Mathf.RoundToInt((float)Input.Progress * 100) + "%"
                : Input.Mode == FocusMode.Ready ? (targets != null && targets.HoveredIndex >= 0
                    ? "LOOKING AT " + targets.HoveredName + "\nChoose INSPECT to open its record"
                    : "READY / SCAN to reveal virtual targets\nLook at a marker, then choose INSPECT")
                : Input.Mode == FocusMode.Suspended ? "PAUSED / restore headset tracking"
                : passthroughFailed ? "PASSTHROUGH FAILED / restart app" : "WAITING FOR XR / PASSTHROUGH";
        }
        void StopEffect() { if (visuals != null) visuals.Stop(); if (audioSource != null) audioSource.Stop(); }
        void OnApplicationPause(bool value) { paused = value; RefreshAvailability(); }
        void OnApplicationFocus(bool value) { unfocused = !value; RefreshAvailability(); }
        void OnRecenter() { SetAvailability(true, false, false); }
        void OnDisable()
        {
            if (subscribedDisplay != null) subscribedDisplay.RecenteredPose -= OnRecenter;
            subscribedDisplay = null;
            Input.SetAvailability(false, false, true); placed = false; StopEffect();
            if (targets != null) targets.Hide();
        }
    }
}
