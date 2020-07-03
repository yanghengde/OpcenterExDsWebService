
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Timers;

namespace Siemens.MES.Net.CommonService
{
    public class OAuth2Token
    {
        private static HttpClient clientAuth;
        private static Timer tokenTimer; 
        private static bool isTokenTimerExpired;
        private const string AuthRelativeURL = "sit-auth/OAuth/Token"; 
        public static string BaseURL //网站url， http://localhost/
        {
            get;
            private set;
        }

        public static string Domain //域名，没有域就用机器名
        {
            get;
            private set;
        }

        public static string UserName
        {
            get;
            private set;
        }

        public static string Password
        {
            get;
            private set;
        }

        private static string token;
        public static string Token //
        {
            get {
                if (isTokenTimerExpired) //
                {
                    GetTokenByRefreshToken();
                    return token;
                }
                else
                    return token;
            }
        }
        public static string RefreshToken //
        {
            get;
            private set;
        }
        public static  int expiresInSeconds //
        {
            get;
            private set;
        }

        public static void Initialize(string userName, string password, string domain, string baseURL) //初始化
        {
            UserName = userName;
            Password = password;
            Domain = domain;
            BaseURL = baseURL;

            //初始化HttpClient
            clientAuth = new HttpClient();
            clientAuth.Timeout = new TimeSpan(0, 0, 30);
            clientAuth.BaseAddress = new Uri(BaseURL);

            clientAuth.DefaultRequestHeaders.Accept.Clear();
            clientAuth.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            clientAuth.DefaultRequestHeaders.Connection.Add("keep-alive"); 
            clientAuth.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "clientID", "clientPsw"))));

            GetTokenFromRequest(); 
        }

        private static void GetTokenFromRequest() 
        {
            var fields = new Dictionary<string, string>
            {  { "grant_type",  "password" }, 
                { "username", string.Format("{0}",UserName) }, 
                { "password", Password },
                { "scope", "global" }}; 
            FormUrlEncodedContent fc = new FormUrlEncodedContent(fields);
            //HttpResponseMessage response = clientAuth.PostAsync(AuthRelativeURL, fc).Result;

            //string result = response.Content.ReadAsStringAsync().Result;
            //JObject parsedJson = JObject.Parse(result);
            token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjFGMjlCMDNEOEMwRjYxNzNEMzQ5MkYwMEJEMTI4QzQ5QjA3MkRBNUYiLCJ4NXQiOiJIeW13UFl3UFlYUFRTUzhBdlJLTVNiQnkybDgiLCJ0eXAiOiJKV1QifQ.eyJ1bmlxdWVfbmFtZSI6IkNOPUZhd1Rva2VuQ2VydCwgVD1TaW1hdGljSVQgVUEgRm91bmRhdGlvbiIsImNlcnR0aHVtYnByaW50IjoiMUYyOUIwM0Q4QzBGNjE3M0QzNDkyRjAwQkQxMjhDNDlCMDcyREE1RiIsInVybjpyZWFsbSI6InguNTA5IiwibmJmIjoxNTkxODU2MTU3LCJleHAiOjE1OTk4MDQ5NTcsImlhdCI6MTU5MTg1NjE1NywiaXNzIjoidXJuOnVuaWZpZWRvYXV0aCIsImF1ZCI6InVybjp1bmlmaWVkIn0.tY-UtqmjzXwt3qYvqBwDxeo73b0aDfSH3VtKyfrjbuFUcY2MQ7DAweIbRsFwbQc00-M9SGgcuyLFGXvcoCN9IMoGdAmiGYtlBvVEl_asWVEMlomCBjdN0BKSPUKmfiPC27rlCZ4WDQUvk4ygUiFpcwkFL499O92VZyrS62gyaV7wbuDLmnK7-IBkLLdryOo75K5vn-a7fWoZbB7mouu2E96827_ml6olUXNdm_bJAlmlKhC7DdU3k-w2Y72jyNBF4OrWMQpf5VqLmYND8xIbP89_x2Nk1bib9eqAaEXwoU1K76FRXopvEY349CquFr63pu2p05xT7vqE_KBtMqvAIg";
            //RefreshToken = parsedJson["refresh_token"].ToString();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            tokenTimer.Stop();
            isTokenTimerExpired = true;
        }

        public static void GetTokenByRefreshToken() 
        {
            var fields = new Dictionary<string, string>
            {  { "grant_type",  "refresh_token" }, 
                { "refresh_token", RefreshToken }}; 
            FormUrlEncodedContent fc = new FormUrlEncodedContent(fields);
            HttpResponseMessage response = clientAuth.PostAsync(AuthRelativeURL, fc).Result;

            string result = response.Content.ReadAsStringAsync().Result;
            JObject parsedJson = JObject.Parse(result);
            if (parsedJson["access_token"] != null) 
            {
                token = parsedJson["access_token"].ToString();
                RefreshToken = parsedJson["refresh_token"].ToString();
                expiresInSeconds = parsedJson["expires_in"].Value<int>();

                tokenTimer.Interval = (expiresInSeconds - 60) * 1000;//设置超时前60秒时触发事件
                tokenTimer.Start();
                isTokenTimerExpired = false;
            }
            else 
                GetTokenFromRequest();
        }
    }
}
