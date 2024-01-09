using PrimeTween;
using UnityEngine;

namespace MobX.UI
{
    public class HUD : UIComponent
    {
        [SerializeField] private float fadeInDuration;
        [SerializeField] private float fadeOutDuration;

        protected override Sequence OnShowAsync()
        {
            CanvasGroup.alpha = 0;
            Tween.StopAll(this);
            var tween = Tween.Custom(this, CanvasGroup.alpha, 1, fadeInDuration,
                (controller, value) => controller.CanvasGroup.alpha = value);
            return Sequence.Create(tween);
        }

        protected override Sequence OnHideAsync()
        {
            Tween.StopAll(this);
            var sequence = Sequence.Create();
            var tween = Tween.Custom(this, CanvasGroup.alpha, 0, fadeOutDuration,
                (controller, value) => controller.CanvasGroup.alpha = value);
            sequence.Chain(tween);
            return sequence;
        }

        public override bool ConsumeBackPressed()
        {
            return false;
        }
    }
}