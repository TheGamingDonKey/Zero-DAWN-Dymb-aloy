using UnityEngine;
using UnityEngine.EventSystems;
namespace MRWorkshop
{
    public sealed class WorkshopButton : MonoBehaviour,IPointerClickHandler
    {
        public WorkshopController workshop;
        public string action;
        public void OnPointerClick(PointerEventData evt) { if(workshop!=null)workshop.Action(action); }
    }
}
