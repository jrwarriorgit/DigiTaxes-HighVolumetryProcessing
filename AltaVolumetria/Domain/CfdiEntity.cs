using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class CfdiEntity:TableEntity
    {
        public CfdiEntity(string guid, string xml)
        {
            Guid = guid;
            Xml = xml;
            this.RowKey = Guid;
            this.PartitionKey = "none";

        }

        public CfdiEntity() { }

        public string Guid { get; set; }

        public string Xml { get; set; }
    }
}
