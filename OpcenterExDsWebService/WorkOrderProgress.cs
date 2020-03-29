using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpcenterExDsWebService
{
    public class WorkOrderProgress
    {
        private string _OrderId;
        private int _TargetQuantity;
        private int _ProducedQuantity;

        public string OrderId { get => _OrderId; set => _OrderId = value; }
        public int Quantity { get => _TargetQuantity; set => _TargetQuantity = value; }
        public int ProducedQuantity { get => _ProducedQuantity; set => _ProducedQuantity = value; }
    }
}