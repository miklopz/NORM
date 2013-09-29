using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NORM
{
    internal static class QueryEnum
    {
        internal static readonly string AT = "@";
        internal static readonly string OPEN_PARENTHESIS = "(";
        internal static readonly string CLOSE_PARENTHESIS = ")";
        internal static readonly string INSERT_INTO = "INSERT INTO ";
        internal static readonly string UPDATE = "UPDATE ";
        internal static readonly string VALUES = "VALUES";
        internal static readonly string SELECT_IDENTITY = "SELECT @@IDENTITY";
        internal static readonly string SEMI_COLON = ";";
        internal static readonly string SET = "SET ";
        internal static readonly string COMMA = ",";
        internal static readonly string SPACE = " ";
        internal static readonly string WHERE = "WHERE ";
        internal static readonly string AND = "AND ";
        internal static readonly string OR = "OR ";
        internal static readonly string LIKE = "LIKE ";
        internal static readonly string PERCENT = "%";
        internal static readonly string PLUS = "+";
        internal static readonly string EQUAL = " = ";
        internal static readonly string DELETE = "DELETE FROM ";
        internal static readonly string SELECT = "SELECT ";
        internal static readonly string FROM = "FROM ";
        internal static readonly string PARAMETER = "?";
    }
}
