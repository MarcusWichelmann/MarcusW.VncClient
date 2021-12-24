using System.Windows;
using System.Windows.Controls;

namespace WpfVncClient;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private bool _fullscreen;
    private WindowState _state;
    private WindowStyle _style;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = App.Current?.GetService<ViewModel>();
        DataContext = viewModel;
    }

    private void FrameworkElement_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not ScrollViewer sv)
        {
            return;
        }

        if (sv.Content is not Control c)
        {
            return;
        }

        c.Width = e.NewSize.Width;
        c.Height = e.NewSize.Height;
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (!_fullscreen)
        {
            _state = WindowState;
            _style = WindowStyle;
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
        }
        else
        {
            WindowState = _state;
            WindowStyle = _style;
        }

        _fullscreen = !_fullscreen;
    }
}
