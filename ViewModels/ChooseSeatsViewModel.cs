using Licenta.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ChooseSeatsViewModel
    {
        
        public int screeningId { get; set; }
        public List<Seat> seats{ get;set; }
        public List<int> reserved { get; set; }
        public int cols { get; set; }
        public float total { get; set; }
        public int notickets { get; set; }


        [EmailAddress(ErrorMessage = "Formatul nu este potrivit!")]
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
        public string selectedseats { get; set; }


        [Required]
        [Phone(ErrorMessage = "Formatul nu este potrivit!")]
        [DisplayName("Telefon")]
        public string telefon { get; set; }

        public string userId { get; set; }


        [Required]
        public string action { get; set; }


        public string Token { get; set; }
       
    }
}
