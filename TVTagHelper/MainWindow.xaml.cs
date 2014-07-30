using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using iTunesSearch.Library;
using iTunesSearch.Library.Models;
using TVTagHelper.Models;

namespace TVTagHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<FileItem> fileItems = new ObservableCollection<FileItem>();
        ObservableCollection<TVShowSeachResult> tvShows = new ObservableCollection<TVShowSeachResult>();
        iTunesSearchManager iTunes = new iTunesSearchManager();

        public MainWindow()
        {
            InitializeComponent();

            /*
            fileItems.Add(new FileItem() { Title = "Complete this WPF tutorial"});
            fileItems.Add(new FileItem() { Title = "Learn C#"});
            fileItems.Add(new FileItem() { Title = "Wash the car"});

            List<EpisodeInfo> epItems = new List<EpisodeInfo>();
            epItems.Add(new EpisodeInfo() { EpisodeNumber = 1, Name = "Episode 1", Description = "Description 1" });
            epItems.Add(new EpisodeInfo() { EpisodeNumber = 2, Name = "Episode 2", Description = "Description 2" });
            epItems.Add(new EpisodeInfo() { EpisodeNumber = 3, Name = "Episode 3", Description = "Description 3" });
            tvShows.Add(new TVShowSeachResult() { ShowName = "Modern family", SeasonNumber = 1, Episodes = epItems, ArtworkUrl = "http://a4.mzstatic.com/us/r30/Video/67/db/24/mzl.avsbdzjp.100x100-75.jpg" });
            tvShows.Add(new TVShowSeachResult() { ShowName = "Modern family", SeasonNumber = 2, Episodes = epItems, ArtworkUrl = "http://a4.mzstatic.com/us/r30/Video/17/cb/33/mzl.vnswhqyb.100x100-75.jpg" });
            tvShows.Add(new TVShowSeachResult() { ShowName = "Breaking Bad", SeasonNumber = 1, Episodes = epItems, ArtworkUrl = "http://a3.mzstatic.com/us/r30/Features/fc/3c/14/dj.tkqxkglc.100x100-75.jpg" });
            */

            filesDataGrid.ItemsSource = fileItems;
            searchResults.ItemsSource = tvShows;
        }

        private void cmdSearch_Click(object sender, RoutedEventArgs e)
        {
            Task<TVEpisodeListResult> searchTask = iTunes.GetEpisodesForShow(txtSearch.Text, 500);
            searchTask.ContinueWith((t) =>
            {
                //  Clear our tvshows observable:
                tvShows.Clear();

                //  Get the results
                var results = t.Result;

                //  Group into seasons
                var seasons = from episode in results.Episodes
                              orderby episode.Number
                              group episode by episode.SeasonNumber into seasonGroup
                              orderby seasonGroup.Key
                              select seasonGroup;

                //  Create our structure:
                foreach(var seasonGroup in seasons)
                {
                    //  Filter our episodes:
                    var filteredEpisodes = from episode in seasonGroup
                                    where episode.PriceHD > 0
                                    select episode;

                    if(filteredEpisodes.Any())
                    {
                        //  Create the show result
                        TVShowSeachResult tvResult = new TVShowSeachResult()
                        {
                            SeasonNumber = seasonGroup.Key
                        };

                        //  Add the episodes
                        tvResult.Episodes = (from episode in filteredEpisodes
                                             select new EpisodeInfo()
                                             {
                                                 Name = episode.Name,
                                                 Description = episode.DescriptionShort,
                                                 EpisodeNumber = episode.Number
                                             }).ToList();

                        //  Set the show properties
                        tvResult.ArtworkUrl = filteredEpisodes.First().ArtworkUrl;
                        tvResult.ShowName = filteredEpisodes.First().ShowName;

                        //  Add the result to the list:
                        tvShows.Add(tvResult);
                    }
                }
            },
            TaskScheduler.FromCurrentSynchronizationContext());
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
            if(e.Data.GetDataPresent("TVTagHelper.Models.EpisodeInfo", true))
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
            if(e.Data.GetDataPresent("TVTagHelper.Models.EpisodeInfo", true))
            {
                var data = (EpisodeInfo)e.Data.GetData(typeof(EpisodeInfo));
                TextBlock text = (TextBlock)sender;
                text.Text = data.Name;
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            //  Check status of fileItems
            int test = fileItems.Count;
        }

        private void filesDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.DragOver += new DragEventHandler(fileRow_DragOver);
            e.Row.Drop += new DragEventHandler(fileRow_Drop);
        }

        private void fileRow_DragOver(object sender, DragEventArgs e)
        {
            bool dropEnabled = false;

            //  We need to check for the correct data format.  We want to
            //  allow updating titles with episode title information (not file drops)
            if(e.Data.GetDataPresent("TVTagHelper.Models.EpisodeInfo", true))
            {
                dropEnabled = true;

                //  Select the item and scroll it into view:
                DataGridRow r = (DataGridRow)sender;
                filesDataGrid.SelectedItem = r.Item;
                filesDataGrid.ScrollIntoView(r.Item);
            }

            if(!dropEnabled)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void fileRow_Drop(object sender, DragEventArgs e)
        {
            //  We need to check for the correct data format.  We want to
            //  allow updating titles with episode title information (not file drops)
            if(e.Data.GetDataPresent("TVTagHelper.Models.EpisodeInfo", true))
            {
                var data = (EpisodeInfo)e.Data.GetData(typeof(EpisodeInfo));
                FileItem file = (FileItem)filesDataGrid.SelectedItem;

                var item = fileItems.FirstOrDefault(f => f.Id == file.Id);
                if(item != null)
                {
                    item.Title = data.Name;
                    item.Description = data.Description;
                    item.EpisodeNumber = data.EpisodeNumber;
                }

                var oldindex = fileItems.IndexOf(item);
                fileItems[oldindex] = item;
            }
        }

    }
}
