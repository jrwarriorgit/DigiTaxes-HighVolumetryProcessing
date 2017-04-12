using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.ServiceBus.Messaging;
using Domain;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Table;

namespace Publisher
{
    class Program
    {
        static string processId = Guid.NewGuid().ToString();

        static void Main(string[] args)
        {
            //var connectionString = "Endpoint=sb://dmservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=OHQ9AWdbOfGJ6uLiZswVQVfGw0NxE3I+v8M14fv7z8c=";
            var connectionString = "Endpoint=sb://prodvolservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=k9X/1hnaxSuUe1Mpa0GIUSeemmk4K6Dj3NZ5TKAyNuA=";
            var queueName = "ToProcessQueue";
            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            Console.WriteLine("[{0}] Process Started", processId);
            do
            {
                var files = Directory.GetFiles(Environment.CurrentDirectory + "/xml");
                Console.WriteLine($"Se agregan {files.Count()} archivos a procesar");
                //List<string> fileTop = new List<string>();
                //fileTop.Add(files.First());
                Parallel.ForEach(files, (currentFile) =>
                    {
                        var guid = Guid.NewGuid().ToString();
                        var tuple=uploadAndGetStorageUri(guid, currentFile);
                        CfdiFile file = new CfdiFile()
                        {
                            Guid = guid,
                            FileName = currentFile,
                            FileContent = tuple.Item1,
                            FechaCreacion = DateTime.Now,
                            Storage= tuple.Item2
                        };
                        try
                        {
                            var message = new BrokeredMessage(file);
                            client.Send(message);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                );
            } while (true);
        }

        private static Tuple<string,string> uploadAndGetStorageUri(string guid,string currentFile)
        {
            string uri="";
            string[] storages =
                {
                    "DefaultEndpointsProtocol=https;AccountName=dmfacturacion;AccountKey=zq6PuQhMKMb4+qaswC05TUTWFyJzTG4eAw+dgc+tJGYk+azuYmmhlwjSvg0PodFC1sw3tz1RRKHOu6DHU/IcZw==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion02;AccountKey=WIxu9f5GC4aBU2+HdKa3n67fgnwG4N/+c49WPDlkbusBhIsHBotSntm3IfX0kGOOyxIha02RV7xIBZDf8kvvVA==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion03;AccountKey=aQYvhrsGeV70qqqgzo8+H9oDcv8erThSUjgY/MoXc9K9rWF5fsWoktqWo2VScdwcph3fxfbtMyJTESds0McgdQ==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion04;AccountKey=YXZhLIQQbx3AR6WU4Y7v13kBnrFBaD2VciJNZcnyOvHC96j0/x/mV9p606K97+RJ9hWfnF46n2XMU+JdygfILg==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion05;AccountKey=RCse6DueK1JdTfRnaFVena2wvBBBwcZcou6R812ukwal+aUbhX9zXDFfcRrqApTBxZAZbhTMOZxJ++3grCSJXA==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion06;AccountKey=Uk5cnOu67G5rPrUfJRPzEpiT3mRplva2AeMIWGMBiajCHflWy3z01SUkZDdYd8x2XJNYZC65600icKwbcCl5TQ==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion07;AccountKey=1+4tJk0FaGDodQZ/pHZuCgzbZuf5DuBT73xeIusRAEKCFFNnNhJOJeO+Y9hCaaTS2NRtrOw2qfks0E43MX596w==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion08;AccountKey=Xd3JCYflwuPQeHRLVD+vVkvFC0X6EIsLHrGWetQmySEqJAAN74Vspwz9JhjS88vwtjWw5WV5VKFMrrhj+wk69w==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion09;AccountKey=sZbHd2tXs8jfX6b2vsxC86yGOMNehSx986+Jk06k9y/WIPhc1C+ThYWUc6gDINil+fTcz8Ue2xuVQ6r2jXgZBg==;EndpointSuffix=core.windows.net"
                    ,"DefaultEndpointsProtocol=https;AccountName=dmfacturacion10;AccountKey=V3Ex/xZ3tDwnln5z/PMpIT1oDpaSxVB8SFh3Tdu5WVjWgj1IiHEJXSmJhqyK6BsjZRkk9MMEIVKYOqEwFWJ4WA==;EndpointSuffix=core.windows.net"

                };
            var rnd = 0;// new Random(guid.GetHashCode()).Next(0, storages.Length - 1);
            // Retrieve storage account from connection string.
            try

            {
                
                
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storages[rnd]);

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                
                // Retrieve reference to a previously created container.
                CloudBlobContainer container = blobClient.GetContainerReference("facturas");

                if (!container.Exists())
                    container.CreateIfNotExists();
                //container.CreateIfNotExists();
               

                // Retrieve reference to a blob named "myblob".
                var fileName = Path.GetFileName(currentFile);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference($"{DateTime.Now.ToString("yyyy-MM-dd/HH-mm/ss")}/{guid}--{fileName}");

                blockBlob.UploadFromFile(currentFile);
                //// Create or overwrite the "myblob" blob with contents from a local file.
                //using (var fileStream = System.IO.File.OpenRead(@"path\myfile"))
                //{
                //    blockBlob.UploadFromStream(fileStream);
                //}
                uri = blockBlob.Uri.AbsoluteUri;


                //////////////////
                // Retrieve the storage account from the connection string.
                var enableInsertInTableStorage = false;
                // Create the table client.
                if (enableInsertInTableStorage)
                {
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                    // Create the CloudTable object that represents the "people" table.
                    CloudTable table = tableClient.GetTableReference("cfdi");

                    table.CreateIfNotExists();
                    // Create the TableOperation object that inserts the customer entity.
                    TableOperation insertOperation = TableOperation.Insert(new CfdiEntity(guid, File.ReadAllText(currentFile)));

                    // Execute the insert operation.
                    table.Execute(insertOperation);
                }




            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new Tuple<string, string>(uri,storages[rnd]);
        }
    }
}
