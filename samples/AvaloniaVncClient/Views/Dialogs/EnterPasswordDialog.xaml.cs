using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaVncClient.Views.Dialogs;

public class EnterPasswordDialog : Window
{
    public EnterPasswordDialog()
    {
        InitializeComponent();
    }

    private TextBox PasswordTextBox => this.FindControl<TextBox>("PasswordTextBox")!;

    public void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    public void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(PasswordTextBox.Text);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
