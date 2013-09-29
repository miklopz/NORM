using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NORM
{
    public sealed class AndConditionGroup : IConditionGroup
    {
        private List<IConditionGroup> _conditionGroups;
        public List<IConditionGroup> ConditionGroups
        {
            get { return _conditionGroups; }
            set { _conditionGroups = value; }
        }
        private List<IWhereCondition> _whereConditions;
        public List<IWhereCondition> WhereConditions
        {
            get { return _whereConditions; }
            set { _whereConditions = value; }
        }

        public void AddWhereCondition(IWhereCondition condition)
        {
            if(condition == null) throw new ArgumentException("condition");
            if (_whereConditions == null) _whereConditions = new List<IWhereCondition>();
            _whereConditions.Add(condition);
        }

        public void AddConditionGroup(IConditionGroup conditionGroup)
        {
            if (conditionGroup == null) throw new ArgumentException("conditionGroup");
            if (_conditionGroups == null) _conditionGroups = new List<IConditionGroup>();
            _conditionGroups.Add(conditionGroup);
        }
    }
}
