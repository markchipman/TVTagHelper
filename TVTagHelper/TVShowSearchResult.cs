using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTagHelper
{
    public class TVShowSeachResult
    {
        public string ShowName { get; set; }
        public int SeasonNumber { get; set; }
        public string ArtworkUrl { get; set; }
        public List<EpisodeInfo> Episodes { get; set; }
    }

    public class EpisodeInfo
    {
        public int EpisodeNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
