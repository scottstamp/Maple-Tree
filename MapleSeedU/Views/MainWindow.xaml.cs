// Project: MapleSeedU
// File: MainWindow.xaml.cs
// Updated By: Scott Stamp <scott@hypermine.com>
// Updated On: 01/30/2017

using MapleSeedL.ViewModels;

namespace MapleSeedL.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = (MainWindowViewModel)DataContext;
            Closing += viewModel.OnWindowClosing;
        }
    }
}