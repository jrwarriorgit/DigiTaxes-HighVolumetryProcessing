using Configuration;
using Domain;
using Microsoft.ServiceBus;
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

            var connectionString = InternalConfiguration.QueueConnectionString;
            var sqlconnectionstring = InternalConfiguration.SqlConnectionString;
            var listMonitorQueue = new List<Monitor>();
            listMonitorQueue.Add(new MonitorSql(sqlconnectionstring ,
                null, "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/88f274fc-26d8-4841-87be-3665fdb3889c/rows?key=UZ9ukHOkM%2B9k2dkW24ZCW8h0U3un5DJ13aBLI4Sovb4VL3fw1ejZYgdv9bgonsbK88L%2BpWjihgijrB3%2FyteZeQ%3D%3D"));
            listMonitorQueue.Add(new MonitorQueue(connectionString, "ToProcessQueue",
                "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/6fbe1b74-0310-4747-b694-e5fed797d34d/rows?key=aFU9V1Y6EQUpIPZHin4pa7VX66HGmVM%2BZSkih53Nl0VicRBdA8ffHLO%2BUafvWUErPaV4Ut1wg8RmJIQuRHR5oA%3D%3D"
                , "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/5fac7cbb-653f-4342-8211-8b674259641a/rows?key=eudZSUPzwbiDOC5l9YJE6hBP4m6zNdRrNwV4VDKB8WE1aQwoGaJBYVQig%2BTVhZbpn0RuVZQ6pJoSTZ0B%2FzlcFw%3D%3D"));
            listMonitorQueue.Add(new MonitorQueue(connectionString, "ToSignQueue",
                "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/57803735-43a3-462c-9a6e-606184190e51/rows?key=a6kWykNXbrGHPM6NphnPQfsfAblZ3zBTC0TUXkZqBAObJ1LuJKHOVWLDTCetR0x%2FuTLPeDXqSts257ZhhKmY%2Bw%3D%3D"
                , "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/1e83da64-1b07-4439-8fe9-377b971f4d92/rows?key=sX2kFaqR0bxXe3UEY2Or%2FsVIOvudSvft0bpeMX6PYD5cyvn4NpNZ7uFE%2Fip8oWuGXOiiw72MOeEcO9btF9a4aA%3D%3D"));
            listMonitorQueue.Add(new MonitorQueue(connectionString, "tosignstepkeyvault",
                "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/811e6811-4f42-4a89-afac-4a8504c3a09a/rows?key=3utsLJUaq4fYfBeB6UCekznLPTDuebHjx2wlBXllhAUqjwIEU6XXTG1C9IGSMYWEvkpa%2BXgeTBvdTbldAopSCA%3D%3D"
                , "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/d528a902-20b3-42f4-9ba3-24a7aa4af3b4/rows?key=m56wE2vdJjZ33WdaxvZP%2BAg3sNTCjcGW9mmR3ABs2E5Mwy45Bzi8kOqBbxmIOXX2TL%2Fo7jlxBq%2FXwryVLYEjDw%3D%3D"));


            

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(EverySecond);
            timer.Interval = 1000;
            timer.Start();

            void EverySecond(object source, ElapsedEventArgs e)
            {
                StringBuilder consoleText= new StringBuilder();
                consoleText.Append($"{DateTime.Now.ToLongTimeString()}");
                foreach (var monitor in listMonitorQueue)
                {
                    consoleText.Append (monitor.Process());
                }
                Console.WriteLine(consoleText);
            }
            new System.Threading.AutoResetEvent(false).WaitOne();

        }
        
        

    }
   
}
