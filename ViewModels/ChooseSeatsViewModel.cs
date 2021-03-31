using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ChooseSeatsViewModel
    {
        
        public int screeningId { get; set; }
        public int rows { get; set; }
        public int cols { get; set; }
        public List<int> paths{ get;set; }
        public List<int> reserved { get; set; }
        public List<int> seatids { get; set; }
        public float total { get; set; }
        public int notickets { get; set; }
    }
}
