using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.ViewModels
{
    public class VerifyModelError
    {
        public string action { get; set; }
        public string message { get; set; }
        public bool isValid { get; set; }
    }
}
