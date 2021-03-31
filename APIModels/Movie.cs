using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.APIModels
{
    public class Movie
    {
        public string id { get; set; }
        public int runningTimeInMinutes { get; set; }
        public string title { get; set; }
        public string titleType { get; set; }
        public int year { get; set; }

        public List<Actor> principals { get; set; }
    }
}
