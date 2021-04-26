using Licenta.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class HallStructureViewModel
    {

        public int hallid { get; set; }
        public string name { get; set; }
        public List<Seat> seats { get; set; }
      
        public int cols { get; set; }

    }
}
