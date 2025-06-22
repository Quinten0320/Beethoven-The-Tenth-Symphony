
using BeethovenBusiness.NewFolder;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
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
    public partial class GameStats : Window
    {
        private readonly GameStatsService _gameStatsService;
        public PlotModel MyModel { get; private set; }
        public GameStats(GameStatsService gameStatsService)
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            _gameStatsService = gameStatsService;
            loadModel();

            this.DataContext = _gameStatsService;
        }

        private void loadModel()
        {
            MyModel = new PlotModel { Title = "Aantal sessies per dag" };
            var series = new LineSeries();
            series.Points.Add(new DataPoint(0, 4));
            series.Points.Add(new DataPoint(1, 7));
            series.Points.Add(new DataPoint(2, 5));
            MyModel.Series.Add(series);

            MyModel.InvalidatePlot(true);
        }
    }
}
