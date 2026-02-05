using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using LibreMed.Services;
using LibreMed.ViewModels;
using LibreMed.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LibreMed;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<IWindowService, WindowService>();
        serviceCollection.AddSingleton<IFileSaveDialogService, AvaloniaFileSaveDialogService>();

        Services = serviceCollection.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var fileDialogs = (IFileSaveDialogService)Services.GetService(typeof(IFileSaveDialogService))!;

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(fileDialogs),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}