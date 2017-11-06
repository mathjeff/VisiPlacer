using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisiPlacement
{
    class LayoutQuery_And_Response
    {
        public LayoutQuery_And_Response()
        {
        }

        public LayoutQuery_And_Response(LayoutQuery query, SpecificLayout response)
        {
            this.Query = query;
            this.Response = response;
        }

        public LayoutQuery Query { get; set; }
        public SpecificLayout Response { get; set; }
    }
}
