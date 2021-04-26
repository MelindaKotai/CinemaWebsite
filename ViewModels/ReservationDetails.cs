using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ReservationDetails
    {
        [Required]
        public int screeningId { get; set; }

        [EmailAddress(ErrorMessage ="Formatul nu este potrivit!")]
        [DisplayName("Email")]
        [Required]
        public string email { get; set; }


        
        [DisplayName("Nume")]
        [Required]
        public string nume { get; set; }


        
        [DisplayName("Prenume")]
        [Required]
        public string prenume { get; set; }

        [Required]
        public string seats { get; set; }


     
        public string userId { get; set; }


   
        [Required]
        public string action { get; set; }
        
    }
}
