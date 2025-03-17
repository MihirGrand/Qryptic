using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Qryptic
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public ToastType Type { get; }

        public ToastEventArgs(string title, string message, ToastType type)
        {
            Title = title;
            Message = message;
            Type = type;
        }
    }

    public enum CodeType
    {
       Text, Website, Contact, Wifi, Email, SMS, Location, Event, UPI, Barcode, None
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



        void BackToMenu()
        {
            CurrentPage = 1;
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
