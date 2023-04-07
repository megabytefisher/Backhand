using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Backhand.ViewModels.Windows;
using Backhand.Windows;

namespace Backhand
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new HomeWindow
                {
                    DataContext = new HomeViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}