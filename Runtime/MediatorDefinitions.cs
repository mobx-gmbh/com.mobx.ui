using MobX.Mediator.Generation;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static MobX.UI.MediatorDefinitions;

[assembly: GenerateMediatorFor(typeof(PlayerInput),
    MediatorTypes = MediatorTypes.ValueAsset,
    NameSpace = NameSpace,
    Subfolder = Subfolder)]

[assembly: GenerateMediatorFor(typeof(EventSystem),
    MediatorTypes = MediatorTypes.ValueAsset,
    NameSpace = NameSpace,
    Subfolder = Subfolder)]

namespace MobX.UI
{
    public static class MediatorDefinitions
    {
        public const string NameSpace = "MobX.UI";
        public const string Subfolder = "Mediator";
    }
}