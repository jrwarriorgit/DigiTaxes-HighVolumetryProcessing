using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class FacturasBi
    {
        public FacturasBi()
        {
        }

        public FacturasBi(long numfacturas)
        {
            facturas = numfacturas;
            timestamp = DateTime.Now;
        }
        public long facturas { get; set; }
        public DateTime timestamp { get; set; }
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
