﻿using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;

namespace Qryptic
{
    public partial class MainWindow : Window
    {
        private readonly Random random = new();
        private readonly DispatcherTimer blinkTimer;
        private readonly Storyboard? blinksb;

        private FilterInfoCollection? videoDevices;
        private VideoCaptureDevice? videoSource;
        private BarcodeReader? qrReader;

        private ViewModel viewModel = ViewModel.Instance;

        public MainWindow()
        {
            InitializeComponent();
            blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(random.Next(2, 6)) };
            blinkTimer.Tick += Blink;
            blinkTimer.Start();

            blinksb = FindResource("blinksb") as Storyboard;

            qrReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    //PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
                }
            };

            //Sample();

            StartCamera();

            this.DataContext = viewModel;
            viewModel.AnimationRequested += ViewModel_AnimationRequested;
        }

        Thickness lower = new(10, 10, 10, -75);
        Thickness upper = new(10);

        private void ViewModel_AnimationRequested(object? sender, ToastEventArgs e)
        {
            toast_title.Content = e.Title;
            toast_msg.Content = e.Message;
            if(e.Type == ToastType.Success)
            {
                toast_icon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "check_boldDrawingImage");
            } else {
                toast_icon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "x_boldDrawingImage");
            }

            Storyboard storyboard = new();

            ThicknessAnimation forwardAnimation = new()
            {
                From = lower,
                To = upper,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                BeginTime = TimeSpan.FromSeconds(0)
            };

            ThicknessAnimation reverseAnimation = new()
            {
                From = upper,
                To = lower,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                BeginTime = TimeSpan.FromSeconds(2)
            };

            storyboard.Children.Add(forwardAnimation);
            storyboard.Children.Add(reverseAnimation);
            Storyboard.SetTarget(forwardAnimation, toast);
            Storyboard.SetTarget(reverseAnimation, toast);
            Storyboard.SetTargetProperty(forwardAnimation, new PropertyPath(MarginProperty));
            Storyboard.SetTargetProperty(reverseAnimation, new PropertyPath(MarginProperty));

            storyboard.Begin();
        }

        private void StartUpDownAnimation()
        {
            double maxHeight = ScannerGrid.ActualHeight - Scanner.Height;

            ThicknessAnimation upDownAnimation = new()
            {
                From = new Thickness(0, 0, 0, 0),
                To = new Thickness(0, maxHeight, 0, 0),
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            Scanner.BeginAnimation(MarginProperty, upDownAnimation);
        }

        private void StartCamera()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("No webcam found!");
                return;
            }

            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if(eventArgs.Frame != null)
            {
                try
                {
                    Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
                    if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
                    {
                        return;
                    }
                    WebcamFeed.Dispatcher.Invoke(() =>
                    {
                        WebcamFeed.ImageSource = BitmapToImageSource(bitmap);
                    });
                    qrReader ??= new BarcodeReader
                    {
                        AutoRotate = true,
                        Options = new ZXing.Common.DecodingOptions
                        {
                            TryHarder = true,
                            //PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
                        }
                    };
                    var result = qrReader.Decode(bitmap);
                    if (result != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(result.Text);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error decoding QR code: " + ex.Message);
                    Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                }
            }
        }

        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using MemoryStream memory = new();
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            MoveEyes(e.GetPosition(DrawingCanvas));
        }

        private void MoveEyes(System.Windows.Point mousePos)
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

            UpdateEyePositions(dx, dy);
        }

        private void UpdateEyePositions(double dx, double dy)
        {
            Canvas.SetTop(LeftEye, 132 + dy);
            Canvas.SetLeft(LeftEye, 164 + dx);

            Canvas.SetTop(RightEye, 132 + dy);
            Canvas.SetLeft(RightEye, 222 + dx);
        }

        private void Blink(object? sender, EventArgs e)
        {
            blinksb!.Completed += StoryBoard_Completed;
            blinksb!.Begin();
        }

        private void StoryBoard_Completed(object? sender, EventArgs e)
        {
            blinksb!.Stop();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
           if(MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
           {
                Application.Current.Shutdown();
           }
        }

        private void Nav_Scan_Click(object sender, RoutedEventArgs e)
        {
            Navigate(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }

        private void Nav_Create_Click(object sender, RoutedEventArgs e)
        {
            Navigate(1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartUpDownAnimation();
        }

        void Navigate(int index)
        {
            if (Navigator.TabIndex != index)
            {
                DoubleAnimation height2Animation = new()
                {
                    From = IndRow.ActualHeight,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                viewModel.NavigateTo(index);
                height2Animation.Completed += (s, e) =>
                {
                    if (index == 3)
                    {
                        index++;
                        Indicator.CornerRadius = new CornerRadius(0, 5, 5, 35);
                    } else {
                        Indicator.CornerRadius = new CornerRadius(0, 5, 5, 0);
                    }
                    Grid.SetRow(Indicator, index);

                    DoubleAnimation heightAnimation = new()
                    {
                        From = 0,
                        To = IndRow.ActualHeight,
                        Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    Indicator.BeginAnimation(Border.HeightProperty, heightAnimation);
                };

                Indicator.BeginAnimation(Border.HeightProperty, height2Animation);
            }
        }

        private void Nav_Cache_Click(object sender, RoutedEventArgs e)
        {
            Navigate(2);
        }

        private void Nav_Config_Click(object sender, RoutedEventArgs e)
        {
            Navigate(3);
        }

        private void TextBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(4);
        }

        private void WebBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(5);
        }

        private void ContactBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(6);
        }

        private void WifiBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(7);
        }

        private void EmailBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SMSBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LocBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EventBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}