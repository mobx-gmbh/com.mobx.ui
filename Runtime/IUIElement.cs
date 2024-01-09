using Cysharp.Threading.Tasks;

namespace MobX.UI
{
    public interface IUIElement : IBackPressedConsumer
    {
        public void Open();

        public void OpenImmediate();

        public UniTask OpenAsync();

        public void Close();

        public void CloseImmediate();

        public UniTask CloseAsync();
    }
}