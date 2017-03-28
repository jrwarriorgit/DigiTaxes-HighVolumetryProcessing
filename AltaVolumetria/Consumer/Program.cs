using Domain;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "Endpoint=sb://dmservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=OHQ9AWdbOfGJ6uLiZswVQVfGw0NxE3I+v8M14fv7z8c=";
            var queueName = "ToProcessQueue";
            var sqlconnectionstring = "Server=tcp:dmvolumetriadbserver.database.windows.net,1433;Initial Catalog=dmVolumetria;Persist Security Info=False;User ID=jrwarrior;Password=XQh*9^kJS&ew;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            
            do
            {
                var files = client.ReceiveBatch(1000);
                Console.WriteLine(files.Count());
                Parallel.ForEach(files, (currentFile) =>
                {
                    try
                    {
                        CfdiFile file = currentFile.GetBody<CfdiFile>();
                        using (var cnn = new SqlConnection(sqlconnectionstring))
                        {
                            cnn.Open();
                            using (var cmd = cnn.CreateCommand())
                            {
                                cmd.CommandText = "INSERT INTO [dbo].[Facturas] ([Guid],[FileName],[FileContent]) VALUES (@Guid,@FileName,@FileContent)";
                                cmd.Parameters.AddWithValue("@Guid", file.Guid);
                                cmd.Parameters.AddWithValue("@FileName", file.FileName);
                                cmd.Parameters.AddWithValue("@FileContent", file.FileContent);
                                cmd.ExecuteNonQuery();

                            }
                        }

                        currentFile.Complete();
                    }
                    catch( Exception ex)
                    {
                        currentFile.Abandon();
                    }
                }
                );
                Thread.Sleep(1000);
            } while (true);
        }
    }
}
