using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class Reservation
    {
        [Key]
        [Required]
        public int id { get; set; }
        
        
        [Required]
        public int screeningId { get; set; }
        [Required]
        public int payed { get; set; }

       
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        
        public virtual User User { get; set; }
        public virtual Screening screening { get; set; }
        public ICollection<ReservedSeats> reservedSeats { get; set; }

    }
}
