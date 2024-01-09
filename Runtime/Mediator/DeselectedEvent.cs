using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MobX.UI.Mediator
{
    public class DeselectedEvent : MonoBehaviour, IDeselectHandler
    {
        public event Action Deselected;

        public void OnDeselect(BaseEventData eventData)
        {
            Deselected?.Invoke();
        }
    }
}