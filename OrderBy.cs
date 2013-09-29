using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NORM
{
    public sealed class OrderBy<TEntity, TProperty>
    {
        public Func<TEntity, TProperty> Property { get; set; }
        public OrderDirection Direction { get; set; }
    }
}
