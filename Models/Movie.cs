using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class Movie
    {
        [Key]
        [Required]
        public int id { get; set; }
        [Required]
        public string title { get; set; }
  
        public string img { get; set; }
       
        public string video { get; set; }
        [Required]
        public string description { get; set; }
        [Required]
        public int duration { get; set; }
        [Required]
        [DataType(DataType.Date)]
        
        public DateTime date_added { get; set; }
        [Required]
        [DataType(DataType.Date)]
       
        public DateTime release_date { get; set; }
        [Required]
        public int active { get; set; }
        public virtual ICollection<Screening> Screenings { get; set; }

    }
}
