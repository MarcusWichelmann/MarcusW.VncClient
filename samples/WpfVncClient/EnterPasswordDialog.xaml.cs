using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfVncClient;

/// <summary>
///     Interaction logic for EnterPasswordDialog.xaml
/// </summary>
public partial class EnterPasswordDialog : Window
{
    public EnterPasswordDialog()
    {
        InitializeComponent();
        PasswordTextBox.Focus();
    }

    public string Password => PasswordTextBox.Password;

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
