using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace TVTagHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ObservableCollection<FileList> fileItems = new ObservableCollection<FileList>();
            fileItems.Add(new FileList() { Title = "Complete this WPF tutorial", Completion = 45 });
            fileItems.Add(new FileList() { Title = "Learn C#", Completion = 80 });
            fileItems.Add(new FileList() { Title = "Wash the car", Completion = 0 });

            List<TVShowSeachResult> items = new List<TVShowSeachResult>();
            List<EpisodeInfo> epItems = new List<EpisodeInfo>();
            epItems.Add(new EpisodeInfo() { EpisodeNumber = 1, Name = "Episode 1", Description = "Description 1" });
            epItems.Add(new EpisodeInfo() { EpisodeNumber = 2, Name = "Episode 2", Description = "Description 2" });
            epItems.Add(new EpisodeInfo() { EpisodeNumber = 3, Name = "Episode 3", Description = "Description 3" });
            items.Add(new TVShowSeachResult() { ShowName = "Modern family", SeasonNumber = 1, Episodes = epItems, ArtworkUrl = "http://a4.mzstatic.com/us/r30/Video/67/db/24/mzl.avsbdzjp.100x100-75.jpg" });
            items.Add(new TVShowSeachResult() { ShowName = "Modern family", SeasonNumber = 2, Episodes = epItems, ArtworkUrl = "http://a4.mzstatic.com/us/r30/Video/17/cb/33/mzl.vnswhqyb.100x100-75.jpg" });
            items.Add(new TVShowSeachResult() { ShowName = "Breaking Bad", SeasonNumber = 1, Episodes = epItems, ArtworkUrl = "http://a3.mzstatic.com/us/r30/Features/fc/3c/14/dj.tkqxkglc.100x100-75.jpg" });

            fileList.ItemsSource = fileItems;
            searchResults.ItemsSource = items;
        }

        private void cmdSearch_Click(object sender, RoutedEventArgs e)
        {
            //  Use this as a temporary check of fileList
            var test = fileList.ItemsSource;

        }

        private void dgEpisodes_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                DataGrid mydatagrid = (DataGrid)sender;

                Point currentPosition = e.GetPosition(mydatagrid);
                var selectedItem = mydatagrid.SelectedItem;

                //  If we have something selected...
                if(selectedItem != null)
                {
                    DataGridRow container = (DataGridRow)mydatagrid.ItemContainerGenerator.ContainerFromItem(selectedItem);

                    //  If we found our container ... 
                    if(container != null)
                    {
                        DragDropEffects finalDropEffect = DragDrop.DoDragDrop(container, selectedItem, DragDropEffects.Copy);
                    }
                }
            }
        }

        private void txtFileTitle_DragOver(object sender, DragEventArgs e)
        {
            bool dropEnabled = false;

            //  We need to check for the correct data format.  We want to
            //  allow updating titles with episode title information (not file drops)
            if(e.Data.GetDataPresent("TVTagHelper.EpisodeInfo", true))
            {
                dropEnabled = true;
            }

            if(!dropEnabled)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void txtFileTitle_Drop(object sender, DragEventArgs e)
        {
            //  We need to check for the correct data format.  We want to
            //  allow updating titles with episode title information (not file drops)
            if(e.Data.GetDataPresent("TVTagHelper.EpisodeInfo", true))
            {
                var data = (EpisodeInfo)e.Data.GetData(typeof(EpisodeInfo));
                TextBlock text = (TextBlock)sender;
                text.Text = data.Name;
            }
        }
    }
}
