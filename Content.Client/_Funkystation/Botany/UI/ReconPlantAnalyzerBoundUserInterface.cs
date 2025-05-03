using Content.Shared._Funkystation.Botany.PlantAnalyzer;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Funkystation.Botany.UI
{
    [UsedImplicitly]
    public sealed class ReconPlantAnalyzerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ReconPlantAnalyzerWindow? _window;

        public ReconPlantAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<ReconPlantAnalyzerWindow>();
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        }


        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;

            if (message is not PlantAnalyzerUserMessage cast)
                return;

            _window.Populate(cast);
        }

    }
}
