using FoxIRCClient.ViewModels;
using System.Windows;

namespace FoxIRCClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();
        }
    }
}