using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ReservationPageList
    {

        public List<ReservationPageViewModel> past { get; set; }
        public List<ReservationPageViewModel> future { get; set; }
    }
}
