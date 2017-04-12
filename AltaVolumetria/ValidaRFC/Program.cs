﻿using Domain;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ValidaRFC
{
    class Program
    {

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect("dmRedizTest.redis.cache.windows.net:6380,password=saGLpB+N6FF/bZFarS5UnfBK003DiTULqbofeA1NGQE=,ssl=True,abortConnect=False");
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        static void Main(string[] args)
        {
            var connectionString = "Endpoint=sb://prodvolservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=k9X/1hnaxSuUe1Mpa0GIUSeemmk4K6Dj3NZ5TKAyNuA=";
            var queueName = "ToSignQueue";

            //var sqlconnectionstring = "Server=tcp:proddbvolumetriaserver.database.windows.net,1433;Initial Catalog=prodDbVolumetria;Persist Security Info=False;User ID=jrwarrior;Password=l00MdPbig3fZ;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            // Create the blob client.
            //CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            //CloudBlobContainer container = blobClient.GetContainerReference("facturas");

            //////IDatabase cache = Connection.GetDatabase();

            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            var toSignKeyVault = QueueClient.CreateFromConnectionString(connectionString, "tosignstepkeyvault");
            var count = 0;
            do
            {
                Stopwatch swProcess = Stopwatch.StartNew();

                var files = client.ReceiveBatch(1000);
                count = files.Count();
                Console.WriteLine(count);
                if (count > 0)
                {
                    
                    Parallel.ForEach(files, (currentFile) =>
                    {
                        try
                        {
                            CfdiFile file = currentFile.GetBody<CfdiFile>();
                            // Retrieve storage account from connection string.
                            //ServicePointManager.DefaultConnectionLimit = 10000;
                            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(file.Storage);//"DefaultEndpointsProtocol=https;AccountName=dmfacturacion;AccountKey=zq6PuQhMKMb4+qaswC05TUTWFyJzTG4eAw+dgc+tJGYk+azuYmmhlwjSvg0PodFC1sw3tz1RRKHOu6DHU/IcZw==;EndpointSuffix=core.windows.net");
                            //CloudBlockBlob blob = new CloudBlockBlob(new Uri(file.FileContent), storageAccount.Credentials);//container.GetBlockBlobReference(file.FileContent);

                            ////////////////

                            // Retrieve the storage account from the connection string.
                            

                            // Create the table client.
                            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                            // Create the CloudTable object that represents the "people" table.
                            CloudTable table = tableClient.GetTableReference("cfdi");

                            // Create a retrieve operation that takes a customer entity.
                            TableOperation retrieveOperation = TableOperation.Retrieve<CfdiEntity>("none", file.Guid);

                            // Execute the retrieve operation.
                            TableResult retrievedResult = table.Execute(retrieveOperation);

                            // Print the phone number of the result.
                            if (retrievedResult.Result != null)
                            {
                                var xml = ((CfdiEntity)retrievedResult.Result).Xml;
                                byte[] byteArray = Encoding.UTF8.GetBytes(xml);
                                MemoryStream stream = new MemoryStream(byteArray);





                                ///////////////

                                
                                
                                //var requestOptions = new BlobRequestOptions();
                                //requestOptions.ParallelOperationThreadCount = 1000;
                                var cfdi = new Cfdi();
                                //var xml=blob.DownloadText();

                                //using (var stream = blob.OpenRead())//null,requestOptions))
                                //{
                                    cfdi = new Cfdi(stream);
                                //}
                           
                            ////////Stream stream = new MemoryStream();
                            ////////blob.DownloadToStream(stream);
                            ////////cfdi = new Cfdi(stream);


                            //////Stopwatch sw = Stopwatch.StartNew();
                            //////cfdi.ValidaRfcEmision(cache.StringGet(cfdi.RfcEmisor));
                            //////cfdi.ValidaRfcReceptor(cache.StringGet(cfdi.RfcReceptor));
                            //////cfdi.ValidationTimeSpend = sw.ElapsedMilliseconds;

                            toSignKeyVault.Send(new BrokeredMessage(new Tuple<CfdiFile, Cfdi>(file, cfdi)));
                           
                            currentFile.Complete();
                            }
                        }
                        catch (Exception ex)
                        {
                            currentFile.Abandon();
                        }
                    }
                    );
                }
                if (swProcess.ElapsedMilliseconds > 1000) Console.WriteLine($"-> [{count} / {swProcess.ElapsedMilliseconds/1000}] = {count / (swProcess.ElapsedMilliseconds / 1000)} x segundo");
                if (count == 0)
                    Thread.Sleep(1000);
            } while (true);
        }

    }
}
