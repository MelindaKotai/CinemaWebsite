using Licenta.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ChooseTicketsViewModel
    {
        public int id { get; set; }
        public float price { get; set; }
        public List<TicketType> types { get; set; }
    }
}
