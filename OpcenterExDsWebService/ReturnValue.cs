using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpcenterExDsWebService
{
    public class ReturnValue
    {
        private bool _Succeed;
        private string _Message;
        private object _Result;

        public bool Succeed { get => _Succeed; set => _Succeed = value; }
        public string Message { get => _Message; set => _Message = value; }
        public object Result { get => _Result; set => _Result = value; }
    }
}