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
        public PianoWindow()
        {
            InitializeComponent();

            SizeChanged += PianoWindow_SizeChanged; //adjust key size on window resize

            GeneratePiano();
        }

        private int _octaves = 6;
        private const int _whiteKeyCount = 7; //niet aanpassen

        private void GeneratePiano()
        {
            PianoCanvas.Children.Clear();

            double canvasWidth = PianoCanvas.ActualWidth;
            double canvasHeight = PianoCanvas.ActualHeight;

            double whiteKeyWidth = canvasWidth / (_octaves * _whiteKeyCount);
            double whiteKeyHeight = canvasHeight;
            double blackKeyWidth = whiteKeyWidth * 0.6;
            double blackKeyHeight = canvasHeight * 0.6;

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
                    Canvas.SetTop(whiteKey, 0);
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
                        Canvas.SetTop(blackKey, 0);
                        blackKey.MouseDown += Key_MouseDown;
                        PianoCanvas.Children.Add(blackKey);
                    }

                    currentX += whiteKeyWidth;
                }
            }
        }

        private void PianoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GeneratePiano(); //re-generate piano keys on resize
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