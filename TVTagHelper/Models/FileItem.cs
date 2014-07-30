using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTagHelper.Models
{
    public class FileItem
    {
        public FileItem()
        {
            this.Id = Guid.NewGuid();
        }

        public string Path { get; set; }
        public string ShowName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int EpisodeNumber { get; set; }
        public int Completion { get; set; }
        public Guid Id { get; set; }
    }
}
