using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace MRWorkshop
{
    public sealed class WorkshopButton : MonoBehaviour,IPointerDownHandler,IPointerClickHandler
    {
        public WorkshopController workshop;
        public string action;
        struct Press { public int sourceRevision,availabilityRevision; }
        readonly Dictionary<int,Press> presses=new Dictionary<int,Press>();
        public void OnPointerDown(PointerEventData evt)
        {
            presses.Remove(evt.pointerId);
            if(workshop!=null && workshop.IsSourceAvailable(evt.pointerId) && workshop.TryGetSource(evt.pointerId,out var source))
                presses[evt.pointerId]=new Press{sourceRevision=source.Revision,availabilityRevision=workshop.AvailabilityRevision};
        }
        public void OnPointerClick(PointerEventData evt)
        {
            if(!presses.TryGetValue(evt.pointerId,out var press))return;
            presses.Remove(evt.pointerId);
            if(workshop!=null && workshop.IsSourceAvailable(evt.pointerId) && workshop.TryGetSource(evt.pointerId,out var source) &&
                source.Revision==press.sourceRevision && workshop.AvailabilityRevision==press.availabilityRevision)workshop.Action(action);
        }
        void OnDisable() { presses.Clear(); }
    }
}
