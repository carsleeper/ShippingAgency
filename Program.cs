using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Text.Json;
using System.Text;


namespace Naver
{
    public class Services
    {
        
        private static string client_id = "";
        private static string client_secret = "";
        private static string access_token = "";
        

        public static async Task GetToken()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); 
            string pw = client_id + "_" + timestamp;
            string signature = Util.Encodings.toBase64(Util.Hash.Bcrypt(pw, client_secret));
            
            string url = "https://api.commerce.naver.com/external/v1/oauth2/token";
            
            var parameters = new Dictionary<string, string>
            {
                { "client_id", client_id },
                { "timestamp", timestamp.ToString() },
                { "grant_type", "client_credentials" },
                { "client_secret_sign", signature },
                { "type", "SELF" }
            };

            var result =  await Util.Upload.postUpload(parameters, url,Util.Upload.method.x_www_form_urlencoded);
            access_token = result["access_token"];
        }
        
        
            
    }
    class Product
    {
        //members
        public enum statusType { WAIT, SALE, OUTOFSTOCK, SUSPENSION, CLOSE, DELETE };
        public enum saleType { NEW, OLD };
        public enum channelProductDisplayStatusType {WAIT,ON,SUSPENSION};
        
        private Dictionary<string, object> originProduct;
        
        private Dictionary<string, object> smartstoreChannelProduct;

        
        //constructor
        public Product()
        {
            originProduct = new Dictionary<string, object>()
            {
                { "statusType", statusType.SALE },
                { "saleType", saleType.NEW },
                { "leafCategoryId", 0 },
                { "name", "상품 이름" },
                { "detailContent", "상품 상세 정보" },
                { "image", new Dictionary<string,object> 
                    { 
                        {"representativeImage", new Dictionary<string,string>{{"url",""}} },   //url은 api에서 제공받아야 함
                        {"optionalImages", new List<Dictionary<string,string>>
                            {
                                new Dictionary<string, string> {{"url",""}},
                                new Dictionary<string, string> {{"url",""}},
                                new Dictionary<string, string> {{"url",""}}
                            }
                    
                        }
                    } 
                },
                {"saleStartDate","yyyy-MM-dd'T'HH:mm[:ss][.SSS]XXX"},
                {"saleEndDate","yyyy-MM-dd'T'HH:mm[:ss][.SSS]XXX"},
                {"salePrice",0},
                {"stockQuantity",0},
                {"detailAttribute", new Dictionary<string, object>
                    {
                        {"afterServiceInfo", new Dictionary<string, string>
                                {
                                    {"afterServiceTelephoneNumber","010-1111-1111"},
                                    {"afterServiceGuideContent","AS 안내"}
                                }
                        }
                    }
                },
                {"originAreaInfo", new Dictionary<string, string>
                    {
                        {"originAreaCode","02"}, //00(국산), 01(원양산), 02(수입산), 03(기타-상세 설명에 표시), 04(기타-직접 입력), 05(원산지 표기 의무 대상 아님)
                        {"importer","수입사"}
                    }
                },
                {"minorPurchasable",true}
            };

            smartstoreChannelProduct = new Dictionary<string, object>
            {
                { "naverShoppingRegistration", false },
                { "channelProductDisplayStatusType", channelProductDisplayStatusType.WAIT }
            };

        }
        public Product(Dictionary<string, object> originProduct, Dictionary<string, object> smartstoreChannelProduct)
        {
            this.originProduct = originProduct;
            this.smartstoreChannelProduct = smartstoreChannelProduct;
            
        }
    }
}




namespace Util
{
    public class Encodings
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

    public class Upload
    {
        
        private static readonly HttpClient client = new HttpClient();

        public enum method
        {
            x_www_form_urlencoded,
            json
        }
        public static async Task<Dictionary<string, string>> postUpload(Dictionary<string, string> parms, string url ,method meth)
        {
            HttpResponseMessage response = null;
            if (meth == method.x_www_form_urlencoded)
            {
                var content = new FormUrlEncodedContent(parms);

                response = await client.PostAsync(url, content);
                
            }
            else if (meth == method.json)
            {
                var json = JsonSerializer.Serialize(parms);
                var content = new StringContent(json,Encoding.UTF8,"application/json");

                response = await client.PostAsync(url, content);
                

            }

            string responseBody = await response!.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);


            return result;
        }
    }
}

namespace Main
{
    class Program
    {
        public static void Main(string[] args)
        {
            Naver.Services.GetToken();
            
            
        }
    }
}

