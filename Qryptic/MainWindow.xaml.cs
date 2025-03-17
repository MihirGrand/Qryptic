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
using ZXing.Common;
using ZXing;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;
using System.Windows.Documents;
using System.Xml;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using System.Web;
using System.Globalization;
using static QRCoder.PayloadGenerator;

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

        string decodedStr = "";

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

            //StartCamera();

            this.DataContext = viewModel;
            viewModel.AnimationRequested += ViewModel_AnimationRequested;
            viewModel.ModeChanged += ViewModel_ModeChanged;
        }

        private void ViewModel_ModeChanged(object? sender, EventArgs e)
        {
            if(viewModel.LiveMode == true)
            {
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    //videoSource.Stop();
                    videoSource.WaitForStop();
                }
                WebcamFeed.ImageSource = null;
                UploadBtn.Visibility = Visibility.Visible;
            }
            else
            {
                StartCamera();
                UploadBtn.Visibility = Visibility.Hidden;
            }
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
            double maxHeight = ScannerGrid.ActualHeight - Scanner.Height - 40;

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
            // Check if the frame is null
            if (eventArgs.Frame == null)
            {
                Debug.WriteLine("eventArgs.Frame is null.");
                return;
            }

            try
            {
                // Clone the frame to avoid access violations
                using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    // Check if the bitmap is valid
                    if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
                    {
                        Debug.WriteLine("Invalid bitmap.");
                        return;
                    }

                    // Update the UI with the new frame
                    WebcamFeed.Dispatcher.Invoke(() =>
                    {
                        WebcamFeed.ImageSource = BitmapToImageSource(bitmap);
                    });

                    // Initialize the BarcodeReader if it is null
                    qrReader ??= new BarcodeReader
                    {
                        //AutoRotate = true,
                        Options = new ZXing.Common.DecodingOptions
                        {
                            //TryHarder = true,
                        }
                    };

                    try
                    {
                        var result = qrReader.Decode(bitmap);
                        if (result != null)
                        {
                            if(result.BarcodeFormat == BarcodeFormat.QR_CODE)
                            {
                                if(result.Text.StartsWith("http:"))
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "globe_simpleDrawingImage");
                                        decoded.Inlines.Clear();
                                        decoded.Inlines.Add(new Bold(new Run("URL: ")));
                                        decoded.Inlines.Add(new Run(result.Text));
                                    });
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                    catch (Exception decodeEx)
                    {
                        Debug.WriteLine("Error decoding barcode/QR code: " + decodeEx.Message);
                        Debug.WriteLine("Stack Trace: " + decodeEx.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                Debug.WriteLine("Stack Trace: " + ex.StackTrace);
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
                System.Windows.Application.Current.Shutdown();
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
            if (Navigator.SelectedIndex != index)
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
            viewModel.NavigateTo(8);
        }

        private void SMSBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(9);
        }

        private void LocBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(10);
        }

        private void EventBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(11);
        }

        private void UPIBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(12);
        }

        private void BarcodeBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NavigateTo(13);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
           //UploadBtn.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
           //UploadBtn.Visibility = Visibility.Hidden;
        }

        private void UploadBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select an Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                WebcamFeed.ImageSource = new BitmapImage(new Uri(openFileDialog.FileName));
                qrReader ??= new BarcodeReader
                {
                    AutoRotate = true,
                    Options = new ZXing.Common.DecodingOptions
                    {
                        TryHarder = true,
                    }
                };
                var result = qrReader.Decode((Bitmap)Bitmap.FromFile(openFileDialog.FileName));
                if (result != null)
                {
                    decodedStr = result.Text;
                    if (result.BarcodeFormat == BarcodeFormat.QR_CODE)
                    {
                        if (result.Text.StartsWith("http:"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "globe_simpleDrawingImage");
                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("URL: ")));
                                decoded.Inlines.Add(new Run(result.Text));
                            });
                        }
                        else if (result.Text.StartsWith("BEGIN:VCARD"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "user_circleDrawingImage");
                                string name = ExtractValue(result.Text, @"FN:(.+)");
                                string phone = ExtractValue(result.Text, @"TEL[^:]*:(\d+)");
                                string email = ExtractValue(result.Text, @"EMAIL:(.+)");
                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("Name: ")));
                                decoded.Inlines.Add(new Run(name));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Phone: ")));
                                decoded.Inlines.Add(new Run(phone));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Email: ")));
                                decoded.Inlines.Add(new Run(email));
                            });
                        }
                        else if (result.Text.StartsWith("WIFI:"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "wifi_highDrawingImage");
                                var split = result.Text.Split(';');
                                string ssid = split[1].Replace("S:", "");
                                string pw = split[2].Replace("P:", "");
                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("SSID: ")));
                                decoded.Inlines.Add(new Run(ssid));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("PW: ")));
                                decoded.Inlines.Add(new Run(pw));
                            });
                        }
                        else if (result.Text.StartsWith("mailto:"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "envelopeDrawingImage");
                                Uri uri = new Uri(result.Text);
                                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                                string subject = queryParams["subject"] ?? "";
                                string body = queryParams["body"] ?? "";
                                subject = HttpUtility.UrlDecode(subject);
                                body = HttpUtility.UrlDecode(body);

                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("To: ")));
                                decoded.Inlines.Add(new Run(result.Text.Split('?')[0].Replace("mailto:", "")));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Subject: ")));
                                decoded.Inlines.Add(new Run(subject));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Body: ")));
                                decoded.Inlines.Add(new Run(body));
                            });
                        }
                        else if (result.Text.StartsWith("sms:"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "chat_dotsDrawingImage");
                                Uri uri = new Uri(result.Text);
                                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                                string body = queryParams["body"] ?? "";
                                body = HttpUtility.UrlDecode(body);

                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("To: ")));
                                decoded.Inlines.Add(new Run(result.Text.Split('?')[0].Replace("sms:", "")));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Message: ")));
                                decoded.Inlines.Add(new Run(body));
                            });
                        }
                        else if (result.Text.StartsWith("geo:"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "map_pinDrawingImage");
                                var split = result.Text.Replace("geo:", "").Split(',');
                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("Lat: ")));
                                decoded.Inlines.Add(new Run(split[0]));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Long: ")));
                                decoded.Inlines.Add(new Run(split[1]));
                            });
                        }
                        else if (result.Text.StartsWith("BEGIN:VEVENT"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "calendar_dotsDrawingImage");

                                string title = ExtractValue(result.Text, @"SUMMARY:(.+)");
                                string description = ExtractValue(result.Text, @"DESCRIPTION:(.+)");
                                string location = ExtractValue(result.Text, @"LOCATION:(.+)");
                                string startDateRaw = ExtractValue(result.Text, @"DTSTART:(\d+)");
                                string endDateRaw = ExtractValue(result.Text, @"DTEND:(\d+)");

                                // Convert date format YYYYMMDD → DD.MM.YYYY
                                string startDate = FormatDate(startDateRaw);
                                string endDate = FormatDate(endDateRaw);

                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("Title: ")));
                                decoded.Inlines.Add(new Run(title));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Description: ")));
                                decoded.Inlines.Add(new Run(description));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Location: ")));
                                decoded.Inlines.Add(new Run(location));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Start: ")));
                                decoded.Inlines.Add(new Run(startDate));
                                decoded.Inlines.Add(new Bold(new Run("   End: ")));
                                decoded.Inlines.Add(new Run(endDate));
                            });
                        }
                        else if (result.Text.StartsWith("upi:"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "currency_inrDrawingImage");

                                Uri uri = new Uri(result.Text);
                                var queryParams = HttpUtility.ParseQueryString(uri.Query);

                                string upiId = queryParams["pa"] ?? "Not Found";
                                string payeeName = queryParams["pn"] ?? "Not Found";
                                string amount = queryParams["am"] ?? "0";
                                string currency = queryParams["cu"] ?? "Not Found";
                                string note = queryParams["tn"] ?? "";
                                string amountWithCurrency = $"{amount} {currency}";

                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("UPI ID: ")));
                                decoded.Inlines.Add(new Run(upiId));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Payee: ")));
                                decoded.Inlines.Add(new Run(payeeName));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Amount: ")));
                                decoded.Inlines.Add(new Run(amountWithCurrency));
                                decoded.Inlines.Add(new LineBreak());
                                decoded.Inlines.Add(new Bold(new Run("Note: ")));
                                decoded.Inlines.Add(new Run(note));
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "article_ny_timesDrawingImage");
                                decoded.Inlines.Clear();
                                decoded.Inlines.Add(new Bold(new Run("Text: ")));
                                decoded.Inlines.Add(new Run(result.Text));
                                Debug.WriteLine(result.Text);
                            });
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            qrType.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "barcodeDrawingImage");
                            decoded.Inlines.Clear();
                            decoded.Inlines.Add(new Bold(new Run("Data: ")));
                            decoded.Inlines.Add(new Run(result.Text));
                        });
                    }
                } else
                {
                    decodedStr = "";
                }
            }
        }

        static string ExtractValue(string text, string pattern)
        {
            Match match = Regex.Match(text, pattern, RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim() : "Not found";
        }

        static string FormatDate(string rawDate)
        {
            if (DateTime.TryParseExact(rawDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return date.ToString("dd.MM.yyyy");
            }
            return "Invalid Date";
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (decodedStr != "")
            {
                if (decodedStr.StartsWith("http:"))
                {
                    Clipboard.SetText(decodedStr);
                    viewModel.ShowToast("Copied", "URL copied to clipboard", ToastType.Success);
                }
                else if (decodedStr.StartsWith("BEGIN:VCARD"))
                {
                    string name = ExtractValue(decodedStr, @"FN:(.+)");
                    string phone = ExtractValue(decodedStr, @"TEL[^:]*:(\d+)");
                    string email = ExtractValue(decodedStr, @"EMAIL:(.+)");
                    Clipboard.SetText($"Name: {name}\nPhone: {phone}\nEmail: {email}");
                    viewModel.ShowToast("Copied", "Contact copied to clipboard", ToastType.Success);
                }
                else if (decodedStr.StartsWith("WIFI:"))
                {
                    var split = decodedStr.Split(';');
                    string ssid = split[1].Replace("S:", "");
                    string pw = split[2].Replace("P:", "");
                    Clipboard.SetText($"SSID: {ssid}\nPassword: {pw}");
                    viewModel.ShowToast("Copied", "Creds copied to clipboard", ToastType.Success);
                }
                else if (decodedStr.StartsWith("mailto:"))
                {
                    Uri uri = new Uri(decodedStr);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);
                    string subject = queryParams["subject"] ?? "";
                    string body = queryParams["body"] ?? "";
                    subject = HttpUtility.UrlDecode(subject);
                    body = HttpUtility.UrlDecode(body);

                    Clipboard.SetText($"To: {decodedStr.Split('?')[0].Replace("mailto:", "")}\nSubject: {subject}\nBody: {body}");
                    viewModel.ShowToast("Copied", "Mail copied to clipboard", ToastType.Success);
                }
                else if (decodedStr.StartsWith("sms:"))
                {
                    Uri uri = new Uri(decodedStr);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);
                    string body = queryParams["body"] ?? "";
                    body = HttpUtility.UrlDecode(body);

                    Clipboard.SetText($"To: {decodedStr.Split('?')[0].Replace("sms:", "")}\nMessage: {body}");
                    viewModel.ShowToast("Copied", "SMS copied to clipboard", ToastType.Success);
                }
                else if (decodedStr.StartsWith("geo:"))
                {
                    var split = decodedStr.Replace("geo:", "").Split(',');

                    Clipboard.SetText($"Lat: {split[0]}, Long: {split[1]}");
                    viewModel.ShowToast("Copied", "Coords copied to clipboard", ToastType.Success);
                }
                else if (decodedStr.StartsWith("BEGIN:VEVENT"))
                {
                    string title = ExtractValue(decodedStr, @"SUMMARY:(.+)");
                    string description = ExtractValue(decodedStr, @"DESCRIPTION:(.+)");
                    string location = ExtractValue(decodedStr, @"LOCATION:(.+)");
                    string startDateRaw = ExtractValue(decodedStr, @"DTSTART:(\d+)");
                    string endDateRaw = ExtractValue(decodedStr, @"DTEND:(\d+)");

                    string startDate = FormatDate(startDateRaw);
                    string endDate = FormatDate(endDateRaw);

                    Clipboard.SetText($"Event Title: {title}\nDescription: {description}\nLocation: {location}\nStart: {startDate} | End: {endDate}");
                    viewModel.ShowToast("Copied", "Details copied to clipboard", ToastType.Success);
                }
                else if (decodedStr.StartsWith("upi:"))
                {
                    Uri uri = new Uri(decodedStr);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);

                    string upiId = queryParams["pa"] ?? "Not Found";
                    string payeeName = queryParams["pn"] ?? "Not Found";
                    string amount = queryParams["am"] ?? "0";
                    string currency = queryParams["cu"] ?? "Not Found";
                    string note = queryParams["tn"] ?? "";
                    string amountWithCurrency = $"{amount} {currency}";

                    Clipboard.SetText($"UPI ID: {upiId}");
                    viewModel.ShowToast("Copied", "UPI ID copied to clipboard", ToastType.Success);
                }
                else
                {
                    Clipboard.SetText(decodedStr);
                    viewModel.ShowToast("Copied", "Text copied to clipboard", ToastType.Success);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (decodedStr != "")
            {
                if (decodedStr.StartsWith("http:"))
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Text File|*.txt",
                        Title = "Save URL",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, decodedStr);
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "URL saved!", ToastType.Success);
                }
                else if (decodedStr.StartsWith("BEGIN:VCARD"))
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Contact|*.vcard",
                        Title = "Save Contact",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, decodedStr);
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "Contact saved!", ToastType.Success);
                }
                else if (decodedStr.StartsWith("WIFI:"))
                {
                    var split = decodedStr.Split(';');
                    string ssid = split[1].Replace("S:", "");
                    string pw = split[2].Replace("P:", "");
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Text File|*.txt",
                        Title = "Save Wifi Credentials",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, $"SSID: {ssid}\nPassword: {pw}");
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "Wifi Creds saved!", ToastType.Success);
                }
                else if (decodedStr.StartsWith("mailto:"))
                {
                    Uri uri = new Uri(decodedStr);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);
                    string subject = queryParams["subject"] ?? "";
                    string body = queryParams["body"] ?? "";
                    subject = HttpUtility.UrlDecode(subject);
                    body = HttpUtility.UrlDecode(body);

                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Text File|*.txt",
                        Title = "Save Email",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, $"To: {decodedStr.Split('?')[0].Replace("mailto:", "")}\nSubject: {subject}\nBody: {body}");
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "Mail saved!", ToastType.Success);
                }
                else if (decodedStr.StartsWith("sms:"))
                {
                    Uri uri = new Uri(decodedStr);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);
                    string body = queryParams["body"] ?? "";
                    body = HttpUtility.UrlDecode(body);

                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Text File|*.txt",
                        Title = "Save SMS",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, $"To: {decodedStr.Split('?')[0].Replace("sms:", "")}\nMessage: {body}");
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "SMS saved!", ToastType.Success);
                }
                else if (decodedStr.StartsWith("geo:"))
                {
                    var split = decodedStr.Replace("geo:", "").Split(',');

                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Text File|*.txt",
                        Title = "Save Location",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, $"Lat: {split[0]}, Long: {split[1]}");
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "Co-ordinates saved!", ToastType.Success);
                }
                else if (decodedStr.StartsWith("BEGIN:VEVENT"))
                {
                    string title = ExtractValue(decodedStr, @"SUMMARY:(.+)");
                    string description = ExtractValue(decodedStr, @"DESCRIPTION:(.+)");
                    string location = ExtractValue(decodedStr, @"LOCATION:(.+)");
                    string startDateRaw = ExtractValue(decodedStr, @"DTSTART:(\d+)");
                    string endDateRaw = ExtractValue(decodedStr, @"DTEND:(\d+)");

                    string startDate = FormatDate(startDateRaw);
                    string endDate = FormatDate(endDateRaw);

                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Event File|*.ics",
                        Title = "Save Event",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, decodedStr);
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "Event saved!", ToastType.Success);
                }
                else if (decodedStr.StartsWith("upi:"))
                {
                    Uri uri = new Uri(decodedStr);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);

                    string upiId = queryParams["pa"] ?? "Not Found";
                    string payeeName = queryParams["pn"] ?? "Not Found";
                    string amount = queryParams["am"] ?? "0";
                    string currency = queryParams["cu"] ?? "Not Found";
                    string note = queryParams["tn"] ?? "";
                    string amountWithCurrency = $"{amount} {currency}";

                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Text File|*.txt",
                        Title = "Save UPI Details",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, $"UPI ID: {upiId}");
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "UPI Details saved!", ToastType.Success);
                }
                else
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Text File|*.txt",
                        Title = "Save URL",
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, decodedStr);
                        }
                        catch (Exception ex)
                        {
                            viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                        }
                    }
                    viewModel.ShowToast("Saved", "URL saved!", ToastType.Success);
                }
            }
        }
    }
}