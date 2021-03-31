using Licenta.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ScreeningAdminList
    {
        public int mid { get; set; }
        public string title { get; set; }
        public string img { get; set; }
        public DateTime date { get; set; }
        public int sid { get; set; }
        public DateTime shour { get; set; }
        public DateTime fhour { get; set; }
        public int is3d { get; set; }
        public int hallid { get; set; }
        public Hall hall { get; set; }
    }
}
