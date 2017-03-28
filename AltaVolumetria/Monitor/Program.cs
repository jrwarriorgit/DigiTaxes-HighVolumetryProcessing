using Domain;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Monitor
{
    class Program
    {
        static  void Main(string[] args)
        {
            var connectionString = "Endpoint=sb://dmservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=OHQ9AWdbOfGJ6uLiZswVQVfGw0NxE3I+v8M14fv7z8c=";
            var queueName = "ToProcessQueue";
            var sqlconnectionstring = "Server=tcp:dmvolumetriadbserver.database.windows.net,1433;Initial Catalog=dmVolumetria;Persist Security Info=False;User ID=jrwarrior;Password=XQh*9^kJS&ew;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            
            var nsmgr = Microsoft.ServiceBus.NamespaceManager.CreateFromConnectionString(connectionString);
            HttpClient httpClient = new HttpClient();


            long initialCount = 0;// nsmgr.GetQueue(queueName).MessageCount;
            long initialSqlCount = 0;

            long actualCount = 0;
            long actualSqlCount = 0;

            long diferentialCount = 0;
            long diferentialSqlCount = 0;

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(EverySecond);
            timer.Interval = 1000; // 1000 ms is one second
            timer.Start();

            void EverySecond(object source, ElapsedEventArgs e)
            {
                using (var cnn = new SqlConnection(sqlconnectionstring))
                {
                    cnn.Open();
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = "Select * From FacturasCount";
                        actualSqlCount=Convert.ToInt64( cmd.ExecuteScalar());
                    }
                }
                actualCount = nsmgr.GetQueue(queueName).MessageCount;
                if (initialCount != 0)
                { 
                    diferentialCount = actualCount - initialCount;
                    diferentialSqlCount = actualSqlCount - initialSqlCount;
                }
                initialCount = actualCount;
                initialSqlCount = actualSqlCount;



                Console.WriteLine("{0} - Total:{1} - Dif:{2}", DateTime.Now.ToLongTimeString(),actualCount,diferentialCount);
                var result=httpClient.PostAsync("https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/6fbe1b74-0310-4747-b694-e5fed797d34d/rows?key=aFU9V1Y6EQUpIPZHin4pa7VX66HGmVM%2BZSkih53Nl0VicRBdA8ffHLO%2BUafvWUErPaV4Ut1wg8RmJIQuRHR5oA%3D%3D", new StringContent(new FacturasBi(actualCount).ToJson(), Encoding.UTF8, "application/json")).Result;
                var resultBis = httpClient.PostAsync("https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/5fac7cbb-653f-4342-8211-8b674259641a/rows?key=eudZSUPzwbiDOC5l9YJE6hBP4m6zNdRrNwV4VDKB8WE1aQwoGaJBYVQig%2BTVhZbpn0RuVZQ6pJoSTZ0B%2FzlcFw%3D%3D", new StringContent(new FacturasBi(diferentialCount).ToJson(), Encoding.UTF8, "application/json")).Result;
                var resultProcesadas = httpClient.PostAsync("https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/88f274fc-26d8-4841-87be-3665fdb3889c/rows?key=UZ9ukHOkM%2B9k2dkW24ZCW8h0U3un5DJ13aBLI4Sovb4VL3fw1ejZYgdv9bgonsbK88L%2BpWjihgijrB3%2FyteZeQ%3D%3D", new StringContent(new FacturasBi(diferentialSqlCount).ToJson(), Encoding.UTF8, "application/json")).Result;

            }
            new System.Threading.AutoResetEvent(false).WaitOne();

        }
    }

}
