using System;
using System.Collections.Generic;
using System.Text;

namespace VisiPlacement
{
    public interface ValueProvider<T>
    {
        T Get();
    }
    public class ConstantValueProvider<T> : ValueProvider<T>
    {
        public ConstantValueProvider(T value)
        {
            this.value = value;
        }
        public T Get()
        {
            return this.value;
        }
        public T value;
    }

    public interface ValueConverter<T, U>
    {
        U Get(T input);
    }

    public class ConstantValueConverter<T, U> : ValueConverter<T, U>
    {
        public ConstantValueConverter(U value)
        {
            this.value = value;
        }
        public U Get(T input)
        {
            return this.value;
        }
        public U value;
    }
}
