using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Licenta.Models
{
    public class Prize
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public string name { get; set; }

        [Required]
        public int active { get; set; }

        [JsonIgnore]
        public virtual ICollection<UserPrizes> userPrizes { get; set; }

    }
}
