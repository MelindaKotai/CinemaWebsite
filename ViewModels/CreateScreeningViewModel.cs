using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class CreateScreeningViewModel
    {
        [Required(ErrorMessage = "Sala este obligatorie!")]
        [Display(Name = "Sala")]
        public int hallId { get; set; }
        [Required(ErrorMessage = "Filmul este obligatoriu!")]
        [Display(Name = "Filmul")]
        public int movieId { get; set; }
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Data este obligatorie!")]
        [Display(Name = "Data")]
        public DateTime date { get; set; }
        [Required(ErrorMessage = "Ora de inceput este obligatorie!")]
        [DataType(DataType.Time)]
        [Display(Name = "Ora")]
        public DateTime s_hour { get; set; }

        [Required(ErrorMessage = "Prețul este obligatoriu")]
        [Display(Name = "Preț")]
        public float price { get; set; }
        [Required(ErrorMessage = "Trebuie să alegeți o opțiune!")]
        [Display(Name = "Ecranizare 3D")]
        public bool is3D { get; set; }

    }
}
