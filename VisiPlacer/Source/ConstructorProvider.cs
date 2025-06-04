using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisiPlacement
{
    public class ConstructorProvider<Child,Parent> : ValueProvider<Parent> where Child: Parent, new()
    {
        public ConstructorProvider() { }

        public Parent Get()
        {
            return new Child();
        }
    }
}
