using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class Screening
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public int hallId { get; set; }
        [Required]
        public int movieId { get; set; }
        [DataType(DataType.Date)]
        [Required]
        public DateTime date { get; set; }
        [Required]
        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:H:mm}")]
        public DateTime s_hour { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:H:mm}")]
        public DateTime f_hour { get; set; }

        [Required]
        public float price { get; set; }
        [Required]
        public int is3D{ get; set; }

        public virtual Movie movie { get; set; }
        public virtual Hall hall { get; set; }


}
}
