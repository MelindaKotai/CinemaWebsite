using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ChangeStatusViewModel
    {

        public string title { get; set; }
        public DateTime date { get; set; }
        public DateTime hour { get; set; }
        public string type { get; set; }
        public int row { get; set; }
        public int seatNo { get; set; }
        public string hallname { get; set; }
    }
}
