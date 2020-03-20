using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpcenterExDsWebService
{
    public class ReturnValueBom : ReturnValue
    {
        List<string> _Boms;

        public List<string> Boms { get => _Boms; set => _Boms = value; }
    }
}