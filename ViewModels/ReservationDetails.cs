using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ReservationDetails
    {
        public int screeningId { get; set; }
        public string email { get; set; }
        public string nume { get; set; }
        public string prenume { get; set; }
        public string seats { get; set; }
        public string userId { get; set; }
        public string action { get; set; }
        
    }
}
