using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpcenterExDsWebService
{
    public class OrderInfo
    {
        private string _OrderId;
        private string _FinalMaterialId;
        private int _Quantity;
        private string _Sequence;
        private IList<string> _SerialNumbers;

        public string OrderId { get => _OrderId; set => _OrderId = value; }
        public string FinalMaterialId { get => _FinalMaterialId; set => _FinalMaterialId = value; }
        public int Quantity { get => _Quantity; set => _Quantity = value; }
        public string Sequence { get => _Sequence; set => _Sequence = value; }
        public IList<string> SerialNumbers { get => _SerialNumbers; set => _SerialNumbers = value; }
    }
}