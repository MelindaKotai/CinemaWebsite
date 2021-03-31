using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class Hall
    {
        [Key]
        [Required]
        public int id { get; set; }

        [Required]
        public string name { get; set; }

        [Required]
        public int rows { get; set; }

        [Required]
        public int columns { get; set; }

        [Required]
        public int active { get; set; }

        public virtual ICollection<Seat> Seat { get; set; }

 
    }
}
