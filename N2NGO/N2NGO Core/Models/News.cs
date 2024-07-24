using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N2NGO_Core.Models
{
    public class News
    {
        public readonly Tuple<DateTime, int> Version;
        public readonly string Id;
        public readonly string Title;
        public readonly string Content;

        public News (Tuple<DateTime, int> version, string id, string title, string content)
        {
            Version = version; 
            Id = id;
            Title = title;
            Content = content;
        }
    }
}
