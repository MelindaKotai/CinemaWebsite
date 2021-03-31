using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ScreeningMovies
    {

        public int id { get; set; }
        public string img { get; set; }
        public string title { get; set; }
        public List<ScreeningInfo> screenings { get; set; }

        public DateTime date { get; set; }
    }
}
