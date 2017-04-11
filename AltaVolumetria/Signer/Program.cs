using Domain;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signer
{
    
    class Program
    {
        static KeyVaultClient keyVaultClient;
        static void Main(string[] args)
        {
            var connectionString = "Endpoint=sb://prodvolservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=k9X/1hnaxSuUe1Mpa0GIUSeemmk4K6Dj3NZ5TKAyNuA=";
            var queueName = "tosignstepkeyvault";

            var keyName = "dmKeyRulo";
            var keyVaultAddress = "https://dmkeyvaultrulo.vault.azure.net/";
            var keyVersion = "bb4d7a3af0884f139944fe050a1907a5";

            var applicationId = "f6644b58-0d44-411c-a290-69b71f092e99";
            var clientSecret = "L0Z4GZrmN6bIRdGTYr13KisXDFeZC14QdK4zxVSgz58=";

            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                   (authority, resource, scope) => GetAccessToken(authority, resource, applicationId, clientSecret)),
                   new HttpClient());

            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            //var toSignKeyVault = QueueClient.CreateFromConnectionString(connectionString, "tosignstepkeyvault");
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
                            var tuple = currentFile.GetBody<Tuple<CfdiFile,Cfdi>>();
                            var algorithm = JsonWebKeySignatureAlgorithm.RS256;
                            var signature = Task.Run(() => keyVaultClient.SignAsync(keyVaultAddress, keyName, keyVersion, algorithm, Convert.FromBase64String(tuple.Item2.Sha256))).ConfigureAwait(false).GetAwaiter().GetResult();

                            //toSignKeyVault.Send(new BrokeredMessage(new Tuple<CfdiFile, Cfdi>(file, cfdi)));

                            currentFile.Complete();
                        }
                        catch (Exception ex)
                        {
                            currentFile.Abandon();
                        }
                    }
                    );
                }
                if (swProcess.ElapsedMilliseconds > 1000) Console.WriteLine($"-> [{count} / {swProcess.ElapsedMilliseconds / 1000}] = {count / (swProcess.ElapsedMilliseconds / 1000)} x segundo");
                if (count == 0)
                    Thread.Sleep(1000);
            } while (true);

        }
        public static async Task<string> GetAccessToken(string authority, string resource, string clientId, string clientSecret)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var clientCredentials = new ClientCredential(clientId, clientSecret);
            var result = await context.AcquireTokenAsync(resource, clientCredentials).ConfigureAwait(false);

            return result.AccessToken;
        }
    }
}
