using Domain;
using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Monitor
{
    public abstract class Monitor
    {
        bool firstAttemp = true;
        long initialCount = 0;
        long actualCount = 0;
        long diferentialCount = 0;

        HttpClient httpClient;
        string _powerBiUrlTotalInQueue;
        string _powerBiUrlLastExecution;

        internal string _connectionString;

        string _name;


        public Monitor(string connectionString, string name, string powerBiUrlTotalInQueue, string powerBiUrlLastExecution)
        {
            httpClient = new HttpClient();
            _powerBiUrlLastExecution = powerBiUrlLastExecution;
            _powerBiUrlTotalInQueue = powerBiUrlTotalInQueue;
            _connectionString = connectionString;
            _name = name;
        }
        public string Process()
        {
            actualCount = getActualValue();
            if (!firstAttemp)
            {
                diferentialCount = actualCount - initialCount;
            }
            firstAttemp = false;
            initialCount = actualCount;

            var response = $" - {_name} - Total:{actualCount} - Dif:{diferentialCount} ";
            if (!string.IsNullOrEmpty(_powerBiUrlTotalInQueue))
            {
                var result = httpClient.PostAsync(_powerBiUrlTotalInQueue,
                  new StringContent(new FacturasBi(actualCount).ToJson(), Encoding.UTF8, "application/json")).Result;
            }
            if (!string.IsNullOrEmpty(_powerBiUrlLastExecution))
            {
                var resultBis = httpClient.PostAsync(_powerBiUrlLastExecution,
                new StringContent(new FacturasBi(diferentialCount).ToJson(), Encoding.UTF8, "application/json")).Result;
            }
            return response;
        }

        internal abstract long getActualValue();
    }

    public class MonitorQueue : Monitor
    {
        NamespaceManager nsmgr;
        public string QueueName { get; set; }

        public MonitorQueue(string connectionString, string queueName, string powerBiUrlTotalInQueue, string powerBiUrlLastExecution) : base(connectionString,queueName ,  powerBiUrlTotalInQueue, powerBiUrlLastExecution)
        {
            nsmgr = NamespaceManager.CreateFromConnectionString(connectionString);
            QueueName = queueName;

        }

        internal override long getActualValue()
        {
            return nsmgr.GetQueue(QueueName).MessageCount;
        }
    }

    public class MonitorSql : Monitor
    {


        public MonitorSql(string connectionString,  string powerBiUrlTotalInQueue, string powerBiUrlLastExecution) : base(connectionString, "Sql" , powerBiUrlTotalInQueue, powerBiUrlLastExecution)
        {
            
        }

        internal override long getActualValue()
        {
            long response=0;
            try
            {
                using (var cnn = new SqlConnection(_connectionString))
                {
                    cnn.Open();
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = "spCuentaFacturas";
                        response= Convert.ToInt64(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

            }
            return response;
            
        }
    }
}
