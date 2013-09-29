using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NORM
{
    public sealed class In<TEntity, TProperty> : IWhereCondition<TEntity, TProperty>
    {
        public Func<TEntity, TProperty> Property { get; set; }
        public List<TProperty> Value { get; set; }
    }
}
