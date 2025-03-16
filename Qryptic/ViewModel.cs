using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public partial class ViewModel: ObservableObject
    {
        [ObservableProperty]
        public int currentPage = 0;

        public void ShowToast(String title, String message, ToastType type)
        {
            AnimationRequested?.Invoke(this, new ToastEventArgs(title, message, type));
        }

        public event EventHandler<ToastEventArgs>? AnimationRequested;

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
