using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.APIModels
{
    public class IMDBRating
    {

        public string id { get; set; }
        public string title { get; set; }
        public string titleType { get; set; }
        public int year { get; set; }

        public bool canRate { get; set; }
        public double rating { get; set; }
        public int ratingCount { get; set; }
    }
}
