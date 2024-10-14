using System;

namespace FluidDemo
{
    public struct EnumIndex<TEnum> where TEnum : System.Enum 
    {
        public TEnum Id;
 
        public EnumIndex(int index)
        {
            Id = (TEnum)(object)index;
        }

        public EnumIndex(TEnum id)
        {
            Id = id;
        }
        
        public int AsIndex => Convert.ToInt32(Id);
    }
}