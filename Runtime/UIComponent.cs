using Cysharp.Threading.Tasks;
using MobX.Inspector;
using PrimeTween;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MobX.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(UIScaleController))]
    public abstract partial class UIComponent : MonoBehaviour, IUIElement
    {
        #region Events

        public event Action Opened
        {
            add => _opened.Add(value);
            remove => _opened.Remove(value);
        }

        public event Action Closed
        {
            add => _closed.Add(value);
            remove => _closed.Remove(value);
        }

        public event Action Opening
        {
            add => _opening.Add(value);
            remove => _opening.Remove(value);
        }

        public event Action Closing
        {
            add => _closing.Add(value);
            remove => _closing.Remove(value);
        }

        public event Action<ViewState> StateChanged
        {
            add => _stateChanged.AddUnique(value);
            remove => _stateChanged.Remove(value);
        }

        #endregion


        #region Properties

        public ViewState State
        {
            get => _state;
            private set => SetViewState(value);
        }

        public bool IsVisible { get; private set; } = true;
        public bool IsOrWillOpen { get; private set; }
        public bool IsOrWillClose => !IsOrWillOpen;

        public Canvas Canvas => canvas;

        public UIAsset Asset { get; internal set; }

        protected CanvasGroup CanvasGroup => canvasGroup;

        protected Button[] Buttons => buttons;

        protected Selectable[] Selectables => selectables;

        #endregion


        #region Methods

        [Line]
        [Button]
        [Foldout("Settings")]
        public void Open()
        {
            OpenInternal();
        }

        [Button]
        [Foldout("Settings")]
        public void OpenImmediate()
        {
            OpenImmediateInternal();
        }

        [Button]
        [Foldout("Settings")]
        public UniTask OpenAsync()
        {
            return OpenAsyncInternal();
        }

        [PropertySpace]
        [Button]
        [Foldout("Settings")]
        public void Close()
        {
            CloseInternal();
        }

        [Button]
        [Foldout("Settings")]
        public void CloseImmediate()
        {
            CloseImmediateInternal();
        }

        [Button]
        [Foldout("Settings")]
        public UniTask CloseAsync()
        {
            return CloseAsyncInternal();
        }

        #endregion


        #region Hide & Show

        public void Show()
        {
            ShowInternal();
        }

        public void Hide()
        {
            HideInternal();
        }

        public Sequence ShowSequence()
        {
            ShowInternal();
            return _showSequence;
        }

        public Sequence HideSequence()
        {
            HideInternal();
            return _hideSequence;
        }

        #endregion


        #region Virtual Callbacks

        /// <summary>
        ///     Override this method to receive and optionally consume 'Back Pressed' or 'Return' callbacks on the UI.
        /// </summary>
        public virtual bool ConsumeBackPressed()
        {
            return false;
        }

        /// <summary>
        ///     Called on the component to play custom opening or fade in effects.
        /// </summary>
        protected virtual Sequence OnShowAsync()
        {
            Tween.StopAll(this);
            Tween.StopAll(CanvasGroup);
            var sequence = Sequence.Create(Tween.Alpha(CanvasGroup, 1, .3f));
            return sequence;
        }

        /// <summary>
        ///     Called on the component to play custom closing or fade out effects.
        /// </summary>
        protected virtual Sequence OnHideAsync()
        {
            Tween.StopAll(this);
            Tween.StopAll(CanvasGroup);
            var sequence = Sequence.Create(Tween.Alpha(CanvasGroup, 0, .3f));
            return sequence;
        }

        /// <summary>
        ///     Called when the component becomes the upper most view component.
        /// </summary>
        protected virtual void OnGainFocus()
        {
        }

        /// <summary>
        ///     Called when the component is no longer the upper most view component.
        /// </summary>
        protected virtual void OnLoseFocus()
        {
        }

        #endregion
    }
}