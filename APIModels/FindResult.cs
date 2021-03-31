using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.APIModels
{
    public class FindResult
    {
       public string query { get; set; }
       public List<Movie> results;

    }
}
