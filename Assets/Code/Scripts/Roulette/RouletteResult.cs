namespace Code.Scripts.Roulette
{
    public readonly struct RouletteResult
    {
        public int RoundId { get; }
        public int Week { get; }
        public int Day { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public int LuckyNumber { get; }
        public bool IsBoss { get; }
        public int BossProbability { get; }
        public int VictoryTolerance => 3 - Week;

        public RouletteResult(int roundId, int week, int day, int minimum, int maximum,
            int luckyNumber, bool isBoss, int bossProbability)
        {
            RoundId = roundId;
            Week = week;
            Day = day;
            Minimum = minimum;
            Maximum = maximum;
            LuckyNumber = luckyNumber;
            IsBoss = isBoss;
            BossProbability = bossProbability;
        }
    }
}
