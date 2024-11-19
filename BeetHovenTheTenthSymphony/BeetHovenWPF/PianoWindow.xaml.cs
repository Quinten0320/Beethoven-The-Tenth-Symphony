using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {
        private Rectangle _movingRectangle;
        public PianoWindow()
        {
            InitializeComponent();

            SizeChanged += PianoWindow_SizeChanged; //adjust key size on window resize
            Loaded += PianoWindow_Loaded;
            

        }

        private int _octaves = 6;
        private const int _whiteKeyCount = 7; //niet aanpassen

        private void GeneratePiano()
        {
            PianoCanvas.Children
        .OfType<Rectangle>()
        .Where(r => r.Tag?.ToString() == "White" || r.Tag?.ToString() == "Black")
        .ToList()
        .ForEach(r => PianoCanvas.Children.Remove(r));

            double canvasWidth = PianoCanvas.ActualWidth;
            double canvasHeight = PianoCanvas.ActualHeight;

            double whiteKeyWidth = canvasWidth / (_octaves * _whiteKeyCount);
            double whiteKeyHeight = canvasHeight/ 3.2;
            double blackKeyWidth = whiteKeyWidth * 0.6;
            double blackKeyHeight = whiteKeyHeight * 0.6;

            string[] whiteKeysWithBlack = { "C", "D", "F", "G", "A" };
            double currentX = 0;

            for (int octave = 0; octave < _octaves; octave++)
            {
                for (int i = 0; i < _whiteKeyCount; i++)
                {
                    //current white kay
                    Rectangle whiteKey = new Rectangle
                    {
                        Width = whiteKeyWidth,
                        Height = whiteKeyHeight,
                        Fill = Brushes.White,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Tag = "White"
                    };

                    Canvas.SetLeft(whiteKey, currentX);
                    Canvas.SetBottom(whiteKey, 0);
                    whiteKey.MouseDown += Key_MouseDown;
                    PianoCanvas.Children.Add(whiteKey);

                    //add black keys
                    if (i < _whiteKeyCount - 1 && whiteKeysWithBlack.Contains(GetWhiteKeyName(i)))
                    {
                        Rectangle blackKey = new Rectangle
                        {
                            Width = blackKeyWidth,
                            Height = blackKeyHeight,
                            Fill = Brushes.Black,
                            Tag = "Black"
                        };
                        
                        Canvas.SetLeft(blackKey, currentX + whiteKeyWidth * 0.75 - (blackKeyWidth / 2));
                        Canvas.SetBottom(blackKey, whiteKeyHeight - blackKeyHeight);
                        blackKey.MouseDown += Key_MouseDown;
                        PianoCanvas.Children.Add(blackKey);
                    }

                    currentX += whiteKeyWidth;
                }
            }
        }
        private void StartAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_movingRectangle == null)
            {
                MessageBox.Show("Het bewegende rechthoekje is nog niet aangemaakt.");
                return;
            }

            // Start de animatie
            AnimateRectangle();
        }
        
        double rectangleHeight = 50;
        private void CreateMovingRectangle()
        {
            double rectangleWidth = PianoCanvas.ActualWidth / (_octaves * _whiteKeyCount); // You can adjust this size

            _movingRectangle = new Rectangle
            {
                Width = rectangleWidth,
                Height = rectangleHeight,
                Fill = Brushes.Black,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            // Initial position of the rectangle
            Canvas.SetLeft(_movingRectangle, 10);
            Canvas.SetTop(_movingRectangle, 10);

            
            PianoCanvas.Children.Add(_movingRectangle);
        }
        private void AnimateRectangle()
        {
            // Create a simple animation for MovingRectangle
            DoubleAnimation moveAnimation = new DoubleAnimation
            {
                From = 10,   // Starting Y position
                To = PianoCanvas.ActualHeight - (PianoCanvas.ActualHeight / 3.2) - rectangleHeight,    // Ending Y position
                Duration = new Duration(TimeSpan.FromSeconds(5)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            // Apply the animation to the rectangle's Canvas.Top property
            _movingRectangle.BeginAnimation(Canvas.TopProperty, moveAnimation);
        }

        private void PianoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GeneratePiano(); //re-generate piano keys on resize
        }
        private void PianoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePiano();
            CreateMovingRectangle();
        }

        private string GetWhiteKeyName(int index)
        {
            string[] whiteKeyNames = { "C", "D", "E", "F", "G", "A", "B" };
            return whiteKeyNames[index];
        }

        private void Key_MouseDown(object sender, MouseButtonEventArgs e) //wanneer een piano noot ingedrukt wordt
        {
            
        }
    }
}