using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media.Animation;
using BeethovenBusiness;

namespace BeetHovenWPF
{
    public partial class PianoWindow : Window
    {
        private readonly PianoInputHandler _inputHandler;
        private Rectangle _movingRectangle;
        public PianoWindow()
        {
            InitializeComponent();

            SizeChanged += PianoWindow_SizeChanged;

            _inputHandler = new PianoInputHandler();
            _inputHandler.NotePressed += OnMidiNotePressed;

            Loaded += PianoWindow_Loaded;
        }

        private void OnMidiNotePressed(string note)
        {
            // Update the TextBox with the received note
            Dispatcher.Invoke(() => LastPressedNoteTextBox.Text = note);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Dispose of the PianoInputHandler when the window closes
            _inputHandler.Dispose();
        }

        private int _octaves = 8;
        private const int _whiteKeyCount = 7; //altijd 7

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

            string[] whiteKeysWithBlack = { "C", "D", "F", "G", "A" }; //alle witte keys waar een zwarte key op moet zitten
            double currentX = 0;

            for (int octave = 0; octave < _octaves; octave++)
            {
                for (int i = 0; i < _whiteKeyCount; i++)
                {
                    string whiteNote = GetWhiteKeyName(i) + (octave+1); //Key naam + Octaaf

                    Rectangle whiteKey = new Rectangle
                    {
                        Width = whiteKeyWidth,
                        Height = whiteKeyHeight,
                        Fill = Brushes.White,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,

                        Tag = whiteNote //geeft witte key een tag
                    };

                    Canvas.SetLeft(whiteKey, currentX);
                    Canvas.SetBottom(whiteKey, 0);
                    Panel.SetZIndex(whiteKey, 0);
                    whiteKey.MouseDown += Key_MouseDown;
                    PianoCanvas.Children.Add(whiteKey);

                    if (i < _whiteKeyCount - 1 && whiteKeysWithBlack.Contains(GetWhiteKeyName(i)))
                    {
                        string blackNote = GetWhiteKeyName(i) + "#" + (octave+1);

                        Rectangle blackKey = new Rectangle
                        {
                            Width = blackKeyWidth,
                            Height = blackKeyHeight,
                            Fill = Brushes.Black,

                            Tag = blackNote //geeft zwarte key tag
                        };

                        Canvas.SetLeft(blackKey, currentX + whiteKeyWidth * 0.75 - (blackKeyWidth / 2) + whiteKeyWidth * 0.25);
                        Canvas.SetBottom(blackKey, whiteKeyHeight - blackKeyHeight);
                        Panel.SetZIndex(blackKey, 1);
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
            GeneratePiano();
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

        private void Key_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle key)
            {
                string note = key.Tag?.ToString();
                LastPressedNoteTextBox.Text = note;
            }
        }
    }
}