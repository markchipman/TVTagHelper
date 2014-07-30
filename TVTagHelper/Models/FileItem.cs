﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTagHelper.Models
{
    public class FileItem
    {
        public string ShowName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int EpisodeNumber { get; set; }
        public int Completion { get; set; }
    }
}
