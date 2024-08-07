using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace WpfVncClient.Logging;

public static class ReactiveLoggerExtensions
{
    public static ILoggingBuilder AddReactiveLogger(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ReactiveLoggerProvider>());

        return builder;
    }
}
