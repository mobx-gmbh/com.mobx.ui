using Cysharp.Threading.Tasks;
using MobX.Inspector;
using MobX.Mediator.Events;
using MobX.Utilities;
using PrimeTween;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobX.UI
{
    public abstract partial class UIComponent
    {
        #region Fields

        [Foldout("Settings")]
        [SerializeField] private bool isSceneObject;
        [SerializeField] private bool hideUnderlyingUI;
        [SerializeField] private bool startVisibility = true;
        [SerializeField] private bool autoSelectFirstObject = true;
        [HideIf(nameof(autoSelectFirstObject))]
        [SerializeField] [Required] private Selectable firstSelected;
        [Tooltip("Standalone means that the UI can be handled without influencing or being influenced by other UI")]
        [SerializeField] private bool standalone;

        [Space]
        [HideIf(nameof(standalone))]
        [Tooltip("Determine when the view will start to receive and consume escape/return inputs")]
        [SerializeField] private OpenSegment gainBackPressedInputDuring = OpenSegment.BeforeOpenSequence;
        [HideIf(nameof(standalone))]
        [Tooltip("Determine when the view will stop to receive and consume escape/return inputs")]
        [SerializeField] private CloseSegment loseBackPressedInputDuring = CloseSegment.BeforeCloseSequence;

        [Header("References")]
        [HideIf(nameof(standalone))]
        [SerializeField] [Required] private UIStack uiStack;
        [HideIf(nameof(standalone))]
        [SerializeField] [Required] private OnBackPressedStack consumerStack;

        [Header("Debug")]
        [ReadOnly]
        [SerializeField] [Required] private Canvas canvas;
        [ReadOnly]
        [SerializeField] [Required] private CanvasGroup canvasGroup;
        [ReadOnly]
        [SerializeField] private Button[] buttons;
        [ReadOnly]
        [SerializeField] private Selectable[] selectables;

        #endregion


        #region Fields

        private readonly Broadcast _opened = new();
        private readonly Broadcast _closed = new();
        private readonly Broadcast _opening = new();
        private readonly Broadcast _closing = new();
        private readonly Broadcast<ViewState> _stateChanged = new();

        private Sequence _showSequence;
        private Sequence _hideSequence;

        private GameObject _lastSelected;
        private Action _forceSelectObject;
        private Action _forceDeselectObject;
        private Action<Selectable> _cacheSelection;

        private enum OpenSegment
        {
            BeforeOpenSequence,
            AfterOpenSequence
        }

        private enum CloseSegment
        {
            BeforeCloseSequence,
            AfterCloseSequence
        }

        #endregion


        #region Properties

        private void SetViewState(ViewState value)
        {
            if (_state == value)
            {
                return;
            }
            _state = value;
            _stateChanged.Raise(value);
            switch (value)
            {
                case ViewState.Open:
                    _opened.Raise();
                    break;
                case ViewState.Opening:
                    _opening.Raise();
                    break;
                case ViewState.Closed:
                    _closed.Raise();
                    break;
                case ViewState.Closing:
                    _closing.Raise();
                    break;
            }
        }

        private ViewState _state = ViewState.None;

        #endregion


        #region Open

        private void OpenInternal()
        {
            OpenAsyncInternal().Forget();
        }

        private async UniTask OpenAsyncInternal()
        {
            IsOrWillOpen = true;
            if (State is ViewState.Open)
            {
                return;
            }

            if (State is ViewState.Opening)
            {
                _showSequence.Complete();
                return;
            }

            State = ViewState.Opening;
            KillRunningTween();
            gameObject.SetActive(true);
            if (gainBackPressedInputDuring == OpenSegment.BeforeOpenSequence)
            {
                consumerStack.PushUnique(this);
            }
            if (standalone is false && hideUnderlyingUI && uiStack.TryPeek(out var underlyingUI))
            {
                var hideSequence = underlyingUI.HideSequence();
                consumerStack.Lock();
                await hideSequence.ToYieldInstruction();
                consumerStack.Unlock();
            }
            AddToStack(this);
            _showSequence = OnShowAsync();
            await _showSequence.ToYieldInstruction();
            IsVisible = true;
            if (gainBackPressedInputDuring == OpenSegment.AfterOpenSequence)
            {
                consumerStack.PushUnique(this);
            }
            if (State != ViewState.Opening)
            {
                return;
            }

            State = ViewState.Open;
        }

        private void OpenImmediateInternal()
        {
            IsOrWillOpen = true;
            if (State is ViewState.Open)
            {
                return;
            }

            if (State is ViewState.Opening)
            {
                _showSequence.Complete();
                return;
            }

            State = ViewState.Opening;
            KillRunningTween();

            gameObject.SetActive(true);
            _showSequence = OnShowAsync();
            _showSequence.Complete();
            IsVisible = true;
            consumerStack.PushUnique(this);
            AddToStack(this);
            _opened.Raise();

            State = ViewState.Open;
        }

        #endregion


        #region Close

        private void CloseInternal()
        {
            CloseAsyncInternal().Forget();
        }

        private async UniTask CloseAsyncInternal()
        {
            IsOrWillOpen = false;
            if (State is ViewState.Closed)
            {
                return;
            }

            if (State is ViewState.Closing)
            {
                _hideSequence.Complete();
                return;
            }

            State = ViewState.Closing;
            KillRunningTween();
            if (loseBackPressedInputDuring == CloseSegment.BeforeCloseSequence)
            {
                consumerStack.Remove(this);
            }
            _hideSequence = CloseSequence();
            await _hideSequence.ToYieldInstruction();

            if (State != ViewState.Closing)
            {
                return;
            }

            State = ViewState.Closed;
            return;

            Sequence CloseSequence()
            {
                var sequence = Sequence.Create();
                sequence.Chain(OnHideAsync());
                if (loseBackPressedInputDuring == CloseSegment.AfterCloseSequence)
                {
                    sequence.ChainCallback(() => consumerStack.Remove(this));
                }
                sequence.ChainCallback(this, target => target.gameObject.SetActive(false));
                sequence.ChainCallback(this, target => target.RemoveFromStack(this));
                sequence.ChainCallback(this, target => target.IsVisible = false);
                return sequence;
            }
        }

        private void CloseImmediateInternal()
        {
            IsOrWillOpen = false;
            if (State is ViewState.Closed)
            {
                return;
            }

            if (State is ViewState.Closing)
            {
                _hideSequence.Complete();
                IsVisible = false;
                return;
            }

            KillRunningTween();
            State = ViewState.Closing;
            _hideSequence = OnHideAsync();
            _hideSequence.Complete();
            IsVisible = false;
            consumerStack.Remove(this);
            RemoveFromStack(this);
            gameObject.SetActive(false);
            State = ViewState.Closed;
        }

        #endregion


        #region Show & Hide

        private void ShowInternal()
        {
            KillRunningTween();
            _showSequence = OnShowAsync();
            _showSequence.OnComplete(this, target => target.IsVisible = true);
        }

        private void HideInternal()
        {
            KillRunningTween();
            _hideSequence = OnHideAsync();
            _hideSequence.OnComplete(this, target => target.IsVisible = false);
        }

        #endregion


        #region Unity Callbacks

        protected virtual void Awake()
        {
            if (startVisibility is false)
            {
                CanvasGroup.alpha = 0;
                IsVisible = false;
            }
        }

        protected virtual void Start()
        {
            if (isSceneObject)
            {
                State = ViewState.Open;
                AddToStack(this);
                consumerStack.PushUnique(this);
                if (startVisibility is false)
                {
                    Show();
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            canvas ??= GetComponent<Canvas>();
            canvasGroup ??= GetComponent<CanvasGroup>();
            buttons = GetComponentsInChildren<Button>(true);
            selectables = GetComponentsInChildren<Selectable>(true);
        }
#endif

        protected virtual void OnDestroy()
        {
            KillRunningTween();
            Controls.NavigationInputReceived -= _forceSelectObject;
            Controls.BecameControllerScheme -= _forceSelectObject;
            Controls.BecameDesktopScheme -= _forceDeselectObject;
            Controls.SelectionChanged -= _cacheSelection;
            uiStack.Remove(this);
            consumerStack.Remove(this);
        }

        #endregion


        #region Misc

        private void KillRunningTween()
        {
            if (_showSequence.isAlive)
            {
                _showSequence.Stop();
            }
            if (_hideSequence.isAlive)
            {
                _hideSequence.Stop();
            }
        }

        private void AddToStack(UIComponent uiComponent)
        {
            if (standalone)
            {
                return;
            }
            if (uiStack.TryPeek(out var view) && view != uiComponent)
            {
                view.OnLoseFocusInternal();
                if (hideUnderlyingUI)
                {
                    view.Hide();
                }
            }
            uiStack.PushUnique(uiComponent);
            uiComponent.OnGainFocusInternal();
        }

        private void RemoveFromStack(UIComponent uiComponent)
        {
            if (standalone)
            {
                return;
            }
            var hasRemovedComponentFocus = uiStack.Peek() == uiComponent;
            uiStack.Remove(uiComponent);

            if (hasRemovedComponentFocus is false)
            {
                return;
            }

            uiComponent.OnLoseFocusInternal();
            if (uiStack.TryPeek(out var nextUIComponent) && nextUIComponent != uiComponent)
            {
                nextUIComponent.OnGainFocusInternal();
                if (uiComponent.hideUnderlyingUI)
                {
                    nextUIComponent.Show();
                }
            }
        }

        protected void DisableSelectables()
        {
            foreach (var selectable in selectables)
            {
#if DEBUG
                if (selectable == null)
                {
                    Debug.LogWarning("UI", $"Cached selectable is null! {name}", this);
                    continue;
                }
#endif
                selectable.interactable = false;
            }
        }

        protected void EnableSelectables()
        {
            foreach (var selectable in selectables)
            {
#if DEBUG
                if (selectable == null)
                {
                    Debug.LogWarning("UI", $"Cached selectable is null! {name}", this);
                    continue;
                }
#endif
                selectable.interactable = true;
            }
        }

        #endregion


        #region Focus Handling

        private void OnGainFocusInternal()
        {
            OnGainFocus();

            _forceSelectObject ??= ForceSelectObject;
            _forceDeselectObject ??= ForceDeselectObject;
            _cacheSelection ??= CacheSelection;

            Controls.NavigationInputReceived += _forceSelectObject;
            Controls.BecameControllerScheme += _forceSelectObject;
            Controls.BecameDesktopScheme += _forceDeselectObject;
            Controls.SelectionChanged += _cacheSelection;

            if (Controls.IsGamepadScheme || Controls.InteractionMode == InteractionMode.NavigationInput)
            {
                ForceSelectObject();
            }
        }

        private void OnLoseFocusInternal()
        {
            Controls.NavigationInputReceived -= _forceSelectObject;
            Controls.BecameControllerScheme -= _forceSelectObject;
            Controls.BecameDesktopScheme -= _forceDeselectObject;
            Controls.SelectionChanged -= _cacheSelection;

            _lastSelected = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null);
            OnLoseFocus();
        }

        private async void ForceSelectObject()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            var objectToSelect = GetObjectToSelect();
            if (objectToSelect != null)
            {
                EventSystem.current.SetSelectedGameObject(objectToSelect);
            }
        }

        private void ForceDeselectObject()
        {
            _lastSelected = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void CacheSelection(Selectable selectable)
        {
            if (selectables.Contains(selectable))
            {
                _lastSelected = selectable.gameObject;
            }
        }

        private GameObject GetObjectToSelect()
        {
            // Check if the currently selected object is already viable.

            if (Controls.HasSelected && Controls.Selected.IsActiveInHierarchy())
            {
                var selectedObject = Controls.Selected;
                if (selectedObject.interactable && Selectables.Contains(selectedObject))
                {
                    return selectedObject.gameObject;
                }
            }

            // Check if the last selected object is viable.
            var lastSelectedIsViable = _lastSelected && _lastSelected.activeInHierarchy;
            if (lastSelectedIsViable)
            {
                return _lastSelected;
            }

            // Get a predetermined first selection object.
            if (autoSelectFirstObject && firstSelected)
            {
                return firstSelected.gameObject;
            }

            // Try to return the first found selectable component.
            var defaultSelection = Selectables.FirstOrDefault();
            return defaultSelection != null ? defaultSelection.gameObject : null;
        }

        #endregion
    }
}