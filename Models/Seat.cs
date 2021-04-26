using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
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
        public int order { get; set; }

        [Required]
        public int row { get; set; }
      
        public int? seatNo { get; set; }

        [JsonIgnore]
        public virtual Hall hall { get; set; }

        [JsonIgnore]
        public virtual ICollection<ReservedSeats> reservedSeats { get; set; }
    }
}
