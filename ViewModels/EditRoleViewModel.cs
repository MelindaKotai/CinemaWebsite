using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class EditRoleViewModel
    {

        public string userId { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }
        public string email { get; set; }
        public string userRole { get; set; }
        public List<string> roles { get; set; }
    }
}
