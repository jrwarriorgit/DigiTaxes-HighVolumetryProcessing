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
            try
            {
                var connectionString = InternalConfiguration.QueueConnectionString;
                var sqlconnectionstring = InternalConfiguration.SqlConnectionString;
                var listMonitorQueue = new List<Monitor>();
                listMonitorQueue.Add(new MonitorSql(sqlconnectionstring,
                    InternalConfiguration.AppSettings("02SQL-Total")
                    , InternalConfiguration.AppSettings("02SQL-Diferencial")));
                listMonitorQueue.Add(new MonitorQueue(connectionString, "01PublisherToConsumer",
                    InternalConfiguration.AppSettings("01PublisherToConsumer-Total")
                    , InternalConfiguration.AppSettings("01PublisherToConsumer-Diferencial")));
                listMonitorQueue.Add(new MonitorQueue(connectionString, "02ConsumerToValidaRFC",
                    InternalConfiguration.AppSettings("02ConsumerToValidaRFC-Total")
                    , InternalConfiguration.AppSettings("02ConsumerToValidaRFC-Diferencial")));
                listMonitorQueue.Add(new MonitorQueue(connectionString, "03ValidaRFCToSigner",
                    InternalConfiguration.AppSettings("03ValidaRFCToSigner-Total")
                    , InternalConfiguration.AppSettings("03ValidaRFCToSigner-Diferencial")));



                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Elapsed += new ElapsedEventHandler(EverySecond);
                timer.Interval = 1000;
                timer.Start();

                void EverySecond(object source, ElapsedEventArgs e)
                {
                    StringBuilder consoleText = new StringBuilder();
                    consoleText.Append($"{DateTime.Now.ToLongTimeString()}");
                    foreach (var monitor in listMonitorQueue)
                    {
                        try
                        {
                            consoleText.Append(monitor.Process());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            //throw ex;
                        }
                    }
                    Console.WriteLine(consoleText);
                }
                new System.Threading.AutoResetEvent(false).WaitOne();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }
        
        

    }
   
}
