using Licenta.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class PrizePageViewModel
    {
        public List<PrizesViewModel> unclaimedprizes { get; set; }
        public int noofprizes { get; set; }
        public int noofticketsleft { get; set; }
        public List<Prize> prizes { get; set; }



    }
}
