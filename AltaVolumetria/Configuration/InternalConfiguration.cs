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

        
        private static string[] _storages;
        private static Dictionary<string, string> _secrets = new Dictionary<string, string>();
        private static Dictionary<string, bool> _banderas = new Dictionary<string, bool>();

        public static async Task<string> GetAccessToken(string authority, string resource, string clientId, string clientSecret)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var clientCredentials = new ClientCredential(clientId, clientSecret);
            var result = await context.AcquireTokenAsync(resource, clientCredentials).ConfigureAwait(false);

            return result.AccessToken;
        }

        public static string QueueConnectionString { get { return GetSecret("QueueConnectionString"); } }
        public static string RedisConnectionString { get { return GetSecret("RedisConnectionString"); } }
        public static bool EnableRedisCache{ get { return GetBandera("EnableRedis"); } }
        public static bool EnableInLineXML { get { return GetBandera("EnableInLineXML"); } }
        public static string[] Storages { get { return GetConfigurationArray(_storages, "Storage"); } }

        private static string GetSecret( string secretName)
        {
            if(!_secrets.ContainsKey(secretName) || refreshNeeded)
                _secrets[secretName] = AppSettings(secretName);

            return _secrets[secretName];
        }
        private static bool GetBandera( string secretName)
        {
            if (!_banderas.ContainsKey(secretName) || refreshNeeded )
                _banderas[secretName] = AppSettings(secretName).ToLower()=="true";

            return _banderas[secretName];
        }

        private static string[] GetConfigurationArray(string[] localVariable, string secretName)
        {
            if (localVariable == null || refreshNeeded)
            {
                var count = 1;
                var response = new List<string>();
                string secret = null;
                do
                {
                    try
                    {
                        secret = AppSettings($"{secretName}{count:D3}");
                        response.Add(secret);
                        count++;
                    }
                    catch
                    {
                        secret = null;
                    }
                } while (!string.IsNullOrEmpty(secret));
                localVariable= response.ToArray();
            }
            return localVariable;
        }


        public static string SqlConnectionString { get { return AppSettings("SqlConnectionString"); } }

        //TODO: Logic about refresh
        public static bool refreshNeeded { get { return false; } }

        static KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                  (authority, resource, scope) => GetAccessToken(authority, resource, ApplicationId, ApplicationKey)),
                  new HttpClient()); 
        public static string AppSettings(string secretName)
        {
            return keyVaultClient.GetSecretAsync(KeyVaultAddress, secretName).Result.Value;
        }
    }

}
