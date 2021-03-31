using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class TicketTypesViewModel
    {
        [Required]
        [MaxLength(25)]
        public string name { get; set; }

        [Required]
        [Range(0,100)]
        public float discount { get; set; }
    }
}
