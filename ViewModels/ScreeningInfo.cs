using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ScreeningInfo
    {

        public int screeningId { get; set; }
        public DateTime hour { get; set; }
        public bool is3D { get; set; }
    }
}
