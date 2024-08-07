using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AvaloniaVncClient.ViewModels;

namespace AvaloniaVncClient;

public class ViewLocator : IDataTemplate
{
    public bool SupportsRecycling => false;

    public Control Build(object? data)
    {
        string? viewName = data?.GetType().FullName?.Replace("ViewModel", "View");
        if (viewName == null)
        {
            return new TextBlock { Text = "Not Found" };
        }

        var viewType = Type.GetType(viewName);
        if (viewType == null)
        {
            return new TextBlock { Text = "Not Found: " + viewName };
        }

        return (Control)Activator.CreateInstance(viewType)!;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
