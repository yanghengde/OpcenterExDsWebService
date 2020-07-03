using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpcenterExDsWebService.Odata;
using Siemens.MES.Net.CommonService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Services;

namespace OpcenterExDsWebService
{
    /// <summary>
    /// Summary description for OpcenterWebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class OpcenterWebService : System.Web.Services.WebService
    {
        private string userName = ConfigurationManager.AppSettings["userName"];
        private string password = ConfigurationManager.AppSettings["password"];
        private string baseURL = ConfigurationManager.AppSettings["baseURL"];
        private string domain = ConfigurationManager.AppSettings["domain"];
        private string prefix = ConfigurationManager.AppSettings["orderPrefix"];

        private BusinessLogic bl = new BusinessLogic();

        #region 获取token信息
        public string GetToken()
        {
            OAuth2Token.Initialize(userName, password, domain, baseURL);
            OData4.Initialize(baseURL);

            //return OAuth2Token.Token;

            return ConfigurationManager.AppSettings["token"];
        }

        public ReturnValue GetAccessToken()
        {
            string token111 = this.GetToken();
           
            ReturnValue ret = new ReturnValue();

            ret.Succeed = true;ret.Result = token111;
            return ret;
        }
        #endregion


        #region PassStation
        [WebMethod]
        public string PassStation(string input)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };
            string serialNumber;
            string terminalId;
            string operationId;
            string userId;
            string type;
            string eventDatetime;
            try
            {
                JObject jb = JObject.Parse(input);
                serialNumber = jb["SerialNumber"].ToString();
                terminalId = jb["TerminalId"].ToString();
                operationId = jb["OperationId"].ToString();
                userId = jb["UserId"].ToString();
                type = jb["Type"].ToString();
                eventDatetime = jb["EventDatetime"].ToString();
            }
            catch (Exception ex)
            {
                rv.Message = string.Format("输入的参数有误，请检查，参考示例：{\"SerialNumber\":\"SN2020000001\",\"TerminalId\":\"SE1L1-NC-MACHINING\",\"OperationId\":\"NC-MACHINING\",\"UserId\":\"hengde\",\"Type\":\"In\"");
                return JsonConvert.SerializeObject(rv);
            }

            DateTime dt = DateTime.Parse(eventDatetime);

            var datetimeoffset = bl.ToDateTimeOffset(dt);
            eventDatetime = datetimeoffset.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }
            string dm_mtu_id;
            ReturnValue checkSn = ValidateSerialNumber(token.Result.ToString(),serialNumber,out dm_mtu_id);
            if (!checkSn.Succeed)
            {
                rv.Message = checkSn.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string WO_ID = checkSn.Result.ToString();

            string jopid = bl.GetOperationIdByOrderAndOperationName(token.Result.ToString(), WO_ID, operationId);
            JArray jaa = JArray.Parse(jopid);
            string op_id;
            if (jaa.Count == 0)
            {
                op_id = null;
            }
            else
            {
                op_id = jaa[0]["Id"].ToString();

            }
            if (string.IsNullOrEmpty(op_id))
            {
                rv.Message = string.Format("输入的工序[{0}]并不在序列号对应的工单中",operationId);
                return JsonConvert.SerializeObject(rv);
            }

            string _Terminal = bl.GetTerminalIdFromWorkOperation(token.Result.ToString(), op_id);
            JArray arr = JArray.Parse(_Terminal);
            if(arr.Count == 0)
            {
                rv.Message = string.Format("工序[{0}]上并没有设置工位，请设置", operationId,terminalId);
                return JsonConvert.SerializeObject(rv);
            }

            string _TerminalId = arr[0]["Name"].ToString();

            if (_TerminalId.ToLower() != terminalId.Trim().ToLower())
            {
                rv.Message = string.Format("工序[{0}]与工位[{1}]不匹配,请重新设置", operationId,terminalId);
                return JsonConvert.SerializeObject(rv);
            }

            string wo = bl.GetWorkOrder(token.Result.ToString(), WO_ID);
            JObject jwo = JObject.Parse(wo);
            string OrderId = jwo["NId"].ToString();

            //start
            string start = bl.Start(token.Result.ToString(), _TerminalId, dm_mtu_id, op_id);

            JObject jstart = JObject.Parse(start);
            int jec = int.Parse(jstart["ErrorCode"].ToString());
            if(jec !=0)
            {
                rv.Succeed = false;
                rv.Message = jstart["ErrorMessage"].ToString();
                return JsonConvert.SerializeObject(rv);
            }

            if (type.ToLower() == "out")
            {
                //complete
                string complete = bl.Complete(token.Result.ToString(), dm_mtu_id, op_id);
                JObject jcomplete = JObject.Parse(complete);
                int jecc = int.Parse(jcomplete["ErrorCode"].ToString());
                if (jecc != 0)
                {
                    rv.Succeed = false;
                    rv.Message = jcomplete["ErrorMessage"].ToString();
                    return JsonConvert.SerializeObject(rv);
                }
            }

            bl.CreateSerialNumberCache(token.Result.ToString(), OrderId, serialNumber, operationId, terminalId, Guid.NewGuid().ToString(), eventDatetime, serialNumber + terminalId,type);

            string json4 = bl.GetOrderOperationProcessQty(token.Result.ToString(), op_id);
            JObject j4 = JObject.Parse(json4);

            string processQty = string.IsNullOrEmpty(j4["ProducedQuantity"].ToString()) ? "0" : j4["ProducedQuantity"].ToString();
            string qty = j4["TargetQuantity"].ToString();
            string rst = "{\"OrderId\":\"" + OrderId + "\",\"Quantity\":" + int.Parse(qty) + ",\"ProcessQuantity\":" + int.Parse(processQty) + "}";
            rv.Succeed = true;
            rv.Result = rst;
            return JsonConvert.SerializeObject(rv);
        }

        public ReturnValue ValidateSerialNumber(string token,string serialNumber,out string sn_id)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };

            rv.Result = bl.GetMaterialTrackingUnit(token, serialNumber);

            JArray array = JArray.Parse(rv.Result.ToString());
            if (array.Count == 0)
            {
                rv.Message = string.Format("序列号[{0}]不存在", serialNumber);
                sn_id = null;
                return rv;
            }

            string MTU_ID = array[0]["Id"].ToString();

            rv.Result = bl.GetDMMaterialTrackingUnit(token, MTU_ID);

            JArray arrayDM = JArray.Parse(rv.Result.ToString());
            if (arrayDM.Count == 0)
            {
                rv.Message = string.Format("序列号[{0}]无对应工单", serialNumber);
                sn_id = null;
                return rv;
            }

            string DM_MTU_ID = arrayDM[0]["Id"].ToString();


            rv.Result = bl.GetWorkOrderID(token, DM_MTU_ID);

            JArray arrayPC = JArray.Parse(rv.Result.ToString());
            if (arrayPC.Count == 0)
            {
                rv.Message = string.Format("序列号[{0}]无对应工单", serialNumber);
                sn_id = null;
                return rv;
            }

            string WorkOrder_ID = arrayPC[0]["WorkOrder_Id"].ToString();

            rv.Result = WorkOrder_ID;

            string result = bl.GetWorkOrder(token, WorkOrder_ID);
            JObject jbw = JObject.Parse(result);
            string orderStatus = jbw["Status"]["StatusNId"].ToString();

            if (orderStatus == "Edit")
            {
                rv.Message = string.Format("序列号[{0}]对应的工单是Edit状态，请Release后执行", serialNumber);
                sn_id = null;
                return rv;
            }
            sn_id = DM_MTU_ID;
            rv.Succeed = true;
            return rv;
        }

        #endregion

        #region OrderOnLine
        //{"OrderId":"WO_2020_0001","TerminalId":"SE1L1-NC-MACHINING","StartTime":"2020-03-15 18:00:00"}
        [WebMethod]
        public string OnlineOrder(string input)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };
            string OrderId;
            string terminalId;
            string startTime;
            try {
                JObject jb = JObject.Parse(input);
                OrderId = jb["OrderId"].ToString();
                terminalId = jb["TerminalId"].ToString();
                startTime = DateTimeOffset.Parse(jb["StartTime"].ToString()).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
            catch (Exception ex)
            {
                rv.Message = string.Format("输入的参数有误，请检查，参考示例：{\"OrderId\":\"WO_2020_0001\",\"TerminalId\":\"SE1L1 - NC - MACHINING\",\"StartTime\":\"2020 - 03 - 15 18:00:00\"}");
                return JsonConvert.SerializeObject(rv);
            }

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string first_op_id;

            ReturnValue CheckOrder = this.CheckWorkOrder(token.Result.ToString(), OrderId, terminalId,out first_op_id);
            if (!CheckOrder.Succeed)
            {
                rv.Message = CheckOrder.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string result = bl.GetWorkOrder(token.Result.ToString(), CheckOrder.Result.ToString());
            JObject jbw = JObject.Parse(result);
            string orderStatus = jbw["Status"]["StatusNId"].ToString();

            if (orderStatus == "Release")
            {
                string lineId1 = bl.GetLineIdByUnit(token.Result.ToString(), terminalId);
                if (string.IsNullOrEmpty(lineId1))
                {
                    rv.Message = string.Format("输入的工位[{0}]没有关联产线信息，请配置", terminalId);
                    return JsonConvert.SerializeObject(rv);
                }

                string json21 = bl.CreateOrderCache(token.Result.ToString(), OrderId, CheckOrder.Result.ToString(), lineId1);
                rv.Succeed = true;
                return JsonConvert.SerializeObject(rv);
            }

            if (orderStatus == "Edit" || orderStatus == "ReadyForScheduling")
            {
                string json = bl.ReleaseOrder(token.Result.ToString(), CheckOrder.Result.ToString(), OrderId);
                JObject jb1 = JObject.Parse(json);
                int errorcode = int.Parse(jb1["ErrorCode"].ToString());
                if (errorcode != 0)
                {
                    rv.Message = jb1["ErrorMessage"].ToString();
                    return JsonConvert.SerializeObject(rv);
                }
            }
            string lineId = bl.GetLineIdByUnit(token.Result.ToString(), terminalId);
            if (string.IsNullOrEmpty(lineId))
            {
                rv.Message = string.Format("输入的工位[{0}]没有关联产线信息，请配置", terminalId);
                return JsonConvert.SerializeObject(rv);
            }

            string json2 = bl.CreateOrderCache(token.Result.ToString(), OrderId, CheckOrder.Result.ToString(), lineId);

            string json4 = bl.GetOrderOperationProcessQty(token.Result.ToString(), first_op_id);
            JObject j4 = JObject.Parse(json4);

            string processQty = string.IsNullOrEmpty(j4["ProducedQuantity"].ToString()) ? "0" : j4["ProducedQuantity"].ToString();
            string qty = j4["TargetQuantity"].ToString();
            string rst = "{\"OrderId\":\""+OrderId+"\",\"Quantity\":"+int.Parse(qty)+",\"ProcessQuantity\":"+int.Parse(processQty)+"}";

            rv.Succeed = true;
            rv.Result = rst;
            return JsonConvert.SerializeObject(rv);
        }

        private ReturnValue CheckWorkOrder(string token,string OrderId,string TerminalId,out string wo_op_id)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };
            try
            {
                string result = bl.GetWorkOrderByOrderId(token, OrderId);
                if (string.IsNullOrEmpty(result) || result == "[]")
                {
                    rv.Message = string.Format("输入的工单[{0}]在系统中不存在",OrderId);
                    wo_op_id = null;
                    return rv;
                }
                JArray jbw = JArray.Parse(result);
                string Id = jbw[0]["Id"].ToString();

                string firstOpe = bl.GetOrderFirstOperationId(token, Id);
                wo_op_id = firstOpe;
                if (string.IsNullOrEmpty(firstOpe))
                {
                    rv.Message = string.Format("输入的工单[{0}]在系统中找不到其对应的工序", OrderId);
                    return rv;
                }

                string _Terminal = bl.GetTerminalIdFromWorkOperation(token, firstOpe);
                JArray arr = JArray.Parse(_Terminal);
                if (arr.Count == 0)
                {
                    rv.Message = string.Format("输入的工单[{0}]的首工序没有找到对应的工位信息，请设置", OrderId);
                    return rv;
                }

                string _TerminalId = arr[0]["Name"].ToString();

                if (_TerminalId.ToLower() != TerminalId.Trim().ToLower())
                {
                    rv.Message = string.Format("输入的工单[{0}]的首工位为[{1}],请在此位置上线", OrderId,_TerminalId);
                    return rv;
                }


                rv.Result = Id;
                rv.Succeed = true;
                return rv;

            }
            catch (Exception ex)
            {
                rv.Succeed = false;
                rv.Message = ex.Message;
                wo_op_id = null;
                return rv;
            }
        }

        #endregion

        #region GetSerialNumberInfo
        [WebMethod]
        public string GetSerialNumberInfo(string input)
        {
            ReturnValueBom rv = new ReturnValueBom
            {
                Succeed = false
            };
            string serialNumber;
            string operationId;
            try
            {
                JObject jb = JObject.Parse(input);
                serialNumber = jb["SerialNumber"].ToString();
                operationId = jb["OperationId"].ToString();
            }
            catch (Exception ex)
            {
                rv.Message = string.Format("输入的参数有误，请检查，参考示例：{\"SerialNumber\":\"SN0000001\",\"OperationId\":\"NC-MACHINING\"}");
                return JsonConvert.SerializeObject(rv);
            }

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string dm_mtu_id;
            ReturnValue checkSn = ValidateSerialNumber(token.Result.ToString(), serialNumber, out dm_mtu_id);
            if (!checkSn.Succeed)
            {
                rv.Message = checkSn.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string WO_ID = checkSn.Result.ToString();

            string jopid = bl.GetOperationIdByOrderAndOperationName(token.Result.ToString(), WO_ID, operationId);
            JArray jaa = JArray.Parse(jopid);
            string op_id;
            if (jaa.Count == 0)
            {
                op_id = null;
            }
            else
            {
                op_id = jaa[0]["Id"].ToString();

            }
            if (string.IsNullOrEmpty(op_id))
            {
                rv.Message = string.Format("输入的工序[{0}]并不在序列号对应的工单中", operationId);
                return JsonConvert.SerializeObject(rv);
            }

            string arr = bl.GetAssemblyMaterials(token.Result.ToString(), op_id);
            JArray ja = JArray.Parse(arr);
            if(ja.Count == 0)
            {
                rv.Message = string.Format("序列号[{0}]对应的工序[{1}]无组装物料", serialNumber ,operationId);
                return JsonConvert.SerializeObject(rv);
            }

            //if(ja.Count != 1)
            //{
            //    rv.Message = string.Format("当前的模型只支持组装1颗物料，请在Process中设置", serialNumber, operationId);
            //    return rv;
            //}

            rv.Boms = new List<string>();
            for (int i = 0 ;i < ja.Count;i++)
            {
                rv.Boms.Add(ja[i]["NId"].ToString());
            }

            rv.Succeed = true;

            return JsonConvert.SerializeObject(rv);
        }

        #endregion

        #region AssembleSerialNumber
        [WebMethod]
        public string AssembleSerialNumber(string input)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };
            string serialNumber;
            string subSerialNumber;
            string terminalId;
            string operationId;
            string userId;
            try
            {
                JObject jb = JObject.Parse(input);
                serialNumber = jb["SerialNumber"].ToString();
                subSerialNumber = jb["SubSerialNumber"].ToString();
                terminalId = jb["TerminalId"].ToString();
                operationId = jb["OperationId"].ToString();
                userId = jb["UserId"].ToString();
            }
            catch (Exception ex)
            {
                rv.Message = string.Format("输入的参数有误，请检查，参考示例：{\"SerialNumber\":\"SN2020000001\",\"SubSerialNumber\":\"ABCD-0002\",\"TerminalId\":\"SE1L1-NC-MACHINING\",\"OperationId\":\"NC-MACHINING\",\"UserId\":\"hengde\"}");
                return JsonConvert.SerializeObject(rv);
            }


            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }
            string dm_mtu_id;
            ReturnValue checkSn = ValidateSerialNumber(token.Result.ToString(), serialNumber, out dm_mtu_id);
            if (!checkSn.Succeed)
            {
                rv.Message = checkSn.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string WO_ID = checkSn.Result.ToString();

            string jopid = bl.GetOperationIdByOrderAndOperationName(token.Result.ToString(), WO_ID, operationId);
            JArray jaa = JArray.Parse(jopid);
            string op_id;string nopid;
            if (jaa.Count == 0)
            {
                op_id = null;
                nopid = null;
            }
            else
            {
                op_id = jaa[0]["Id"].ToString();
                nopid = jaa[0]["NId"].ToString();
            }

            if (string.IsNullOrEmpty(op_id))
            {
                rv.Message = string.Format("输入的工序[{0}]并不在序列号对应的工单中", operationId);
                return JsonConvert.SerializeObject(rv);
            }

            string _Terminal = bl.GetTerminalIdFromWorkOperation(token.Result.ToString(), op_id);
            JArray arr = JArray.Parse(_Terminal);
            if (arr.Count == 0)
            {
                rv.Message = string.Format("工序[{0}]上并没有设置工位，请设置", operationId, terminalId);
                return JsonConvert.SerializeObject(rv);
            }

            string _TerminalId = arr[0]["Name"].ToString();

            if (_TerminalId.ToLower() != terminalId.Trim().ToLower())
            {
                rv.Message = string.Format("工序[{0}]与工位[{1}]不匹配,请重新设置", operationId, terminalId);
                return JsonConvert.SerializeObject(rv);
            }

            string jm = bl.GetMaterialTrackingUnit(token.Result.ToString(), serialNumber);

            JArray array = JArray.Parse(jm);
            if (array.Count == 0)
            {
                rv.Message = string.Format("序列号[{0}]不存在", serialNumber);
                return JsonConvert.SerializeObject(rv);
            }

            string prd_id = array[0]["MaterialNId"].ToString();

            string mat_ids = bl.GetAssemblyMaterials(token.Result.ToString(), op_id);

            JArray ja = JArray.Parse(mat_ids);
            if(ja.Count == 0)
            {
                rv.Message = string.Format("序列号[{0}]在工序[{1}]下没有关键件信息，无法执行组装", serialNumber,operationId);
                return JsonConvert.SerializeObject(rv);
            }
            string mat_id = ja[0]["NId"].ToString();

            //assembly
            string json = bl.UADMConsumeMaterialItemList(token.Result.ToString(), op_id, nopid, serialNumber, prd_id, mat_id, subSerialNumber);
            JObject jb1 = JObject.Parse(json);
            int errorcode = int.Parse(jb1["ErrorCode"].ToString());
            if (errorcode != 0)
            {
                rv.Message = jb1["ErrorMessage"].ToString();
                return JsonConvert.SerializeObject(rv);
            }

            rv.Succeed = true;
            return JsonConvert.SerializeObject(rv);
        }

        #endregion

        #region GetWorkOrders
        [WebMethod]
        public string GetWorkOrders()
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string wojson = bl.GetPsOrders(token.Result.ToString(), prefix);
            JArray array = JArray.Parse(wojson);
            if(array.Count == 0)
            {
                rv.Message = "未找到可执行的工单";
                return JsonConvert.SerializeObject(rv);
            }

            IList<OrderInfo> list = new List<OrderInfo>();
            for(int j = 0; j < array.Count; j++)
            {
                string wo_id = array[j]["Id"].ToString();
                string snjson = bl.GetSerialNumberByWOId(token.Result.ToString(), wo_id);
                JArray snarray = JArray.Parse(snjson);
                if (snarray.Count == 0) continue;

                IList<string> snlist = new List<string>();
                for(int k = 0; k < snarray.Count; k++)
                {
                    snlist.Add(snarray[k]["DM_MaterialTrackingUnit"]["MaterialTrackingUnit"]["NId"].ToString());
                }
                OrderInfo info = new OrderInfo()
                {
                    OrderId = array[j]["NId"].ToString(),
                    FinalMaterialId = array[j]["FinalMaterial"]["Material"]["NId"].ToString(),
                    Quantity = int.Parse(array[j]["InitialQuantity"].ToString()),
                    Sequence = array[j]["NId"].ToString(),
                    SerialNumbers = snlist
                };
                list.Add(info);
            }

            if(list.Count == 0)
            {
                rv.Message = "未找到可使用的工单, 或者序列号未分配";
                return JsonConvert.SerializeObject(rv);
            }

            rv.Result = list;
            rv.Succeed = true;
            return JsonConvert.SerializeObject(rv);
        }
        #endregion

        #region CreateOrder
        [WebMethod]
        public string CreateOrder(string input)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };

            string orderId;
            int quantity;
            string materialId;
            string color;
            string dueDate;
            try
            {
                JObject jb = JObject.Parse(input);
                orderId = jb["OrderId"].ToString();
                quantity = int.Parse(jb["Quantity"].ToString());
                materialId =jb["MaterialId"].ToString();
                color = jb["Color"].ToString();
                dueDate = DateTimeOffset.Parse(jb["DueDate"].ToString()).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
            catch (Exception ex)
            {
                rv.Message = string.Format("输入的参数有误，请检查，参考示例：{\"OrderId\":\"WO-00000006\",\"Quantity\":10,\"MaterialId\":\"A1003860348\",\"DueDate\":\"2020-03-19 18:00:00\"}");
                return JsonConvert.SerializeObject(rv);
            }

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string json = bl.GetProcessByFinalMaterialId(token.Result.ToString(), materialId);
            JArray array = JArray.Parse(json);
            if(array.Count == 0)
            {
                rv.Message = string.Format("产品[{0}]未找到其对应的工艺路线",materialId);
                return JsonConvert.SerializeObject(rv);
            }

            string processId = array[0]["Id"].ToString();
            JArray asplans = JArray.Parse(array[0]["AsPlannedBOP"].ToString());
            string asplantid = asplans[0]["Id"].ToString();
            string plant = array[0]["Plant"].ToString();
            string finalMaterialID = array[0]["FinalMaterialId_Id"].ToString();

            string createJson = bl.CreateOrder(token.Result.ToString(), orderId, processId, quantity, asplantid,finalMaterialID, plant, dueDate,color);

            JObject jb0 = JObject.Parse(createJson);
            JToken res_value;
            jb0.TryGetValue("Error", out res_value);

            JObject jb1 = JObject.Parse(res_value.ToString());
            int errorcode = int.Parse(jb1["ErrorCode"].ToString());
            if (errorcode != 0)
            {
                rv.Message = jb1["ErrorMessage"].ToString();
                return JsonConvert.SerializeObject(rv);
            }

            string wo_id = jb0["Id"].ToString();

            string jsns = bl.CreateOrderSerialNumbers(token.Result.ToString(), wo_id, orderId);
            JObject jb2 = JObject.Parse(jsns);
            int errorcode1 = int.Parse(jb2["ErrorCode"].ToString());
            if (errorcode1 != 0)
            {
                rv.Message = jb2["ErrorMessage"].ToString();
                return JsonConvert.SerializeObject(rv);
            }

            string jsonsch = bl.UADMSetWorkOrderForScheduling(token.Result.ToString(), wo_id, userName);
            JObject jb3 = JObject.Parse(jsns);
            int errorcode2 = int.Parse(jb3["ErrorCode"].ToString());
            if (errorcode2 != 0)
            {
                rv.Message = jb3["ErrorMessage"].ToString();
                return JsonConvert.SerializeObject(rv);
            }

            rv.Succeed = true;

            return JsonConvert.SerializeObject(rv);
        }
        #endregion

        #region GetOrderOperationProgress
        [WebMethod]
        public string GetOrderOperationProgress(string input)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }

           string json = bl.GetOrderInformation(token.Result.ToString(),input);
            JArray array = JArray.Parse(json);
            if(array.Count == 0)
            {
                rv.Message = "未查询到工单相关的信息";
                return JsonConvert.SerializeObject(rv);
            }

            IList<WorkOrderOperation> list = new List<WorkOrderOperation>();
            for (int j = 0; j < array.Count; j++)
            {
                WorkOrderOperation wo = new WorkOrderOperation
                {
                    OperationDescription = array[j]["Description"].ToString(),
                    OperationName = array[j]["Name"].ToString(),
                    OrderId = array[j]["WorkOrder"]["NId"].ToString(),
                    ProducedQuantity = int.Parse(string.IsNullOrEmpty(array[j]["ProducedQuantity"].ToString()) ? "0" : array[j]["ProducedQuantity"].ToString()),
                    Sequence = array[j]["Sequence"].ToString(),
                    Quantity = int.Parse(array[j]["TargetQuantity"].ToString())
                };
                list.Add(wo);
            }

            rv.Result = list;

            rv.Succeed = true;

            return JsonConvert.SerializeObject(rv);
        }
        #endregion

        #region GetOrderProgress
        [WebMethod]
        public string GetOrderProgress(string input)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string json = bl.GetOrderProgress(token.Result.ToString(), input);
            JArray array = JArray.Parse(json);
            if (array.Count == 0)
            {
                rv.Message = "未查询到工单相关的信息";
                return JsonConvert.SerializeObject(rv);
            }

            IList<WorkOrderProgress> list = new List<WorkOrderProgress>();
            for (int j = 0; j < array.Count; j++)
            {
                WorkOrderProgress wo = new WorkOrderProgress
                {
                    OrderId = array[j]["WorkOrder"]["NId"].ToString(),
                    ProducedQuantity = int.Parse(string.IsNullOrEmpty(array[j]["ProducedQuantity"].ToString()) ? "0" : array[j]["ProducedQuantity"].ToString()),
                    Quantity = int.Parse(array[j]["TargetQuantity"].ToString())
                };
                list.Add(wo);
            }

            rv.Result = list;

            rv.Succeed = true;

            return JsonConvert.SerializeObject(rv);
        }
        #endregion

        [WebMethod]
        public string GetMaxOrderId(string prekey)
        {
            ReturnValue rv = new ReturnValue
            {
                Succeed = false
            };

            ReturnValue token = this.GetAccessToken();
            if (!token.Succeed)
            {
                rv.Message = token.Message;
                return JsonConvert.SerializeObject(rv);
            }

            string mxoid = bl.GetMaxOrderId(token.Result.ToString(),prekey);
            rv.Succeed = true;
            rv.Result = mxoid;

            return JsonConvert.SerializeObject(rv);
        }
    }
}
