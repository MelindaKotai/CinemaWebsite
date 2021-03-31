using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class UserPrizes
    {
        [Required]
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("user")]
        public string userId { get; set; }
       
        
        public virtual User user{get;set;}
        [Required] 
        public int prizeId { get; set; }

        public virtual Prize prize { get; set; }


        [Required]
        public string code { get; set; }
        [Required]
        public int claimed { get; set; }
    }
}
