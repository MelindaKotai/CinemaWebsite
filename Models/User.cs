using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class User:IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [ForeignKey("userId")]
        public virtual ICollection<UserPrizes> UserPrizes{ get; set; }
        [ForeignKey("UserId")]
        public virtual ICollection<Reservation> Reservations { get; set; }
    }
}
