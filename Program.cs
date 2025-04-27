using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Text.Json;
using System.Text;


namespace Naver
{
    public class Services
    {
        
        private static string client_id = "2vRbJYYZZseTOaJVcUqQIC";
        private static string client_secret = "$2a$04$QFco5y/AC4lptfNBZoAZW.";
        public static string access_token = "";
        

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

            var result =  await Util.Upload.postUploadwithUrlencoded(parameters, url);
            access_token = result["access_token"].ToString();
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

        public async Task<bool> Upload()
        {
            Dictionary<string, object> body = new Dictionary<string, object>
            {
                { "originProduct", this.originProduct },
                { "smartstoreChannelProduct", smartstoreChannelProduct }
            };
            
            
            var result = await Util.Upload.postUploadwithJson(body, "https://api.commerce.naver.com/external/v2/products");
            if (!result.ContainsKey("code")) return true;
            else return false;
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
        
        
        public static async Task<Dictionary<string, object>> postUploadwithJson(Dictionary<string, object> parms, string url )
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept","application/json;charset=UTF-8");
            client.DefaultRequestHeaders.Add("Authorization","Bearer "+Naver.Services.access_token);
            HttpResponseMessage response = null;
            
            
            var json = JsonSerializer.Serialize(parms);
            var content = new StringContent(json,Encoding.UTF8,"application/json");

            response = await client.PostAsync(url, content);


            string responseBody = await response!.Content.ReadAsStringAsync();
            
            Console.WriteLine(responseBody);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);


            return result;
        }
        
        public static async Task<Dictionary<string, object>> postUploadwithUrlencoded(Dictionary<string, string> parms, string url)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept","application/json");
            //client.DefaultRequestHeaders.Add("Authorization","Bearer "+Naver.Services.access_token);
            HttpResponseMessage response = null;
            var content = new FormUrlEncodedContent(parms);

            response = await client.PostAsync(url, content);
            string responseBody = await response!.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);


            return result;
        }

        public static async Task<string> PostUploadwithMultipartFormData(
            string image_url, string url)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization","Bearer "+Naver.Services.access_token);

            var filePath = "1.png";
            
            var form = new MultipartFormDataContent();

            using var fileStream = File.OpenRead(filePath);

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            
            form.Add(fileContent, "imageFiles",Path.GetFileName(filePath));

            var url_ = "https://api.commerce.naver.com/external/v1/images/upload";
            var response = await client.PostAsync(url, form);
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            
            
            JsonElement root = doc.RootElement;
            if (root.TryGetProperty("images", out JsonElement imagesArray))
            {
                JsonElement firstImage = imagesArray[0];
                string firstImageUrl = firstImage.GetProperty("url").GetString();
                return firstImageUrl;
            }
            else
            {
                Console.WriteLine("images 키가 없습니다! 응답 확인 필요!");
                Console.WriteLine(responseBody);
                return null;  // 또는 throw 예외!
            }
            
        }
    }
}

namespace Main
{
    class Program
    {
        public static async  Task Main(string[] args)
        {
            await Naver.Services.GetToken();

            var image_url = await Util.Upload.PostUploadwithMultipartFormData("1.png",
                "https://api.commerce.naver.com/external/v1/product-images/upload");
            var originProduct = new Dictionary<string, object>()
            {
                { "statusType", Naver.Product.statusType.SALE },
                { "saleType", Naver.Product.saleType.NEW },
                { "leafCategoryId", 0 },
                { "name", "테스트 상품입니다." },
                { "detailContent", "상품 상세 정보" },
                { "image", new Dictionary<string,object> 
                    { 
                        {"representativeImage", new Dictionary<string,string>{{"url",image_url}} },   //url은 api에서 제공받아야 함
                        {"optionalImages", new List<Dictionary<string,string>>
                            {
                            }
                    
                        }
                    } 
                },
                // {"saleStartDate","yyyy-MM-dd'T'HH:mm[:ss][.SSS]XXX"}, //optional
                // {"saleEndDate","yyyy-MM-dd'T'HH:mm[:ss][.SSS]XXX"}, //optional
                {"salePrice",30000},
                {"stockQuantity",500},
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
            
            var smartstoreChannelProduct = new Dictionary<string, object>
            {
                { "naverShoppingRegistration", false },
                { "channelProductDisplayStatusType", Naver.Product.channelProductDisplayStatusType.WAIT }
            };
            
            var ex1 = new Naver.Product(originProduct, smartstoreChannelProduct);
            bool success = await ex1.Upload();
            
            if (success) Console.WriteLine("업로드 성공");
            else Console.WriteLine("업로드 실패!");
            
        }
    }
}

