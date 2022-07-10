using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Session
    {

        public Guid Id { get; set; }
     
        public string ErrorType { get; set; }
        public string Error { get; set; }


    }
}
