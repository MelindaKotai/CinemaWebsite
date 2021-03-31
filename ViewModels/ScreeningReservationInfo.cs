using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ScreeningReservationInfo
    {
        public int id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string email { get; set; }
        public float total { get; set; }
        public int payed { get; set; }
        public List<TypeNameNumber> tickets { get; set; }
        public Dictionary<int,List<int>> seats { get; set; }

    }
}