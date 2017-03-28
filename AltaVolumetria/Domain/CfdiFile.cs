using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class CfdiFile
    {
        public int ID { get; set; }
        public string Guid { get; set; }
        public string FileName { get; set; }
        public string FileContent { get; set; }
    }
}
