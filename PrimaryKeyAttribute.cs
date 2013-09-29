using System;

namespace NORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute() { }
    }
}
