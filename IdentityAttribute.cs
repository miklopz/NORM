using System;

namespace NORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IdentityAttribute : Attribute
    {
        public IdentityAttribute() { }
    }
}
