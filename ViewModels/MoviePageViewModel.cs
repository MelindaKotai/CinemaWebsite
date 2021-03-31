using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class MoviePageViewModel
    {

        public int id { get; set; }
        public string title { get; set; }
        public string video { get; set; }
        public string img { get; set; }
        public string description { get; set; }
        public int duration { get; set; }
        public DateTime release_date { get; set; }
        public List<string> genres { get; set; }
        public List<string> actors { get; set; }
        public double rating { get; set; }
        public List<DateTime> dates { get; set; }

        
        
       

    }
}
