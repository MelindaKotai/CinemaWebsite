using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class SessionObject
    {

        public List<KVTickets> tickets { get; set; }
        public ChooseSeatsViewModel details { get; set; }
    }
}
