using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class Seat
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public int hallId { get; set; }
        [Required]
        public int number { get; set; }
        [Required]
        public int path { get; set; }
        public virtual Hall hall { get; set; }
        public virtual ICollection<ReservedSeats> reservedSeats { get; set; }
    }
}
