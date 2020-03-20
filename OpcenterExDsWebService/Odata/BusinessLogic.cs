using Newtonsoft.Json.Linq;
using Siemens.MES.Net.CommonService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpcenterExDsWebService.Odata
{
    public class BusinessLogic
    {
        //http://opds31/sit-svc/Application/AppU4DM/odata/MaterialTrackingUnit?$filter=NId eq 'SN2020000001'&$select=Id,NId
        public string GetMaterialTrackingUnit(string token,string serialnumber)
        {
            string json = OData4.QueryData(token, "MaterialTrackingUnit", "?$filter=NId eq '" + serialnumber + "'&$select=Id,NId,MaterialNId");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if(jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/DM_MaterialTrackingUnit?$filter=MaterialTrackingUnit_Id eq b0db828b-f58f-4a2d-b2f5-83689f860b4b&$select=Id
        public string GetDMMaterialTrackingUnit(string token, string MTU_ID)
        {
            string json = OData4.QueryData(token, "DM_MaterialTrackingUnit", "?$filter=MaterialTrackingUnit_Id eq "+MTU_ID+"&$select=Id");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/ProducedMaterialItem?$filter=DM_MaterialTrackingUnit_Id eq 7e4cdd14-6aa3-46a3-8875-562a3dc95752&$select=Id,WorkOrder_Id
        public string GetWorkOrderID(string token, string DM_MTU_ID)
        {
            string json = OData4.QueryData(token, "ProducedMaterialItem", "?$filter=DM_MaterialTrackingUnit_Id eq "+DM_MTU_ID+"&$select=Id,WorkOrder_Id");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/WorkOrder/dae7d1bd-5219-49d6-93d2-3e77b4ccdb68?$select=Id,Nid
        public string GetWorkOrder(string token, string WO_ID)
        {
            string json = OData4.QueryData(token, "WorkOrder", "/"+WO_ID+ "?$select=Id,Nid,Status");
         
            return json;
        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/WorkOrder?$filter=NId eq 'WO-2020-0001'&$select=Id
        public string GetWorkOrderByOrderId(string token, string stringOrderId)
        {
            string json = OData4.QueryData(token, "WorkOrder", "?$filter=NId eq '"+ stringOrderId + "'&$select=Id");

            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/UADMReleaseWorkOrder
        //{"command":{"ReleaseWorkOrderDetails":[{"WorkOrderId":"51dc60ee-60ca-47a1-9506-de910625bb2d","WorkOrderNId":"WO-2020-0002"}]}}
        //{"@odata.context":"http://opds31/sit-svc/application/AppU4DM/odata/$metadata#Siemens.SimaticIT.U4DM.AppU4DM.AppU4DM.DMPOMModel.Commands.UADMReleaseWorkOrderResponse","Succeeded":false,"Id":[],"Error":{"ErrorCode":-702597,"ErrorMessage":"The work order(s) WO-2020-0002 has a serialized production type. There are not all produced material items associated: Select serial number to associate."},"SitUafExecutionDetail":null}
        public string ReleaseOrder(string token,string WO_ID, string OrderID)
        {
            string json = OData4.SendCmd(token, "UADMReleaseWorkOrder", "{\"command\":{\"ReleaseWorkOrderDetails\":[{\"WorkOrderId\":\""+WO_ID+"\",\"WorkOrderNId\":\""+OrderID+"\"}]}}");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/ItlkCheckWOOpAssociation?$filter=WorkOrderOperation_Id eq 322db10f-cc19-4a80-80b0-e61d19155630&$select=Id,WorkOrderOperation_Id&$top=0&$count=true
        public string ItlkCheckWOOpAssociation(string token, string WO_ID, string OrderID)
        {
            string json = OData4.QueryData(token, "ItlkCheckWOOpAssociation", "?$filter=WorkOrderOperation_Id eq "+WO_ID+"&$select=Id,WorkOrderOperation_Id&$top=0&$count=true");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //通过工单工艺获取unit的信息
        //http://opds31/sit-svc/Application/AppU4DM/odata/UADMGetFullEquipment(function=@x)?@x={"OperationId":"322db10f-cc19-4a80-80b0-e61d19155630"}&$top=10&$skip=0&$orderby=Preferred asc&$count=true
        public string GetTerminalIdFromWorkOperation(string token,string OP_ID)
        {
            string json = OData4.QueryData(token, "UADMGetFullEquipment", "(function=@x)?@x={\"OperationId\":\""+OP_ID+"\"}&$top=10&$skip=0&$orderby=Preferred asc&$count=true");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //获取工单下所有Operation的信息，相当于Entry
        //http://opds31/sit-svc/Application/AppU4DM/odata/WorkOrderOperation?$filter=WorkOrder_Id eq dae7d1bd-5219-49d6-93d2-3e77b4ccdb68&$orderby=Sequence asc&$select=Id
        public string GetOrderFirstOperationId(string token,string WO_ID)
        {
            string json = OData4.QueryData(token, "WorkOrderOperation", "?$filter=WorkOrder_Id eq "+WO_ID+"&$orderby=Sequence asc&$select=Id");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                JArray jaa = JArray.Parse(res_value.ToString());
                string woOperationId;
                if (jaa.Count == 0)
                {
                    woOperationId = null;
                }
                else
                {
                    woOperationId = jaa[0]["Id"].ToString();

                }
                return woOperationId;
            }
            return null;
           
        }

        //通过工单和工艺取得工序的Id
        //http://opds31/sit-svc/Application/AppU4DM/odata/WorkOrderOperation?$filter=WorkOrder_Id eq dae7d1bd-5219-49d6-93d2-3e77b4ccdb68 and Name eq 'NC-MACHINING'&$orderby=Sequence asc&$select=Id,NId
        public string GetOperationIdByOrderAndOperationName(string token, string WO_ID,string operationName)
        {
            string json = OData4.QueryData(token, "WorkOrderOperation", "?$filter=WorkOrder_Id eq "+WO_ID+" and Name eq '"+ operationName + "'&$orderby=Sequence asc&$select=Id,NId");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
               
                return res_value.ToString();
            }
            return null;

        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/CreateOrderCache
        //{"command":{"ReleaseWorkOrderDetails":[{"WorkOrderId":"51dc60ee-60ca-47a1-9506-de910625bb2d","WorkOrderNId":"WO-2020-0002"}]}}
        //{"@odata.context":"http://opds31/sit-svc/application/AppU4DM/odata/$metadata#Siemens.SimaticIT.U4DM.AppU4DM.AppU4DM.DMPOMModel.Commands.UADMReleaseWorkOrderResponse","Succeeded":false,"Id":[],"Error":{"ErrorCode":-702597,"ErrorMessage":"The work order(s) WO-2020-0002 has a serialized production type. There are not all produced material items associated: Select serial number to associate."},"SitUafExecutionDetail":null}
        public string CreateOrderCache(string token, string orderId,string WO_ID,string lineId)
        {
            string json = OData4.SendCmd(token, "CreateOrderCache", "{\"command\":{\"OrderID\":\""+orderId+"\",LineId:\""+lineId+"\",\"WoId\":\""+WO_ID+"\",Status:1}}",DataScope.OpeApp);
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //通过Unit的信息获取LineId的信息值
        //http://opds31/sit-svc/Application/Equipment/odata/EquipmentGraphLink?$expand=Source($select=EquipmentNId),Destination($select=EquipmentNId)&$filter=Destination/EquipmentNId eq 'SE1L1-NC-MACHINING'&$select=Id
        public string GetLineIdByUnit(string token, string unit)
        {
            string json = OData4.QueryData(token, "EquipmentGraphLink", "?$expand=Source($select=EquipmentNId),Destination($select=EquipmentNId)&$filter=Destination/EquipmentNId eq '"+unit+"'&$select=Id");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                JArray jaa = JArray.Parse(res_value.ToString());
                string woOperationId;
                if (jaa.Count == 0)
                {
                    woOperationId = null;
                }
                else
                {
                    woOperationId = jaa[0]["Source"]["EquipmentNId"].ToString();

                }
                return woOperationId;
            }
            return null;

        }

        //序列号Start 方法
        //http://opds31/sit-svc/Application/AppU4DM/odata/UADMStartWOOperationSerializedList
        //{"command":{"StartWOOperationSerializedParameterTypeList":[{"EquipmentNId":"SE1L1-NC-MACHINING","DM_MTUIdList":["7e4cdd14-6aa3-46a3-8875-562a3dc95752"],"WorkOrderOperationId":"322db10f-cc19-4a80-80b0-e61d19155630","SetPointItemParameterType":[]}]}}
        public string Start(string token,string EquipmentNId,string sn_id,string op_id)
        {
            string json = OData4.SendCmd(token, "UADMStartWOOperationSerializedList", "{\"command\":{\"StartWOOperationSerializedParameterTypeList\":[{\"EquipmentNId\":\""+EquipmentNId+"\",\"DM_MTUIdList\":[\""+sn_id+"\"],\"WorkOrderOperationId\":\""+op_id+"\",\"SetPointItemParameterType\":[]}]}}");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //	http://opds31/sit-svc/Application/AppU4DM/odata/UADMCompleteWOOperationSerializedList
        //{"command":{"CompleteSerializedWoOpParameterList":[{"WorkOrderOperationId":"322db10f-cc19-4a80-80b0-e61d19155630","DM_MTUIdList":["7e4cdd14-6aa3-46a3-8875-562a3dc95752"]}]}}
        public string Complete(string token, string sn_id, string op_id)
        {
            string json = OData4.SendCmd(token, "UADMCompleteWOOperationSerializedList", "{\"command\":{\"CompleteSerializedWoOpParameterList\":[{\"WorkOrderOperationId\":\""+op_id+"\",\"DM_MTUIdList\":[\""+sn_id+"\"]}]}}");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //http://siemensopds31/sit-svc/Application/AppU4DM/odata/SetPointItem?$filter= NId eq 'RecordTime'&$select=Id,SetPoint_Id
        public string GetSetPointItem(string token,string item)
        {
            string json = OData4.QueryData(token, "SetPointItem", "?$filter= NId eq '"+item+"'&$select=Id,SetPoint_Id");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {

                return res_value.ToString();
            }
            return null;
        }
        //http://opds31/sit-svc/Application/AppU4DM/odata/WorkOrderOperation/322db10f-cc19-4a80-80b0-e61d19155630?$select=ProducedQuantity,TargetQuantity
        //{
        //"@odata.context": "http://opds31/sit-svc/application/AppU4DM/odata/$metadata#WorkOrderOperation(ProducedQuantity,TargetQuantity)/$entity",
        //"ProducedQuantity": 4.000,
        //"TargetQuantity": 10.000}
        public string GetOrderOperationProcessQty(string token,string wo_op_id)
        {
            string json = OData4.QueryData(token, "WorkOrderOperation", "/"+wo_op_id+"?$select=ProducedQuantity,TargetQuantity");
            
            return json;
        }

        //http://opds31/sit-svc/Application/AppU4DM/odata/ToBeConsumedMaterial?&$orderby=Sequence%20asc&$count=true&$filter=WorkOrderOperation_Id eq 9c1c15ed-5634-4eeb-9f8e-e40101904b5d&$select=NId
        public string GetAssemblyMaterials(string token, string wo_op_id)
        {
            string json = OData4.QueryData(token, "ToBeConsumedMaterial", "?&$orderby=Sequence%20asc&$count=true&$filter=WorkOrderOperation_Id eq "+wo_op_id+"&$select=NId");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //	http://siemensopds31/sit-svc/Application/AppU4DM/odata/UADMConsumeMaterialItemList
        //{"command":{"WorkOrderOperationId":"5ec00405-b870-4fce-a3ab-0533f902f20b","WorkOrderOperationNId":"WOO_268bf50e-63b7-4ad4-92b7-41fa74b0da85","Notes":"notes","MaterialItemToConsumeList":[{"TargetMaterialItem":{"NId":"SN2020000001","SerialNumber":"SN2020000001","MaterialDefinitionNId":"A1003860348"},"ToBeConsumedMaterialItemList":[{"ToBeConsumedMaterialName":"B3087658","ToBeConsumedMaterialSequence":1,"MaterialDefinitionNId":"B3087658","SerialNumber":"ABCD-0001","TotalQuantity":1,"ActualQuantity":0,"Quantity":1}]}]}}
        public string UADMConsumeMaterialItemList(string token,string wo_op_id,string wo_op_nid,string tsn,string prd_id,string mat_id,string sub_sn)
        {
            string json = OData4.SendCmd(token, "UADMConsumeMaterialItemList", "{\"command\":{\"WorkOrderOperationId\":\""+wo_op_id+"\",\"WorkOrderOperationNId\":\""+wo_op_nid+"\",\"Notes\":\"notes\",\"MaterialItemToConsumeList\":[{\"TargetMaterialItem\":{\"NId\":\""+tsn+"\",\"SerialNumber\":\""+ tsn + "\",\"MaterialDefinitionNId\":\""+prd_id+"\"},\"ToBeConsumedMaterialItemList\":[{\"ToBeConsumedMaterialName\":\""+ mat_id + "\",\"ToBeConsumedMaterialSequence\":1,\"MaterialDefinitionNId\":\""+ mat_id + "\",\"SerialNumber\":\""+ sub_sn + "\",\"TotalQuantity\":1,\"ActualQuantity\":0,\"Quantity\":1}]}]}}");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        public string CreateSerialNumberCache(string token,string orderId,string serialNumber,string stepId,string stationId)
        {
            string json = OData4.SendCmd(token, "CreateSerialNumberCache", "{\"command\":{\"OrderId\":\"" + orderId + "\",SerialNumber:\"" + serialNumber + "\",Status:1,\"StepId\":\"" + stepId + "\",\"StationId\":\"" + stationId + "\"}}", DataScope.OpeApp);
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //get simulation orders
        //http://siemensopds31/sit-svc/Application/AppU4DM/odata/WorkOrder?$expand=FinalMaterial($expand=Material($select=NId))&$filter=startswith(NId, 'WO')&$orderby=Sequence asc&$select=Id,NId,Sequence,InitialQuantity,Status
        public string GetPsOrders(string token,string pre_orderId)
        {
            string json = OData4.QueryData(token, "WorkOrder", "?$expand=FinalMaterial($expand=Material($select=NId))&$filter=startswith(NId, '"+pre_orderId+"')&$orderby=Sequence asc&$select=Id,NId,Sequence,InitialQuantity,Status");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //get serial nubmers by wo id
        //http://siemensopds31/sit-svc/Application/AppU4DM/odata/ProducedMaterialItem?$expand=DM_MaterialTrackingUnit($expand=MaterialTrackingUnit($select=NId))&$select=Id&$filter=WorkOrder_Id eq 307f0069-8189-4e38-81bd-84117dd4be97
        public string GetSerialNumberByWOId(string token, string WO_ID)
        {
            string json = OData4.QueryData(token, "ProducedMaterialItem", "?$expand=DM_MaterialTrackingUnit($expand=MaterialTrackingUnit($select=NId))&$select=Id&$filter=WorkOrder_Id eq "+WO_ID+"");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //通过产品获取工艺的信息
        //http://siemensopds31/sit-svc/Application/AppU4DM/odata/Process?$expand=AsPlannedBOP($select=Id),FinalMaterialId($expand=Material($select=NId))&$filter=FinalMaterialId/Material/NId eq 'A1003860348'
        public string GetProcessByFinalMaterialId(string token,string finalMaterialId)
        {
            string json = OData4.QueryData(token, "Process", "?$expand=AsPlannedBOP($select=Id),FinalMaterialId($expand=Material($select=NId))&$filter=FinalMaterialId/Material/NId eq '"+finalMaterialId+"'");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //	http://siemensopds31/sit-svc/Application/AppU4DM/odata/UADMCreateWorkOrderFromProcess
        //{"command":{"NId":"WO-00000004","ProcessId":"8d545bf2-aa96-47e9-8a49-5a942269173a","ProductionTypeNId":"Serialized","Quantity":10,"AsPlannedId":"2053cf50-4d08-49e1-b712-99581e5d91f3","FinalMaterialId":"9c557480-cc49-4548-860e-dce4643c2f67","Plant":"SIEMENS-ELEC","DueDate":"2020-03-20T12:30:37.000Z"}}
        //{"@odata.context":"http://siemensopds31/sit-svc/application/AppU4DM/odata/$metadata#Siemens.SimaticIT.U4DM.AppU4DM.AppU4DM.DMPOMModel.Commands.UADMCreateWorkOrderFromProcessResponse","Succeeded":true,"Id":"546da152-8d86-4de9-a3fe-02818cd61eff","Error":{"ErrorCode":0,"ErrorMessage":""},"SitUafExecutionDetail":null}//
        public string CreateOrder(string token, string orderId, string processId, int quantity, string asPlannedId,string finalMaterialId,string plant,string duetime)
        {
            string json = OData4.SendCmd(token, "UADMCreateWorkOrderFromProcess", "{\"command\":{\"NId\":\""+ orderId + "\",\"ProcessId\":\""+processId+"\",\"ProductionTypeNId\":\"Serialized\",\"Quantity\":"+ quantity + ",\"AsPlannedId\":\""+ asPlannedId + "\",\"FinalMaterialId\":\""+ finalMaterialId + "\",\"Plant\":\""+plant+ "\",\"DueDate\":\"" + duetime + "\"}}");
           
            return json;
        }

        //http://siemensopds31/sit-svc/Application/AppU4DM/odata/UADMCreateAndAssignProducedMaterialItems
        //{"command":{"WorkOrderId":"2cbf2a97-833c-4315-b607-9ad30b881305","WorkOrderNId":"WO-00000004"}}
        public string CreateOrderSerialNumbers(string token, string wo_id,string orderId)
        {
            string json = OData4.SendCmd(token, "UADMCreateAndAssignProducedMaterialItems", "{\"command\":{\"WorkOrderId\":\""+wo_id+"\",\"WorkOrderNId\":\""+orderId+"\"}}");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("Error", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }

        //http://siemensopds31/sit-svc/Application/AppU4DM/odata/WorkOrderOperation?$expand=WorkOrder($select=NId)&$filter=WorkOrder/NId eq 'WO-00000008'&$orderby=Sequence asc&$select=Name,Description,Sequence,TargetQuantity,ProducedQuantity
        public string GetOrderInformation(string token, string OrderId)
        {
            string json = OData4.QueryData(token, "WorkOrderOperation", "?$expand=WorkOrder($select=NId)&$filter=WorkOrder/NId eq '"+OrderId+"'&$orderby=Sequence asc&$select=Name,Description,Sequence,TargetQuantity,ProducedQuantity");
            JObject jb = JObject.Parse(json);
            JToken res_value;
            if (jb.TryGetValue("value", out res_value))
            {
                return res_value.ToString();
            }
            return null;
        }
    }
}