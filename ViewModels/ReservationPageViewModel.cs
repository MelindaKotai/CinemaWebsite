using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ReservationPageViewModel
    {
        public int id { get; set; }
        public string movieTitle { get; set; }
        public string movieImg { get; set; }
        public DateTime screeningDate { get; set; }
        public DateTime screeningTime { get; set; }
        public bool is3D { get; set; }
        public float total { get; set; }
        public bool payed { get; set; }
        public List<TypeNameNumber> tickets { get; set; }


    }
}
