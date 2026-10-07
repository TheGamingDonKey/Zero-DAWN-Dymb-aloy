using UnityEngine;
using UnityEngine.UI;

namespace FocusCore
{
    // Authored geometry only. This component does not use cameras or identify real objects.
    public sealed class FocusTargets : MonoBehaviour
    {
        public Transform head;
        public RectTransform panel;
        public Material targetMaterial;
        public FocusController focus;
        public int RevealedCount { get; private set; }
        public int HoveredIndex { get; private set; } = -1;
        public int SelectedIndex { get; private set; } = -1;
        public bool InformationVisible => SelectedIndex >= 0;
        public string HoveredName => HoveredIndex >= 0 ? Names[HoveredIndex] : null;
        static readonly string[] Names = { "SIGNAL RELAY", "ENERGY CELL", "DATA CACHE" };
        static readonly string[] Descriptions = {
            "VIRTUAL DEMO / COMMUNICATIONS\nA relay node authored for this scene.\nStatus: signal available.\nNo physical device has been detected.",
            "VIRTUAL DEMO / POWER\nA synthetic energy cell with authored data.\nStatus: charge stable.\nThe Focus is not measuring real electricity.",
            "VIRTUAL DEMO / ARCHIVE\nA geometric cache containing this demo entry.\nStatus: record accessible.\nReal object recognition is a later feature."
        };
        static readonly Vector3[] Offsets = {
            new Vector3(-.62f,.12f,1.7f), new Vector3(0,.32f,1.95f), new Vector3(.62f,.12f,1.7f)
        };
        readonly GameObject[] objects = new GameObject[3];
        readonly Transform[] labels = new Transform[3];
        readonly Renderer[][] surfaces = new Renderer[3][];
        readonly bool[] revealed = new bool[3];
        Transform field;
        Text informationTitle, informationBody;
        Material ownedMaterial;
        MaterialPropertyBlock properties;
        Vector3 scanOrigin;
        bool initialized, scanning;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            properties = new MaterialPropertyBlock();
            if (targetMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                ownedMaterial = new Material(shader) { name = "FocusDemoTargetMaterial" };
                targetMaterial = ownedMaterial;
            }
            field = new GameObject("AuthoredVirtualTargets").transform;
            field.SetParent(transform,false);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var shapes = new[] {PrimitiveType.Cube,PrimitiveType.Sphere,PrimitiveType.Capsule};
            for (int i=0;i<objects.Length;i++)
            {
                var item = new GameObject(Names[i] + " / VIRTUAL DEMO");
                item.transform.SetParent(field,false); item.transform.localPosition = Offsets[i];
                var body = GameObject.CreatePrimitive(shapes[i]); body.transform.SetParent(item.transform,false);
                body.name = "OriginalPrimitive"; body.transform.localScale = Vector3.one * .18f;
                var collider = body.GetComponent<Collider>();
                if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
                var surface = body.GetComponent<Renderer>(); surface.sharedMaterial = targetMaterial;
                surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; surface.receiveShadows = false;
                // Four open corner brackets, authored from line segments; no detailed model needed.
                for (int corner=0;corner<4;corner++)
                {
                    float x = corner%2==0 ? -1 : 1, y = corner<2 ? -1 : 1;
                    var bracket = new GameObject("FocusCorner").AddComponent<LineRenderer>();
                    bracket.transform.SetParent(item.transform,false); bracket.useWorldSpace = false;
                    bracket.positionCount = 3; bracket.widthMultiplier = .004f;
                    bracket.sharedMaterial = targetMaterial;
                    bracket.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    bracket.SetPositions(new[] { new Vector3(x*.10f,y*.16f,-.13f), new Vector3(x*.16f,y*.16f,-.13f),new Vector3(x*.16f,y*.10f,-.13f) });
                }
                var label = new GameObject("VirtualTargetLabel",typeof(RectTransform),typeof(Canvas));
                label.transform.SetParent(item.transform,false); label.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var labelRect = (RectTransform)label.transform; labelRect.sizeDelta = new Vector2(360,90);
                labelRect.localScale = Vector3.one * .0015f; labelRect.localPosition = Vector3.up * .27f;
                Label(labelRect,font,(i+1).ToString("00") + " / " + Names[i] + "\nVIRTUAL DEMO",Vector2.zero,new Vector2(360,90),22,TextAnchor.MiddleCenter);
                objects[i] = item; labels[i] = label.transform;
                surfaces[i] = item.GetComponentsInChildren<Renderer>(true);
                Paint(i); item.SetActive(false);
            }
            if (panel != null)
            {
                informationTitle = Label(panel,font,"SCAN TO REVEAL TARGETS",new Vector2(0,-88),new Vector2(540,46),26);
                informationBody = Label(panel,font,"Three authored virtual objects.\nLook at a revealed marker, then choose INSPECT.\nThese are demo data, not detected furniture.",new Vector2(0,-185),new Vector2(540,128),21);
            }
        }

        public void Place(Vector3 anchor, Vector3 forward)
        {
            Initialize(); Hide();
            forward = Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            field.SetPositionAndRotation(anchor,Quaternion.LookRotation(forward,Vector3.up));
        }

        public void BeginScan(Vector3 origin)
        {
            Initialize(); scanOrigin = origin; scanning = true;
            // Revealed targets persist until tracking is lost or the field is replaced.
            Dismiss();
        }

        public void Advance(float progress)
        {
            if (!scanning) return;
            float radius = FocusVisuals.RadiusAtProgress(progress);
            for (int i=0;i<objects.Length;i++)
                if (!revealed[i] && Vector3.Distance(scanOrigin,objects[i].transform.position) <= radius)
                {
                    revealed[i] = true; RevealedCount++; objects[i].SetActive(true);
                    if (SelectedIndex < 0 && informationTitle != null) informationTitle.text = "TARGETS REVEALED / " + RevealedCount;
                }
            if (progress >= 1) scanning = false;
        }

        public void UpdateGaze()
        {
            if (!initialized || head == null) return;
            int previous = HoveredIndex; HoveredIndex = -1;
            float best = Mathf.Cos(9f * Mathf.Deg2Rad);
            for (int i=0;i<objects.Length;i++)
            {
                if (!revealed[i]) continue;
                var direction = objects[i].transform.position-head.position;
                float alignment = Vector3.Dot(head.forward,direction.normalized);
                if (direction.sqrMagnitude > .04f && direction.sqrMagnitude <= 36f && alignment > best)
                { best = alignment; HoveredIndex = i; }
                labels[i].rotation = Quaternion.LookRotation(labels[i].position-head.position,Vector3.up);
            }
            if (previous != HoveredIndex)
            {
                if (previous >= 0) Paint(previous);
                if (HoveredIndex >= 0) Paint(HoveredIndex);
            }
        }

        public bool InspectHovered()
        {
            if (HoveredIndex < 0 || !revealed[HoveredIndex]) return false;
            int previous = SelectedIndex; SelectedIndex = HoveredIndex;
            if (previous >= 0) Paint(previous); Paint(SelectedIndex);
            if (informationTitle != null) informationTitle.text = Names[SelectedIndex] + " / IDENTIFIED";
            if (informationBody != null) informationBody.text = Descriptions[SelectedIndex];
            return true;
        }

        public void Dismiss()
        {
            int previous = SelectedIndex; SelectedIndex = -1;
            if (previous >= 0) Paint(previous);
            if (informationTitle != null) informationTitle.text = RevealedCount > 0 ? "TARGETS REVEALED / " + RevealedCount : "SCAN TO REVEAL TARGETS";
            if (informationBody != null) informationBody.text = "Look at a virtual marker, then choose INSPECT.\nPinch/ray or direct poke; controller fallback.\nCLOSE dismisses the information card.";
        }

        public void Hide()
        {
            scanning = false; RevealedCount = 0; HoveredIndex = -1;
            Dismiss();
            for (int i=0;i<objects.Length;i++)
            { revealed[i] = false; if (objects[i] != null) { Paint(i); objects[i].SetActive(false); } }
        }

        public Vector3 GetTargetPosition(int index) { Initialize(); return objects[index].transform.position; }

        void Paint(int i)
        {
            if (surfaces[i] == null) return;
            var colour = i == SelectedIndex ? new Color(.32f,.86f,1,1)
                : i == HoveredIndex ? new Color(.84f,.81f,1,1) : new Color(.58f,.34f,.95f,1);
            properties.SetColor("_BaseColor",colour); properties.SetColor("_Color",colour);
            foreach (var surface in surfaces[i]) surface.SetPropertyBlock(properties);
            objects[i].transform.localScale = Vector3.one * (i == HoveredIndex ? 1.12f : 1);
        }

        static Text Label(RectTransform parent, Font font, string content, Vector2 position, Vector2 size, int points, TextAnchor alignment=TextAnchor.UpperLeft)
        {
            var go = new GameObject("FocusInformation",typeof(RectTransform),typeof(Text));
            var rect = (RectTransform)go.transform; rect.SetParent(parent,false);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            var text = go.GetComponent<Text>(); text.font = font; text.text = content; text.fontSize = points;
            text.color = new Color(.89f,.88f,1); text.raycastTarget = false; text.alignment = alignment;
            return text;
        }
        void OnDisable() { if (initialized) Hide(); }
        void OnDestroy() { if (ownedMaterial != null) Destroy(ownedMaterial); }
    }
}
