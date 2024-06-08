using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace OVERKILL
{

    [Serializable][EnumFieldLabel("values")]
    public class EnumIndexedArray<TValue, TEnum> : IEnumerable where TEnum : struct, IConvertible
    {
        public static readonly int EnumSizeCached = Enum.GetValues(typeof(TEnum)).Length;
        
        protected virtual TValue DefaultValue => default(TValue);
        
        //I wish this could be protected, but unfortunately, both unity and odin serialization will refuse to serialize this whatsoever if its not public
        public TValue[] values;
        
        public int Length => values.Length;

        public TValue this[TEnum index]
        {
            get => values[index.ToInt32(CultureInfo.InvariantCulture)];

            set => values[index.ToInt32(CultureInfo.InvariantCulture)] = value;
        }
        public TValue this[int index] 
        {
            get => values[index];

            set => values[index] = value;
        }

        public static T Copy<T>(T original) where T : EnumIndexedArray<TValue, TEnum>, new()
        {
            T copy = new T();
            Array.Copy(original.values, copy.values, original.Length);
            return copy;
        }
        
        /// <summary>
        /// keep use of this to a minimum. Cache its value whenever possible.
        /// </summary>
        protected int EnumSize => EnumSizeCached;
        

        public EnumIndexedArray(TValue[] arr)
        {
            this.values = arr;
        }

        public EnumIndexedArray()
        {
            this.values = new TValue[EnumSize];
        }
        
        public static implicit operator EnumIndexedArray <TValue, TEnum>(TValue v)
        {
            TValue[] arr = new TValue[EnumSizeCached];

            for (int i = 0; i < arr.Length; i++)
                arr[i] = v;
            
            return new EnumIndexedArray <TValue, TEnum>(arr);
        }

        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
    }

    public class EnumFieldLabelAttribute : Attribute
    {
        public string childValuesFieldName;
        public EnumFieldLabelAttribute(string childValuesFieldName)
        {
            this.childValuesFieldName = childValuesFieldName;
        }
    }

}
