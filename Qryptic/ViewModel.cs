using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Qryptic
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastEventArgs(string title, string message, ToastType type) : EventArgs
    {
        public string Title { get; } = title;
        public string Message { get; } = message;
        public ToastType Type { get; } = type;
    }

    public enum CodeType
    {
       Text, Website, Contact, Wifi, Email, SMS, Location, Event, UPI, Barcode, None
    }

    public class Record
    {
        public int Id { get; set; }
        public CodeType Type { get; set; }
        public required string Result { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class CodeTypeToResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CodeType type)
            {
                switch(type)
                {
                    case CodeType.Website:
                        return Application.Current.FindResource("globe_simpleDrawingImage");
                    case CodeType.Contact:
                        return Application.Current.FindResource("user_circleDrawingImage");
                    case CodeType.Wifi:
                        return Application.Current.FindResource("wifi_highDrawingImage");
                    case CodeType.Email:
                        return Application.Current.FindResource("envelopeDrawingImage");
                    case CodeType.SMS:
                        return Application.Current.FindResource("chat_dotsDrawingImage");
                    case CodeType.Location:
                        return Application.Current.FindResource("map_pinDrawingImage");
                    case CodeType.Event:
                        return Application.Current.FindResource("calendar_dotsDrawingImage");
                    case CodeType.UPI:
                        return Application.Current.FindResource("currency_inrDrawingImage");
                    case CodeType.Text:
                        return Application.Current.FindResource("article_ny_timesDrawingImage");
                    case CodeType.Barcode:
                        return Application.Current.FindResource("barcodeDrawingImage");
                    default:
                        return Application.Current.FindResource("barcodeDrawingImage");
                }
            }
            return Application.Current.FindResource("barcodeDrawingImage");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public partial class ViewModel: ObservableObject
    {
        [ObservableProperty]
        public int currentPage = 0;

        [ObservableProperty]
        public bool liveMode = true;

        public void ShowToast(String title, String message, ToastType type)
        {
            AnimationRequested?.Invoke(this, new ToastEventArgs(title, message, type));
        }

        [ObservableProperty]
        private ObservableCollection<Record> cache = new ObservableCollection<Record>();

        public void LoadItemsFromLiteDB()
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            using LiteDatabase db = new(Path.Combine(appFolder, "cache.db"));
            var col = db.GetCollection<Record>("cache");
            Cache = new ObservableCollection<Record>(col.FindAll().ToList());
            Cache.ToList().ForEach(x =>
            {
                Debug.WriteLine($"{x.Id} | {x.Type.ToString()} | {x.Result.ToString()}");
            });
        }

        public static void WriteToCache(CodeType type, string result)
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            using LiteDatabase db = new(Path.Combine(appFolder, "cache.db"));
            var col = db.GetCollection<Record>("cache");

            Record record = new()
            {
                Type = type,
                Result = result,
                DateTime = DateTime.Now
            };
            int insertedCount = col.Insert(record);
            Debug.WriteLine($"Inserted Count: {insertedCount}");
        }

        public event EventHandler<ToastEventArgs>? AnimationRequested;

        public event EventHandler? ModeChanged;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            Debug.WriteLine(e.PropertyName);

            switch (e.PropertyName)
            {
                case nameof(LiveMode):
                    ModeChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        public void NavigateTo(int index)
        {
            CurrentPage = index;
        }

        private static ViewModel? _instance;

        public static ViewModel Instance
        {
            get
            {
                _instance ??= new ViewModel();
                return _instance;
            }
        }
    }
}
