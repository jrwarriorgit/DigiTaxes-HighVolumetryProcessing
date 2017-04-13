using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Configuration
{
    public class InternalConfiguration
    {
        private static string ApplicationId = "70f7abb5-667f-4d5b-8635-71cd5de50d60";
        private static string ApplicationKey = "4gnYL5di7Kz2/gFaU1F2V73IGHm6fh/d+l6J4ZU8Wfw=";
        private static string KeyVaultAddress = "https://prodrgatmskeyvault.vault.azure.net/";
        public static async Task<string> GetAccessToken(string authority, string resource, string clientId, string clientSecret)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var clientCredentials = new ClientCredential(clientId, clientSecret);
            var result = await context.AcquireTokenAsync(resource, clientCredentials).ConfigureAwait(false);

            return result.AccessToken;
        }

        public static string QueueConnectionString { get { return AppSettings("QueueConnectionString"); } }
        public static string RedisConnectionString { get { return AppSettings("RedisConnectionString"); } }
        public static bool EnableRedisCache { get {
                if (_enableRedisCache == null)
                    _enableRedisCache = AppSettings("EnableRedis").ToLower();
                return (_enableRedisCache=="true"); } }
        private static string _enableRedisCache = null;
        public static string[] Storages
        {
            get
            {
                var count = 1;
                var response = new List<string>();
                string secret= null;
                do
                {
                    try
                    {
                        secret = AppSettings($"Storage{count:D3}");
                        response.Add(secret);
                        count++;
                    }
                    catch
                    {
                        secret = null;
                    }
                } while (!string.IsNullOrEmpty(secret));
                return response.ToArray();
               
            }
        }

        public static string SqlConnectionString { get { return AppSettings("SqlConnectionString"); } }

        public static string AppSettings(string secretName)
        {
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                   (authority, resource, scope) => GetAccessToken(authority, resource, ApplicationId, ApplicationKey)),
                   new HttpClient()); ;
            return keyVaultClient.GetSecretAsync(KeyVaultAddress, secretName).Result.Value;

        }
    }

}
