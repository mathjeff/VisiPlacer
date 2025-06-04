using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// EffectFactory allows registering Effect subtypes and getting them
// Hopefully we can migrate to MAUI effects eventually if they can work for us
namespace VisiPlacement
{
    public class EffectFactory
    {
        public static EffectFactory Instance = new EffectFactory();
        private EffectFactory()
        {

        }
        public void RegisterEffect(String name, ValueProvider<Effect> constructor)
        {
            this.constructors[name] = constructor;
        }
        public Effect Resolve(String name)
        {
            return this.constructors[name].Get();
        }
        Dictionary<String, ValueProvider<Effect>> constructors = new Dictionary<string, ValueProvider<Effect>>();
    }
}
