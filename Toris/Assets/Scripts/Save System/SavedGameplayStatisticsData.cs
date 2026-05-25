namespace OutlandHaven.SaveSystem
{
    [System.Serializable]
    public class SavedGameplayStatisticsData
    {
        public int TotalKills;
        public int WolfKills;
        public float PlayTime;
        public int TotalPickUps;
        public System.Collections.Generic.Dictionary<string, int> ItemPickUps = new System.Collections.Generic.Dictionary<string, int>();
    }
}
