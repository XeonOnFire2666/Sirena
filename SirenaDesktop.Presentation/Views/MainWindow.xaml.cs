using System.Windows;
using SirenaDesktop.ViewModels;

namespace SirenaDesktop;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}