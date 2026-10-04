using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Apps.ViewModel;
using LcdMod.Client.Helpers;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Modules.Defense;
using LcdMod.Client.Modules.Energy;
using LcdMod.Client.Modules.Survivors;

namespace LcdMod.Client
{
    public partial class LcdModClientComponent
    {
        public DefenseDataModule DefenseData { get; private set; }
        public EnergyDataModule EnergyData { get; private set; }

        partial void LoadModData()
        {
            DefenseData = new DefenseDataModule();
            DefenseData.Load();
            EnergyData = new EnergyDataModule();
            LineChartPanel.SelfCheck();
        }

        partial void UpdateModData(long frame)
        {
            if (DefenseData != null)
                DefenseData.Update(frame);
            if (EnergyData != null)
                EnergyData.Update(frame);
        }

        partial void UnloadModData()
        {
            if (DefenseData != null)
                DefenseData.Unload();
            DefenseData = null;
            if (EnergyData != null)
                EnergyData.Unload();
            EnergyData = null;
            SurvivorsClient.Clear();
        }

        partial void ClearModCaches()
        {
            ItemsApp.SpriteCache.Clear();
            ProjectorViewModel.ClearBlueprintCache();
            AssemblerBlueprintCatalog.Clear();
        }
    }
}
