using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpcenterExDsWebService
{
    public class WorkOrderOperation
    {
        private string _OperationName;
        private string _OperationDescription;
        private string _OrderId;
        private string _Sequence;
        private int _TargetQuantity;
        private int _ProducedQuantity;

        public string OperationName { get => _OperationName; set => _OperationName = value; }
        public string OperationDescription { get => _OperationDescription; set => _OperationDescription = value; }
        public string OrderId { get => _OrderId; set => _OrderId = value; }
        public string Sequence { get => _Sequence; set => _Sequence = value; }
        public int Quantity { get => _TargetQuantity; set => _TargetQuantity = value; }
        public int ProducedQuantity { get => _ProducedQuantity; set => _ProducedQuantity = value; }
    }
}