﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class UserViewModel
    {
     
        public string id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        public List<PrizesViewModel> userprizes { get; set; }


    }
}
