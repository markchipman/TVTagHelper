using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        
        /// <summary>
        /// Allowed video file extensions (for drop)
        /// </summary>
        private List<string> allowedExtensions = new List<string>() { ".mp4", ".m4v" };
        
        /// <summary>
        /// The iTunes search manager
        /// </summary>
        iTunesSearchManager iTunes = new iTunesSearchManager();

        /// <summary>
        /// The large artwork url cache (based on seasonId)
        /// </summary>
        private ConcurrentDictionary<long, string> seasonArtworkCache = new ConcurrentDictionary<long, string>();

        public MainWindow()
        {
            InitializeComponent();

            /*
            fileItems.Add(new FileItem() { Title = "Complete this WPF tutorial"});
            fileItems.Add(new FileItem() { Title = "Learn C#"});
            fileItems.Add(new FileItem() { Title = "Wash the car"});
            */

            filesDataGrid.ItemsSource = fileItems;
            searchResults.ItemsSource = tvShows;
        }

        private void cmdSearch_Click(object sender, RoutedEventArgs e)
        {
            cmdSearch.IsEnabled = false;
            
            Task<TVEpisodeListResult> searchTask = iTunes.GetTVEpisodesForShow(txtSearch.Text, 500);
            searchTask.ContinueWith((t) =>
            {
                //  Clear our tvshows observable:
                tvShows.Clear();

                //  Get the results
                var results = t.Result;

                //  Group into seasons
                var seasons = from episode in results.Episodes
                              orderby episode.Number
                              group episode by new { episode.ShowName, episode.SeasonNumber } into seasonGroup
                              orderby seasonGroup.Key.ShowName, seasonGroup.Key.SeasonNumber
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
                            SeasonNumber = seasonGroup.Key.SeasonNumber
                        };

                        //  Get the episode along with its ordinal position
                        var filteredEpisodesWithPosition = (from episode in filteredEpisodes
                                                            select episode).Select((episode, index) =>
                                                            new
                                                            {
                                                                Episode = episode,
                                                                OrdinalPosition = index + 1
                                                            });

                        //  Add the episodes
                        tvResult.Episodes = (from item in filteredEpisodesWithPosition
                                             select new EpisodeInfo()
                                             {
                                                 Name = item.Episode.Name,
                                                 Description = item.Episode.DescriptionShort,
                                                 ShowId = item.Episode.ShowId,
                                                 EpisodeNumber = item.OrdinalPosition,
                                                 RunTime = TimeSpan.FromMilliseconds(Convert.ToDouble(item.Episode.RuntimeInMilliseconds)).ConciseFormat()
                                             }).ToList();

                        //  Set the show properties
                        tvResult.ArtworkUrl = filteredEpisodes.First().ArtworkUrl;
                        tvResult.ShowName = filteredEpisodes.First().ShowName;

                        //  Add the result to the list:
                        tvShows.Add(tvResult);
                    }
                }

                cmdSearch.IsEnabled = true;
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Gets the large artwork url for a given show
        /// </summary>
        /// <param name="seasonId"></param>
        /// <returns></returns>
        private string GetLargeArtworkForSeasonId(long seasonId)
        {
            string retval = string.Empty;

            //  If we don't have it in cache, get it in cache
            if(!seasonArtworkCache.ContainsKey(seasonId))
            {
                Task<TVSeasonListResult> lookupTask = iTunes.GetTVSeasonById(seasonId);
                lookupTask.ContinueWith((t) =>
                {
                    //  If we got results back...
                    if(t.Result.Seasons.Any())
                    {
                        string artworkUrl = t.Result.Seasons.First().ArtworkUrlLarge;
                        seasonArtworkCache.AddOrUpdate(seasonId, artworkUrl, (key, oldValue) => artworkUrl );
                    }
                }, 
                TaskScheduler.FromCurrentSynchronizationContext());
            }

            //  Return it from cache:
            retval = seasonArtworkCache[seasonId];

            return retval;
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

            //  If it's episode information, we need to check the data under
            //  the drag over location...
            if(e.Data.GetDataPresent("TVTagHelper.Models.EpisodeInfo", true))
            {
                DataGridRow r = (DataGridRow)sender;

                //  Make sure there is FileItem data there...
                if(r != null && r.Item.GetType() == typeof(FileItem))
                {
                    dropEnabled = true;

                    //  Select the item and scroll it into view:                
                    filesDataGrid.SelectedItem = r.Item;
                    filesDataGrid.ScrollIntoView(r.Item);
                }
            }

            //  If it's a new file it's OK to drop (but it will end up adding
            //  to the list)
            if(e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                foreach(string filename in filenames)
                {
                    if(allowedExtensions.Contains(Path.GetExtension(filename).ToLowerInvariant()))
                    {
                        //  we have at least one video file
                        dropEnabled = true;
                        break;
                    }
                }
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
            if(e.Data.GetDataPresent("TVTagHelper.Models.EpisodeInfo", true) && filesDataGrid.SelectedItem != null)
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

            //  If it's a file drop, let's get any videos not already added
            //  and add them to our list:
            if(e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                foreach(string filename in filenames)
                {
                    if(allowedExtensions.Contains(Path.GetExtension(filename).ToLowerInvariant()))
                    {
                        //  Check to see if the path of the video exists...
                        var existingItems = from item in fileItems
                                            where item.FilePath.Equals(filename)
                                            select item;

                        if(!existingItems.Any())
                        {
                            //  If it doesn't, add it:
                            fileItems.Add(new FileItem()
                                {
                                    FilePath = filename,
                                    Title = Path.GetFileNameWithoutExtension(filename)
                                });
                        }
                    }
                }
            }
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            //  Should we check to see if there is an action in progress first?

            //  Exit the app:
            Application.Current.Shutdown();
        }

    }
}
