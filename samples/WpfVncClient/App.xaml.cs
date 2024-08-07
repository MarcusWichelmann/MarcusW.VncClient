using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WpfVncClient.Logging;
using WpfVncClient.Services;

namespace WpfVncClient;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<ReactiveLog>();

        serviceCollection.AddLogging(builder => {
            builder.AddConsole();
            builder.AddDebug();
            builder.AddReactiveLogger();
        });

        serviceCollection.AddSingleton<ConnectionManager>();
        serviceCollection.AddTransient<ViewModel>();
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public new static App? Current => Application.Current as App;

    public IServiceProvider ServiceProvider { get; set; }

    public T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
}
