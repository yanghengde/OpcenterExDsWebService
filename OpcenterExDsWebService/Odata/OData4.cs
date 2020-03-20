using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Siemens.MES.Net.CommonService
{
    public enum DataScope { OpeApp, AppU4DM, Engineering, Documentation, System, Administration}

    public class OData4
    {
        private static HttpClient clientData;

        
        public static string BaseURL //网站url http://localhost/
        {
            get;
            private set;
        }

        public static void Initialize(string baseURL) //初始化
        {
            BaseURL = baseURL;

            //初始化HttpClient
            clientData = new HttpClient();
            clientData.Timeout = new TimeSpan(0, 0, 30);
            clientData.BaseAddress = new Uri(BaseURL);

            clientData.DefaultRequestHeaders.Accept.Clear();
            clientData.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            clientData.DefaultRequestHeaders.Connection.Add("keep-alive"); 
        }

        //调用Command:token,cmdName Command名字，cmdData 传的json串
        public static string SendCmd(string token, string cmdName,string cmdData) //发送命令到uaf，使用默认数据作用域
        {
            return SendCmd(token,cmdName,cmdData, DataScope.AppU4DM);
        }

		
        public static string SendCmd(string token, string cmdName, string cmdData, DataScope cmdScope)
        {
            string startReq="";

            switch (cmdScope)
            {
                case DataScope.OpeApp:
                    startReq = "sit-svc/Application/OperatorApp";
                    break;
                case DataScope.AppU4DM:
                    startReq = "sit-svc/Application/AppU4DM";
                    break;
                case DataScope.Engineering:
                    startReq = "sit-svc/engineering";
                    break;
                case DataScope.Documentation:
                    startReq = "sit-svc/documentation";
                    break;
                case DataScope.System:
                    startReq = "sit-svc/Application/System";
                    break;
                case DataScope.Administration:
                    startReq = "sit-svc/administration";
                    break;
            }

            if (startReq == "")
                return "";

            startReq = startReq + "/odata/" + cmdName;

            clientData.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); 
            var RequestBody = new StringContent(cmdData, Encoding.UTF32, "application/json"); 
            HttpResponseMessage httpResponseMessage = clientData.PostAsync(startReq, RequestBody).Result; 
            return httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

		////调用Command:token,entityName 查询实体，expressionUrla 传的odata查询语法语句
        public static string QueryData(string token, string entityName, string expressionUrl) 
        {
            return QueryData(token, entityName, expressionUrl, DataScope.AppU4DM);
        }

        public static string QueryData(string token, string entityName, string expressionUrl, DataScope queryScope)
        {
            string startReq = "";

            switch (queryScope)
            {
                case DataScope.AppU4DM:
                    startReq = "sit-svc/Application/AppU4DM";
                    break;
                case DataScope.Engineering:
                    startReq = "sit-svc/engineering";
                    break;
                case DataScope.Documentation:
                    startReq = "sit-svc/documentation";
                    break;
                case DataScope.System:
                    startReq = "sit-svc/Application/System";
                    break;
                case DataScope.Administration:
                    startReq = "sit-svc/administration";
                    break;
                case DataScope.OpeApp:
                    startReq = "sit-svc/Application/OperatorApp";
                    break;
            }

            if (startReq == "")
                return "";

            startReq = startReq + "/odata/" + entityName;
            if (expressionUrl != "")
                startReq = startReq + expressionUrl;

            clientData.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); 
            HttpResponseMessage httpResponseMessage = clientData.GetAsync(startReq).Result; 
            return httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
    }
}