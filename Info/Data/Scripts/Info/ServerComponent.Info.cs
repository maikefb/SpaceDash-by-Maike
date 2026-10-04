namespace LcdMod.Server
{
    public partial class LcdModServerComponent
    {
        public SurvivorsServerTracker Survivors { get; private set; }

        partial void LoadModData()
        {
            Survivors = new SurvivorsServerTracker(this);
            Survivors.Load();
        }

        partial void BeforeStartModData()
        {
            if (Survivors != null)
                Survivors.BeforeStart();
        }

        partial void SaveModData()
        {
            if (Survivors != null)
                Survivors.Save();
        }

        partial void UnloadModData()
        {
            if (Survivors != null)
                Survivors.Unload();
            Survivors = null;
        }
    }
}
