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
        public string Name { get; set; }
        public string Description { get; set; }
        public string ArtworkUrl { get; set; }
        public string ShowName { get; set; }
        public string RunTime { get; set; }
    }
}
