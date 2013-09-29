using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NORM
{
    public sealed class Between<TEntity, TProperty> : IWhereCondition<TEntity, TProperty>
    {
        public Func<TEntity, TProperty> Property { get; set; }
        public TProperty Start { get; set; }
        public TProperty End { get; set; }
    }
}
