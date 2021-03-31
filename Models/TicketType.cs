using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class TicketType
    {   
        [Key]
        [Required]
        public int id { get; set; }
        [Required]
        public string name { get; set; }
      
        [Required]
        public int active { get; set; }
        [Required]
        [Range(0, 100)]
        public float discount { get; set; }
    }
}
