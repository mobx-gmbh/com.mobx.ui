using Cysharp.Threading.Tasks;
using MobX.Mediator.Callbacks;
using MobX.Mediator.Collections;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace MobX.UI
{
    public class OnBackPressedStack : StackAsset<IBackPressedConsumer>
    {
        [SerializeField] [Required] private InputActionReference onBackPressed;
        [NonSerialized] [ReadOnly] [ShowInInspector]
        private int _locks;

        [CallbackOnInitialization]
        private void Initialize()
        {
            Assert.IsNotNull(onBackPressed);
            Assert.IsNotNull(onBackPressed.action);
            onBackPressed.action.performed -= OnBackPressed;
            onBackPressed.action.performed += OnBackPressed;
        }

        [CallbackOnApplicationQuit]
        private void Shutdown()
        {
            _locks = 0;
            onBackPressed.action.performed -= OnBackPressed;
        }

        private void OnBackPressed(InputAction.CallbackContext context)
        {
            if (_locks > 0)
            {
                return;
            }
            foreach (var consumer in this.Reverse())
            {
                if (consumer.ConsumeBackPressed())
                {
                    break;
                }
            }
        }

        public void Lock()
        {
            _locks++;
        }

        public async void Unlock()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            _locks--;
        }
    }
}