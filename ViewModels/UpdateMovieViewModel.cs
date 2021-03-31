using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class UpdateMovieViewModel
    {
        [Required]
        public int id { get; set; }

        public string img1 { get; set; }
        public string video1 { get; set; }
        [Required]
        [DisplayName("Titlu")]
        [MaxLength(100)]
        public string title { get; set; }

     
        [DisplayName("Incarcati o imagine noua")]
        public IFormFile img { get; set; }

   
        [DisplayName("Incarcati un trailer nou")]
        public IFormFile video { get; set; }

        [Required]
        [DisplayName("Descriere")]
        public string description { get; set; }

        [Required]
        [DisplayName("Durata in minute")]

        public int duration { get; set; }


        [Required]
        [DisplayName("Data aparitiei")]
        [DataType(DataType.Date)]

        public DateTime release_date { get; set; }

        [Required]
        [DisplayName("Filmul ruleaza in prezent?")]
        [Range(0,1)]
        public int active { get; set; }
    }
}
