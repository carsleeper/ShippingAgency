using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Text.Json;
using Utls;
using Encoding = System.Text.Encoding;


namespace Services
{
    public class NaverAuthService
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private string client_id = "";
        private string client_secret = "";
        private string pw;
        
        private string signature; 

        public string GetToken()
        {
            pw = client_id + "_" + timestamp;
            signature = Util.Encoding.toBase64(Util.Hash.Bcrypt(pw, client_secret));

            using (var client = new HttpClient())
            {
                var parameters = new Dictionary<string, string>
                {
                    { "client_id", client_id },
                    { "timestamp", timestamp.ToString() },
                    { "grant_type", "client_credentials" },
                    { "client_secret_sign", signature },
                    { "type", "SELF" }
                };
                
                var content = new FormUrlEncodedContent(parameters);
                
                var response = client.PostAsync("https://api.commerce.naver.com/external/v1/oauth2/token", content);

                string responseBody = response.Content.ReadAsStringAsync();
                
                var result = JsonSerializer.Deserialize<Dictionary<string,string>>(responseBody);
                return result["access_token"];
                
            }
            
        }
    }
}

namespace Util
{
    public class Encoding
    {
        public static string toBase64(string text)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(tmp);
        }

    }

    public class Hash
    {

        
        
        public static string Bcrypt(string pw, string salt)
        {
            string signature = BCrypt.Net.BCrypt.HashPassword(pw, salt);
            return signature;
        }
    }

}

