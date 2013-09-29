using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NORM
{
    public interface IWhereCondition
    {
    }

    public interface IWhereCondition<TEntity, TProperty> : IWhereCondition
    {
        Func<TEntity, TProperty>Property { get; set; }
    }
}
