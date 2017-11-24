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
using Configuration;

namespace Publisher
{
    class Program
    {
        static string processId = Guid.NewGuid().ToString();

        static void Main(string[] args)
        {
            var destinationQueue = QueueClient.CreateFromConnectionString(InternalConfiguration.QueueConnectionString, "01PublisherToConsumer");

            var storages = InternalConfiguration.Storages;
            
            Console.WriteLine("[{0}] Process Started", processId);
            do
            {
                try
                {
                    var files = Directory.GetFiles(Environment.CurrentDirectory + "/xml");
                    Console.WriteLine($"Se agregan {files.Count()} archivos a procesar");

                    Parallel.ForEach(files, (currentFile) =>
                        {
                            var guid = Guid.NewGuid().ToString();
                            Tuple<string, string> tuple;

                            if (InternalConfiguration.EnableInLineXML)
                                tuple = new Tuple<string, string>(File.ReadAllText(currentFile), "inline");
                            else
                                tuple = uploadAndGetStorageUri(guid, currentFile, storages);
                            CfdiFile file = new CfdiFile()
                            {
                                Guid = guid,
                                FileName = currentFile,
                                FileContent = tuple.Item1,
                                FechaCreacion = DateTime.Now,
                                Storage = tuple.Item2
                            };
                            try
                            {
                                destinationQueue.Send(new BrokeredMessage(file) { SessionId = file.Guid });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }

                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            } while (true);
        }

        private static Tuple<string,string> uploadAndGetStorageUri(string guid,string currentFile, string[] storages)
        {
            string uri="";
            int hashcode = guid.GetHashCode();
            var rnd = new Random(hashcode).Next(0, storages.Length - 1);
            var selectedStorage = storages[rnd];
            // Retrieve storage account from connection string.
            try

            {
               
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(selectedStorage);

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve reference to a previously created container.
                
                var leftstring = $"{new Random(hashcode).Next(0,999):D3}".Substring(0,3);
                CloudBlobContainer container = blobClient.GetContainerReference($"{leftstring}facturas");

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
                Console.WriteLine($"{selectedStorage} - {ex.ToString()}");
            }
            return new Tuple<string, string>(uri,storages[rnd]);
        }
    }
}
