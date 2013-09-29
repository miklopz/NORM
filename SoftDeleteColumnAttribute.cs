using System;

namespace NORM
{
    public sealed class SoftDeleteColumnAttribute :  System.Attribute
    {
        private object _softDeleteValue;
        public object SoftDeleteValue
        {
            get { return _softDeleteValue; }
            set { _softDeleteValue = value; }
        }

        public SoftDeleteColumnAttribute(object softDeleteValue)
        {
            _softDeleteValue = softDeleteValue;
        }
    }
}
