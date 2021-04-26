﻿using System;
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
        public string phone { get; set; }
        [Required]
        public int screeningId { get; set; }
        [Required]
        public int payed { get; set; }

        [Required]
        public string firstName { get; set; }
        [Required]
        public string lastName { get; set; }


        [Required]
        public string email { get; set; }

        [Required]
        public DateTime date { get; set; }
        public bool online { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        
        public virtual User User { get; set; }
        public virtual Screening screening { get; set; }
        public ICollection<ReservedSeats> reservedSeats { get; set; }

    }
}
