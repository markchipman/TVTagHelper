using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTagHelper.Models
{
    public class TVShowSeachResult
    {
        public TVShowSeachResult()
        {
            //  Initialize the episode list:
            this.Episodes = new List<EpisodeInfo>();
        }

        public string ShowName { get; set; }
        public int SeasonNumber { get; set; }
        public string ArtworkUrl { get; set; }
        public List<EpisodeInfo> Episodes { get; set; }
    }
}
