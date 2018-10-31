namespace NHLAPIScrape
{
    public enum GameStatuses { NotStarted = 1, Preview = 2, InAction = 3, CriticalAction = 4, Intermission, Final = 7 }

    public struct GameInfo
    {
        public bool WereHome;
        public int HomeScore;
        public int HomeSOG;
        public int AwayScore;
        public int AwaySOG;
        public double TimeRemaining;
        public int Period;
    }
}
