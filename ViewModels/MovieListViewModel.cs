using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class MovieListViewModel
    {
       public int id { get; set; }
        
        public string title { get; set; }
        public string img { get; set; }
       public DateTime date_added { get; set; }
       public DateTime release_date { get; set; }
       public int  active { get; set; }
    }
}
