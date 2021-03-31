using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ScreeningReservations
    {
        public List<ScreeningReservationInfo> reservations { get; set; }
        public int screeningid { get; set; }
        public string title { get; set; }
        public DateTime s_hour { get; set; }
        public DateTime f_hour { get; set; }
    }
}
