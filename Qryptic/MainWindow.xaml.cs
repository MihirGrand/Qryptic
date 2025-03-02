using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Qryptic
{
    public partial class MainWindow : Window
    {
        private readonly Random random = new Random();
        private readonly DispatcherTimer blinkTimer;
        private const double NormalEyeHeight = 13;
        Storyboard? blinksb;

        public MainWindow()
        {
            InitializeComponent();
            blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(random.Next(2, 6)) };
            blinkTimer.Tick += Blink;
            blinkTimer.Start();

            blinksb = FindResource("blinksb") as Storyboard;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            MoveEyes(e.GetPosition(DrawingCanvas));
        }

        private void MoveEyes(Point mousePos)
        {
            const double headCenterX = 200;
            const double headCenterY = 155;
            const double maxEyeOffset = 3;

            double dx = mousePos.X - headCenterX;
            double dy = mousePos.Y - headCenterY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > maxEyeOffset)
            {
                dx = (dx / distance) * maxEyeOffset;
                dy = (dy / distance) * maxEyeOffset;
            }

            UpdateEyePositions(dx, dy, NormalEyeHeight);
        }

        private void UpdateEyePositions(double dx, double dy, double eyeHeight)
        {
            Canvas.SetTop(LeftEye, 132 + dy);
            Canvas.SetLeft(LeftEye, 164 + dx);

            Canvas.SetTop(RightEye, 132 + dy);
            Canvas.SetLeft(RightEye, 222 + dx);
        }

        private void Blink(object? sender, EventArgs e)
        {            
            blinksb!.Completed += storyBoard_Completed;
            blinksb!.Begin();
        }

        private void storyBoard_Completed(object? sender, EventArgs e)
        {
            blinksb!.Stop();
        }
    }
}