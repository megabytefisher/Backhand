using System;
using System.Reactive.Disposables;
using Backhand.Services;
using Backhand.Services.Sync;
using ReactiveUI;
using Splat;

namespace Backhand.ViewModels.Windows
{
    public class HomeViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        private SyncService _syncService;

        public HomeViewModel(SyncService? syncService = null)
        {
            _syncService = syncService ?? Locator.Current.GetService<SyncService>() ?? throw new ArgumentNullException(nameof(syncService));
            
            Activator = new ViewModelActivator();
            this.WhenActivated((CompositeDisposable disposables) =>
            {
                _syncService.Start();
                
                Disposable.Create(() =>
                    {
                        /* Handle deactivation */
                    })
                    .DisposeWith(disposables);
            });
        }
    }
}