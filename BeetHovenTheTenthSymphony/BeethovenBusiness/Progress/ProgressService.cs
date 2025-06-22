using BeethovenBusiness.Interfaces;

namespace BeethovenBusiness.Progress
{
    public class ProgressService
    {
        private readonly IData _data;

        public ProgressService(IData data)
        {
            _data = data;
        }

        public int GetCurrentLevel()
        {
            return _data.GetCurrentLevel();
        }

        public int GetCurrentXP()
        {
            return _data.GetCurrentXP();
        }
        public void AddXP(int amount)
        {
            _data.AddXP(amount);
        }

        public (int earnedXP, bool leveledUp) CalculateXP(string difficulty, int score)
        {
            int multiplier = difficulty switch
            {
                "Easy" => 1,
                "Medium" => 2,
                "Hard" => 3,
                _ => 1
            };

            int xp = (int)(score * 0.1 * multiplier);
            int oldLevel = GetCurrentLevel();
            if (xp > 0){AddXP(xp);}
            int newLevel = GetCurrentLevel();
            bool levelUp = newLevel > oldLevel;

            return (xp > 0 ? xp : 0, levelUp);
        }
    }
}
