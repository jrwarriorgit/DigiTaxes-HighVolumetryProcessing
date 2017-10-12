using Configuration;
using Domain;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Data;
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
            var connectionString = InternalConfiguration.QueueConnectionString;
            var queueName = "01PublisherToConsumer";
            var sqlconnectionstring = InternalConfiguration.SqlConnectionString;

            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            var toSignClient = QueueClient.CreateFromConnectionString(connectionString, "02ConsumerToValidaRFC");
            var count = 0;
            do
            {
                var files = client.ReceiveBatch(1000);
                count = files.Count();
                Console.WriteLine(count);
                if (count > 0)
                {
                    var returnvalue = MakeTable(files);
                    var dataTable = returnvalue.Item1;
                    using (var cnn = new SqlConnection(sqlconnectionstring))
                    {
                        cnn.Open();
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(cnn))
                        {
                            bulkCopy.DestinationTableName =
                                "dbo.Facturas";

                            try
                            {
                                // Write from the source to the destination.
                                bulkCopy.WriteToServer(dataTable);
                                toSignClient.SendBatch(returnvalue.Item2);
                                client.CompleteBatch(from f in files
                                                     select f.LockToken);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
                //////////////Parallel.ForEach(files, (currentFile) =>
                //////////////{
                //////////////    try
                //////////////    {
                //////////////        CfdiFile file = currentFile.GetBody<CfdiFile>();
                //////////////        using (var cnn = new SqlConnection(sqlconnectionstring))
                //////////////        {
                //////////////            cnn.Open();
                //////////////            using (var cmd = cnn.CreateCommand())
                //////////////            {
                //////////////                cmd.CommandText = "INSERT INTO [dbo].[Facturas] ([Guid],[FileName],[FileContent]) VALUES (@Guid,@FileName,@FileContent)";
                //////////////                cmd.Parameters.AddWithValue("@Guid", file.Guid);
                //////////////                cmd.Parameters.AddWithValue("@FileName", file.FileName);
                //////////////                cmd.Parameters.AddWithValue("@FileContent", file.FileContent);
                //////////////                cmd.ExecuteNonQuery();

                //////////////            }
                //////////////        }

                //////////////        currentFile.Complete();
                //////////////    }
                //////////////    catch (Exception ex)
                //////////////    {
                //////////////        currentFile.Abandon();
                //////////////    }
                //////////////}
                /////////////);
                if (count==0)
                    Thread.Sleep(1000);
            } while (true);
        }

        private static Tuple<DataTable,List<BrokeredMessage>> MakeTable(IEnumerable<BrokeredMessage> messages)
        // Create a new DataTable named NewProducts. 
        {
            DataTable facturasDataTable = new DataTable("Facturas");
            List<BrokeredMessage> newqueue = new List<BrokeredMessage>();
            // Add three column objects to the table. 
            DataColumn id = new DataColumn();
            id.DataType = System.Type.GetType("System.Int64");
            id.ColumnName = "Id";
            id.AutoIncrement = true;
            facturasDataTable.Columns.Add(id);

            DataColumn guid = new DataColumn();
            guid.DataType = System.Type.GetType("System.String");
            guid.ColumnName = "Guid";
            facturasDataTable.Columns.Add(guid);

            DataColumn fileName = new DataColumn();
            fileName.DataType = System.Type.GetType("System.String");
            fileName.ColumnName = "FileName";
            facturasDataTable.Columns.Add(fileName);

            DataColumn fileContent = new DataColumn();
            fileContent.DataType = System.Type.GetType("System.String");
            fileContent.ColumnName = "FileContent";
            facturasDataTable.Columns.Add(fileContent);

            // Create an array for DataColumn objects.
            DataColumn[] keys = new DataColumn[1];
            keys[0] = id;
            facturasDataTable.PrimaryKey = keys;

            // Add some new rows to the collection. 
            foreach (var item in messages)
            {
                CfdiFile file = item.GetBody<CfdiFile>();
                NewRow(facturasDataTable, file);
                
                newqueue.Add(new BrokeredMessage(file) { SessionId=file.Guid });
            }

            facturasDataTable.AcceptChanges();

            // Return the new DataTable. 
            return new Tuple<DataTable,List<BrokeredMessage>>( facturasDataTable,newqueue);
        }

        private static DataRow NewRow(DataTable facturasDataTable,CfdiFile cfdiFile)
        {
            DataRow row = facturasDataTable.NewRow();
            row["Guid"] = cfdiFile.Guid;
            row["FileName"] = cfdiFile.FileName;
            row["FileContent"] = cfdiFile.FileContent;
            facturasDataTable.Rows.Add(row);
            return row;
        }
    }

}
