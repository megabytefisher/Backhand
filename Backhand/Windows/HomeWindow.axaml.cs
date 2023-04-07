using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Backhand.ViewModels.Windows;
using ReactiveUI;

namespace Backhand.Windows
{
    public partial class HomeWindow : ReactiveWindow<HomeViewModel>
    {
        public HomeWindow()
        {
            this.WhenActivated(disposables =>
            {
                /* Handle view activation etc. */
            });
            InitializeComponent();
        }
    }
}