using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class ReservedSeats
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public int seatId { get; set; }
        [Required]
        public int reservationId { get; set; }

        public bool claimed { get; set; }

        [Required]
        public int TicketTypeid { get; set; }

        public virtual TicketType TicketType { get; set; }
        public virtual Reservation reservation { get; set; }
        public virtual Seat seat { get; set; }

    }
}
