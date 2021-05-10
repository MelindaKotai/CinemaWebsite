using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class CreateMovieViewModel
    {
        [Required]
        [DisplayName("Titlu")]
        [MaxLength(100)]
        public string title { get; set; }
        
        [Required]
        [DisplayName("Încărcați o imagine")]
        public IFormFile img { get; set; }
       
        [Required]
        [DisplayName("Încărcați un trailer")]
        public IFormFile video { get; set; }
      
        [Required]
        [DisplayName("Descriere")]
        public string description { get; set; }
       
        [Required]
        [DisplayName("Durata în minute")]
        
        public int duration { get; set; }
       
     
        [Required]
        [DisplayName("Data apariției")]
        [DataType(DataType.Date)]

        public DateTime release_date { get; set; }
        
        [Required]
        [DisplayName("Filmul va rula în prezent?")]
        [Range(0, 1)]
        public bool active { get; set; }
    }
}
