using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class HomePageViewModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string imgpath { get; set; }

       
        [DataType(DataType.Date)]
        public DateTime release_date { get; set; }
    }
}
