using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using iTunesSearch.Library;
using iTunesSearch.Library.Models;
using iTunesTVMetadata.Library;
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

        /// <summary>
        /// The location of the currently executing assembly
        /// </summary>
        private string currentPath = string.Empty;

        /// <summary>
        /// The location of the artwork cache
        /// </summary>
        private string artworkCachePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            filesDataGrid.ItemsSource = fileItems;
            searchResults.ItemsSource = tvShows;
            currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            artworkCachePath = Path.Combine(currentPath, "ArtworkCache");
        }

        private async void cmdSearch_Click(object sender, RoutedEventArgs e)
        {
            cmdSearch.IsEnabled = false;

            //  Get the results
            TVEpisodeListResult results = await iTunes.GetTVEpisodesForShow(txtSearch.Text, 500);

            //  Clear our tvshows observable:
            tvShows.Clear();

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
                                             SeasonId = item.Episode.SeasonId,
                                             ShowName = item.Episode.ShowName,
                                             EpisodeNumber = item.OrdinalPosition,
                                             SeasonNumber = item.Episode.SeasonNumber,
                                             Rating = item.Episode.Rating,
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
        }

        /// <summary>
        /// Gets the large artwork url for a given show
        /// </summary>
        /// <param name="seasonId"></param>
        /// <returns></returns>
        private async Task<string> GetLargeArtworkForSeasonId(long seasonId)
        {
            string retval = string.Empty;

            //  If we don't have it in cache, get it in cache
            if(!seasonArtworkCache.ContainsKey(seasonId))
            {
                TVSeasonListResult result = await iTunes.GetTVSeasonById(seasonId);
                
                //  If we got results back...
                if(result.Seasons.Any())
                {
                    //  Make sure the artwork cache directory exists:
                    if(!Directory.Exists(artworkCachePath))
                        Directory.CreateDirectory(artworkCachePath);

                    //  Get information about the artwork
                    var showInfo = result.Seasons.First();
                    Uri uri = new Uri(showInfo.ArtworkUrlLarge);
                    string artworkExtension = Path.GetExtension(uri.LocalPath);

                    //  Get our target filename and save the artwork to the cache directory:
                    string savedArtworkPath = Path.Combine(artworkCachePath,
                        string.Format("{0}_S{1}{2}", showInfo.ShowName, showInfo.SeasonNumber, artworkExtension)
                        );

                    WebClient web = new WebClient();
                    web.DownloadFile(showInfo.ArtworkUrlLarge, savedArtworkPath);

                    //  Store the item in the cache:
                    seasonArtworkCache.AddOrUpdate(seasonId, savedArtworkPath, (key, oldValue) => savedArtworkPath);
                }

            }

            //  Return it from cache:
            try
            {
                retval = seasonArtworkCache[seasonId];
            }
            catch(Exception)
            { }

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

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            //  For each of the items
            for(int i = fileItems.Count - 1; i >= 0; i--)
            {
                var item = fileItems[i];

                if(!string.IsNullOrWhiteSpace(item.ShowName))
                {
                    //  Get the artwork path for the showId:
                    string artworkPath = await GetLargeArtworkForSeasonId(item.SeasonId);

                    //  Set the meta information
                    MetadataManager.SetTVMetaData(item.FilePath, new TVMetadata()
                    {
                        ShowRating = item.Rating,
                        ShowArtworkPath = artworkPath,
                        ShowName = item.ShowName,
                        ShowSeason = item.SeasonNumber,
                        EpisodeTitle = item.Title,
                        EpisodeDescription = item.Description,
                        EpisodeNumber = item.EpisodeNumber
                    });
                }

                fileItems.RemoveAt(i);
            }

            //  Indicate that the process completed
            lblStatus.Visibility = System.Windows.Visibility.Visible;
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
            //  Reset the status indicator
            lblStatus.Visibility = System.Windows.Visibility.Hidden;

            //  We need to check for the correct data format.  We want to
            //  allow updating titles with episode title information (not file drops)
            if(e.Data.GetDataPresent("TVTagHelper.Models.EpisodeInfo", true) && filesDataGrid.SelectedItem != null)
            {
                var data = (EpisodeInfo)e.Data.GetData(typeof(EpisodeInfo));
                FileItem file = (FileItem)filesDataGrid.SelectedItem;

                //  Try to find the item in the backing store:
                var item = fileItems.FirstOrDefault(f => f.Id == file.Id);
                if(item != null)
                {
                    //  Update properties:
                    item.Rating = data.Rating;
                    item.Title = data.Name;
                    item.SeasonNumber = data.SeasonNumber;
                    item.SeasonId = data.SeasonId;
                    item.ShowName = data.ShowName;
                    item.Description = data.Description;
                    item.EpisodeNumber = data.EpisodeNumber;
                }

                //  Save the item in the backing store:
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

        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            //  Show the settings page
            //  Code taken shamelessly from http://stackoverflow.com/a/6417636/19020
            SettingsWindow settings = new SettingsWindow()
            {
                Title = "Settings",
                ShowInTaskbar = false,
                Topmost = true,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };

            settings.ShowDialog();
        }

    }
}
