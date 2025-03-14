using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qryptic
{
    public partial class ViewModel: ObservableObject
    {
        [ObservableProperty]
        private string _message = "Hello, WPF!";
    }
}
