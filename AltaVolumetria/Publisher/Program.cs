using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.ServiceBus.Messaging;
using Domain;

namespace Publisher
{
    class Program
    {
        static string processId = Guid.NewGuid().ToString();
        static void Main(string[] args)
        {
            var connectionString = "Endpoint=sb://dmservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=OHQ9AWdbOfGJ6uLiZswVQVfGw0NxE3I+v8M14fv7z8c=";
            var queueName = "ToProcessQueue";
            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            Console.WriteLine("[{0}] Process Started", processId);
            do
            {
                var files = Directory.GetFiles(Environment.CurrentDirectory + "/xml");
                Parallel.ForEach(files, (currentFile) =>
                    {
                        CfdiFile file = new CfdiFile()
                        {
                            Guid = Guid.NewGuid().ToString(),
                            FileName = currentFile,
                            FileContent = File.ReadAllText(currentFile)
                        };
                        var message = new BrokeredMessage(file);
                        client.Send(message);
                    }
                );
            } while (true);
        }
    }
}
