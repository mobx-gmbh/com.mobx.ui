using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MobX.UI.Mediator
{
    public class SelectedEvent : MonoBehaviour, ISelectHandler
    {
        public event Action Selected;

        public void OnSelect(BaseEventData eventData)
        {
            Selected?.Invoke();
        }
    }
}