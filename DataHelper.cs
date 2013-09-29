using System;
using System.Data;

namespace NORM
{
    public static class DataHelper
    {
        public static bool Contains(IDataReader dr, string columnName)
        {
            if (dr == null || dr.IsClosed) return false;
            if (string.IsNullOrEmpty(columnName)) return false;
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i) == columnName)
                    return true;

            return false;
        }

        public static bool Contains(DataRow dr, string columnName)
        {
            if (dr == null || string.IsNullOrEmpty(columnName) || dr.Table == null || dr.Table.Columns == null || dr.Table.Columns.Count == 0) return false;
            return dr.Table.Columns.Contains(columnName);
        }
    }
}
