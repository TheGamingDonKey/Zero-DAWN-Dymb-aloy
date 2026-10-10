using UnityEngine;
using UnityEngine.InputSystem;
namespace MRWorkshop
{
    public sealed class WorkshopDesktopInput : MonoBehaviour
    {
        public WorkshopController workshop;
        public Camera view;
        WorkshopPart dragging;
        Plane dragPlane;
        Vector3 offset;
        const int Pointer=-7001;
        public int DragStarts { get; private set; }
        public int DragReleases { get; private set; }
        public int KeyboardActions { get; private set; }
        void Update()
        {
            var mouse=Mouse.current; var keyboard=Keyboard.current;
            if(workshop==null || view==null || !workshop.Available)return;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame)Cancel();
                if(keyboard.rKey.wasPressedThisFrame) { workshop.Action("Rotate"); KeyboardActions++; }
                if(keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame) { workshop.Action("Larger"); KeyboardActions++; }
                if(keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame) { workshop.Action("Smaller"); KeyboardActions++; }
                if(keyboard.spaceKey.wasPressedThisFrame) { workshop.Action("Power"); KeyboardActions++; }
            }
            if(mouse==null)return;
            var position=mouse.position.ReadValue(); var ray=view.ScreenPointToRay(position);
            if(mouse.leftButton.wasPressedThisFrame && position.y<Screen.height-105 && position.y>94)
            {
                if(Physics.Raycast(ray,out var hit,8))
                {
                    var part=hit.collider.GetComponentInParent<WorkshopPart>();
                    if(part!=null)
                    {
                        dragging=part; dragPlane=new Plane(Vector3.up,part.transform.position);
                        if(dragPlane.Raycast(ray,out float enter))offset=part.transform.position-ray.GetPoint(enter);
                        dragging.BeginGrab(Pointer); DragStarts++;
                    }
                }
            }
            if(dragging!=null)
            {
                if(mouse.leftButton.isPressed && dragPlane.Raycast(ray,out float enter))dragging.transform.position=ray.GetPoint(enter)+offset;
                if(mouse.leftButton.wasReleasedThisFrame) { dragging.EndGrab(Pointer); dragging=null; DragReleases++; }
            }
        }
        void Cancel() { if(dragging!=null)dragging.CancelGrab(Pointer); dragging=null; }
        void OnDisable() { Cancel(); }
        void OnApplicationFocus(bool focus) { if(!focus)Cancel(); }
        void OnGUI()
        {
            if(workshop==null)return;
            var label=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};
            GUI.Box(new Rect(8,8,Screen.width-16,92),GUIContent.none);
            GUI.Label(new Rect(20,12,Screen.width-40,24),"MR WORKSHOP / DESKTOP INPUT SIMULATION",label);
            GUI.Label(new Rect(20,36,Screen.width-40,24),"Drag a part / release cell in socket / R rotate / +/- resize / Space power / Esc cancel",label);
            string[] actions={"Power","Reset","Rotate","Smaller","Larger"};
            for(int i=0;i<actions.Length;i++)if(GUI.Button(new Rect(20+i*100,65,94,26),actions[i]))workshop.Action(actions[i]);
            GUI.Box(new Rect(8,Screen.height-94,Screen.width-16,86),GUIContent.none);
            GUI.Label(new Rect(20,Screen.height-90,Screen.width-40,82),workshop.StatusText()+"  |  No headset / passthrough proof",label);
        }
    }
}
