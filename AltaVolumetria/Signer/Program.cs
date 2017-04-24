using Configuration;
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
            var connectionString = InternalConfiguration.QueueConnectionString;
            var queueName = "tosignstepkeyvault";

            var keyName = "PrincipalHsmKey";
            var keyVaultAddress = "https://prodkeyvaultforpac.vault.azure.net/";
            var keyVersion = "f1fdd90eb24f43dc81d3d91449020205";

            var applicationId = "70f7abb5-667f-4d5b-8635-71cd5de50d60";
            var clientSecret = "4gnYL5di7Kz2/gFaU1F2V73IGHm6fh/d+l6J4ZU8Wfw=";

            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                   (authority, resource, scope) => GetAccessToken(authority, resource, applicationId, clientSecret)),
                   new HttpClient());

            var client = QueueClient.CreateFromConnectionString(connectionString, queueName);
            
            var count = 0;
            do
            {
                Stopwatch swProcess = Stopwatch.StartNew();

                var files = client.ReceiveBatch(1000);
                count = files.Count();
                Console.WriteLine(count);
                if (count > 0)
                {
                    var rnd = new Random(DateTime.Now.Millisecond);
                    Parallel.ForEach(files, (currentFile) =>
                    {
                        try
                        {
                            var value = rnd.Next(1, 6);
                            var keyVaultNumber = value == 6 ? 1 : value;
                            var keyVaultNumberString = (keyVaultNumber != 1) ? $"{keyVaultNumber:D2}" : "";
                            var selectedVault = keyVaultAddress.Replace("prodkeyvaultforpac", $"prodkeyvaultforpac{keyVaultNumberString}");
                            var tuple = currentFile.GetBody<Tuple<CfdiFile,Cfdi>>();
                            var algorithm = JsonWebKeySignatureAlgorithm.RS256;
                            var signature = Task.Run(() => keyVaultClient.SignAsync(selectedVault, keyName, keyVersion, algorithm, Convert.FromBase64String(tuple.Item2.Sha256))).ConfigureAwait(false).GetAwaiter().GetResult();

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
