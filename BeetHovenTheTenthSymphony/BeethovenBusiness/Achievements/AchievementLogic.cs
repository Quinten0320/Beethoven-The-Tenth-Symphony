using BeethovenBusiness.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness.Achievements
{
    public class AchievementLogic
    {
        private readonly IData _data;
        public AchievementLogic(IData data) { 
            _data = data;
        }
        public List<Achievement> GetAchievements()
        {
            return _data.GetAchievements();
        }
        public void UpdateAchievementStatus(Achievement achievement)
        {
            _data.UpdateAchievementStatus(achievement);
        }
    }
}
