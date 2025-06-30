
using BeethovenBusiness.MidiFileLogica;
using BeethovenBusiness.NewFolder;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Windows;

namespace BeetHovenWPF
{
    public partial class GameStats : Window
    {
        private readonly GameStatsService _gameStatsService;
        public PlotModel MyModel { get; private set; }
        public PlotModel NoteBarModel { get; private set; }

        public GameStats(GameStatsService gameStatsService)
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            _gameStatsService = gameStatsService;
            loadModel();

            List<int> scoreList = _gameStatsService.GetScoreBoardData();
            ScoreListBox.ItemsSource = scoreList;


            this.DataContext = _gameStatsService;
        }

        public PlotModel CreateNoteBarModel(List<int> aantalNoten)
        {
            var model = new PlotModel { Title = "Aantal noten" };

            // Categorieën op de Y-as
            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "NotesAxis"
            };

            string[] labels = { "C", "D", "E", "F", "G", "A", "B" };
            foreach (var label in labels)
                categoryAxis.Labels.Add(label);

            // Waarden op de X-as
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Title = "Aantal"
            };

            var barSeries = new BarSeries
            {
                ItemsSource = new List<BarItem>(),
                LabelPlacement = LabelPlacement.Inside,
                LabelFormatString = "{0}"
            };

            foreach (var aantal in aantalNoten)
            {
                barSeries.Items.Add(new BarItem { Value = aantal });
            }

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);
            model.Series.Add(barSeries);

            return model;
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
