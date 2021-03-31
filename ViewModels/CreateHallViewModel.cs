using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class CreateHallViewModel
    {
            [Required]
            [MaxLength(20)]
            public string name { get; set; }

            [Required]
            [Range(0,50)]
            public int rows { get; set; }

            [Required]
            [Range(0, 50)]
            public int cols { get; set; }


            public string paths { get; set; }




    }
}
