using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

// InputScopeUtils provides utils pertaining to the InputScope class
namespace VisiPlacement.Misc
{
    public class InputScopeUtils
    {
        public static InputScope Numeric
        {
            get
            {
                InputScope inputScope = new InputScope();
                InputScopeName inputScopeName = new InputScopeName();
                inputScopeName.NameValue = InputScopeNameValue.Number;
                inputScope.Names.Add(inputScopeName);
                return inputScope;
            }
        }
    }
}
