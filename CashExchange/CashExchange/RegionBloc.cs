using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CashExchange
{
    public class RegionalBloc
    {
        public string acronym { get; set; }
        public string name { get; set; }
        public List<object> otherAcronyms { get; set; }
        public List<object> otherNames { get; set; }
    }
}
