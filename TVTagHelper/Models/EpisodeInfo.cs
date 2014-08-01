using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTagHelper.Models
{
    public class EpisodeInfo
    {
        public int EpisodeNumber { get; set; }
        public int SeasonNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ShowName { get; set; }
        public long ShowId { get; set; }
        public string RunTime { get; set; }

        //  Need to make this a lazy lookup to get artwork based on
        //  showId (because it's higher quality artwork)
        public string ArtworkUrl { get; set; }
    }
}
