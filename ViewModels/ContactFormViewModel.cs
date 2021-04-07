using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class ContactFormViewModel
    {
        [Required]
        [DisplayName("Nume")]
        public string fullName { get; set; }

        [Required]
        [EmailAddress]
        [DisplayName("Email")]
        public string email { get; set; }

        [Required]
        [Phone]
        [DisplayName("Telefon")]
        public string phone { get; set; }

        [Required]
        [DisplayName("Mesaj")]
        public string mesage { get; set; }
    }
}
