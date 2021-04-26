using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ReportsViewModel
    {

        public List<SoldTicketsPerMovie> soldTicketsPerMovies { get; set; }
        public Dictionary<string,float> revenuePerMovies { get; set; }
        
        public int totalTickets { get; set; }
        public float totalRevenue { get; set; }
    
        
        public int ticketsSoldOnline { get; set; }
        public int ticketsSoldAtCinema { get; set; }
   
        
    }
}
