    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    namespace BeetHovenWPF
    {
        public partial class MainWindow : Window
        {
            public MainWindow()
            {
                InitializeComponent();
            }
            private void OpenPianoWindow(object sender, RoutedEventArgs e)
            {
                PianoWindow pianoWindow = new PianoWindow();
                pianoWindow.Show();
            }
        }
    }