using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NORM
{
    public interface IConditionGroup
    {
        List<IConditionGroup> ConditionGroups { get; set; }
        List<IWhereCondition> WhereConditions { get; set; }

        void AddWhereCondition(IWhereCondition condition);
        void AddConditionGroup(IConditionGroup conditionGroup);
    }
}
