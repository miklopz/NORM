using System;

namespace NORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class UnassignableAttribute : Attribute
    {
        public UnassignableAttribute() { }
    }
}
