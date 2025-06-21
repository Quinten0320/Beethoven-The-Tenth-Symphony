using BeethovenBusiness.Interfaces;
using BeethovenBusiness.Progress;
using System.Windows.Controls;

namespace BeetHovenWPF
{
    public partial class Progress : Page
    {
        private readonly ProgressService _progressService;
        private readonly IData _data;
        public Progress()
        {
            _data = DataFactory.CreateDefaultData();
            _progressService = new ProgressService(_data);
            InitializeComponent();
            LoadProgress();
        }

        private void LoadProgress()
        {
            int level = _progressService.GetCurrentLevel();
            int xp = _progressService.GetCurrentXP();

            LevelTextBlock.Text = $"Level: {level}";
            XPTextBlock.Text = $"Total XP: {xp}";

            int xpThisLevel = xp % 1000;
            XPProgressBar.Value = xpThisLevel;
            XPProgressLabel.Text = $"{xpThisLevel} / 1000 XP to next level";
        }
    }
}
