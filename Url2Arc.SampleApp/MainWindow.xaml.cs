using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Url2Arc.SampleApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel.MainWindow viewModel = new ViewModel.MainWindow();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this.viewModel;
        }

        private void buttonToAdd_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.AddUrlToUrls();
        }

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.RemoveUrl(listBoxUrls.SelectedIndex);
        }

        private void buttonSaveFileDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.DefaultExt = "zip";
            dialog.AddExtension = true;
            dialog.Filter = "ZIP(.zip)|*.zip";

            var result = dialog.ShowDialog();
            if (result == true)
            {
                this.viewModel.PathToSave = dialog.FileName;
            }
        }

        private async void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            buttonRun.IsEnabled = false;
            await this.viewModel.Run();
            MessageBox.Show(this.viewModel.LastResult);
            buttonRun.IsEnabled = true;
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.ClearUrls();
        }
    }
}
