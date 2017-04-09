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
                //List<string> fileTop = new List<string>();
                //fileTop.Add(files.First());
                Parallel.ForEach(files, (currentFile) =>
                    {
                        var guid = Guid.NewGuid().ToString();
                        CfdiFile file = new CfdiFile()
                        {
                            Guid = guid,
                            FileName = currentFile,
                            FileContent = uploadAndGetStorageUri(guid,currentFile) //""// File.ReadAllText(currentFile)
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

        private static  string uploadAndGetStorageUri(string guid,string currentFile)
        {
            string uri="";
            // Retrieve storage account from connection string.
            try

            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=dmfacturacion;AccountKey=zq6PuQhMKMb4+qaswC05TUTWFyJzTG4eAw+dgc+tJGYk+azuYmmhlwjSvg0PodFC1sw3tz1RRKHOu6DHU/IcZw==;EndpointSuffix=core.windows.net");

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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return uri;
        }
    }
}
