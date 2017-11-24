using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Domain
{
    public class CfdiFile
    {
        public int ID { get; set; }
        public string Guid { get; set; }
        public string FileName { get; set; }
        public string FileContent { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Storage { get; set; }
    }

    public class Cfdi
    {
       

        public string UUID { get; set; }
        public string Folio { get; set; }
        public string RfcReceptor { get; set; }
        public string RfcEmisor { get; set; }
        public string RfcEmisorCatalogo { get; set; }
        public string RfcReceptorCatalogo { get; set; }
        public long ValidationTimeSpend { get; set; }

        public string Sha256 { get; set; }

        public Dictionary<string,bool> Validaciones { get; set; }

        public Cfdi()
        {
            Validaciones = new Dictionary<string, bool>();
        }

        public Cfdi(Stream stream):base()
        {
            
            Load(stream);
            
        }

        public Cfdi(string xml)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(xml);
            MemoryStream stream = new MemoryStream(byteArray);
            Load(stream);
        }

        private void Load(Stream stream)
        {
            stream.Position = 0;
            using (var reader = XmlReader.Create(stream))
            {
                var elementsToFind = 3;
                while (elementsToFind > 0 && reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        
                        switch (reader.Name)
                        {
                            
                            case "cfdi:Emisor":
                                RfcEmisor = reader["rfc"];
                                elementsToFind--;
                                break;

                            case "cfdi:Receptor":
                                RfcReceptor = reader["rfc"];
                                elementsToFind--;
                                break;

                            case "tfd:TimbreFiscalDigital":
                                UUID = reader["UUID"];
                                elementsToFind--;
                                break;

                        }

                    }
                }
            }
           
            stream.Position = 0;
            Sha256 = Convert.ToBase64String(SHA256.Create().ComputeHash(stream));


        }

        public void ValidaRfcReceptor(string value)
        {
            Validaciones.Add("RfcReceptor", !string.IsNullOrEmpty(value));
            RfcReceptorCatalogo = value;
        }

        public void ValidaRfcEmision(string value)
        {
            Validaciones.Add("RfcEmisor", !string.IsNullOrEmpty(value));
            RfcEmisorCatalogo = value;
        }
    }
}
