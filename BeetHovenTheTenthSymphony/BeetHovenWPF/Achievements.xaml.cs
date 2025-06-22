using BeethovenBusiness.Achievements;
using BeethovenBusiness.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BeetHovenWPF
{
    /// <summary>
    /// Interaction logic for Achievements.xaml
    /// </summary>
    public partial class Achievements : Window
    {
        private AchievementLogic _achievementLogic;

        public Achievements(IData data)
        {
            InitializeComponent();
            _achievementLogic = new AchievementLogic(data);
            LoadAchievements();
        }

        private void LoadAchievements()
        {
            List<Achievement> achievements = _achievementLogic.GetAchievements();
            AchievementsList.ItemsSource = achievements;
        }
    }
}
