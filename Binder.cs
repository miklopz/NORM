/******************************************************************************
*  This file is part of the original NORM project.                            *
*                                                                             *
*  This project is intended to facilitate database binding to entity objects  *
*  on the .NET framework.                                                     *
*                                                                             *
*  Usage of any file contained within this project is AT YOUR OWN RISK.       *
*  Neither I nor GitHub take responsability for any damage caused by the      *
*  usage of any file contained in this project and/or repository. These files *
*  are provided for public and/or commercial use AS IS and its provided       *
*  WITHOUT ANY WARRANTY or SUPPORT for any error or damage it may cause       *
*                                                                             *
*  https://github.com/miklopz/NORM                                            *
*                                                                             *
******************************************************************************/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace NORM
{
	/// <summary>
    /// Utility class that binds a datarow or datareader to an object
    /// </summary>
    public static class Binder
    {
        public static void Bind2<T>(T obj, IDataReader dr)
            where T : new()
        {
            if (obj == null || dr == null || dr.IsClosed) return;
            DynamicMethod dm = Engine._readerBindings[Engine._objects[typeof(T).FullName]];
            dm.Invoke(null, new object[] { obj, dr });
        }

        public static void Bind2<T>(T obj, DataRow dr)
            where T : new()
        {
            if (obj == null || dr == null) return;
            DynamicMethod dm = Engine._rowBindings[Engine._objects[typeof(T).FullName]];
            dm.Invoke(null, new object[] { obj, dr });
        }

        private class PropertyEntityDefinition
        {
            public PropertyInfo Property { get; set; }
            public EntityColumnAttribute EntityObject { get; set; }
        }

        private static Dictionary<string, PropertyInfo[]> _typeCache = new Dictionary<string, PropertyInfo[]>();
        private static Dictionary<string, List<PropertyEntityDefinition>> _eProperties = new Dictionary<string, List<PropertyEntityDefinition>>();
        private static List<string> _entityObjects = new List<string>();
        private static PropertyInfo[] GetProperties(Type t)
        {
            if (_typeCache.ContainsKey(t.FullName))
                return _typeCache[t.FullName];
            PropertyInfo[] props = t.GetProperties();
            lock (_typeCache)
            {
                _typeCache.Add(t.FullName, props);
            }
            return props;
        }
        private static List<PropertyEntityDefinition> GetEntityProperties(Type t)
        {
            if (_eProperties.ContainsKey(t.FullName))
                return _eProperties[t.FullName];

            PropertyInfo[] props = GetProperties(t);
            List<PropertyEntityDefinition> retVal = new List<PropertyEntityDefinition>(props.Length);
            foreach (PropertyInfo prop in props)
            {
                object[] patts = prop.GetCustomAttributes(true);
                foreach (object att in patts)
                {
                    if (att.GetType() == typeof(EntityColumnAttribute))
                    {
                        retVal.Add(new PropertyEntityDefinition { Property = prop, EntityObject = (EntityColumnAttribute)att });
                        break;
                    }
                }
            }
            lock (_eProperties)
            {
                _eProperties.Add(t.FullName, retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Checks wether a given IDataReader contains the specified column
        /// </summary>
        /// <param name="reader">The reader to verify</param>
        /// <param name="columnName">The column to check</param>
        /// <returns>True if the reader has the specified column, false if it doesn't have the specified column</returns>
        public static bool HasColumn(IDataReader reader, string columnName)
        {
            if (reader == null || reader.IsClosed) throw new ArgumentException("reader");
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentException("reader");
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i) == columnName)
                    return true;

            return false;
        }

        /// <summary>
        /// Checks wether a given DataRow contains the specified column
        /// </summary>
        /// <param name="row">The row to verify</param>
        /// <param name="columnName">The column to check</param>
        /// <returns>True if the row has the specified column, false if it doesn't have the specified column</returns>
        public static bool HasColumn(DataRow row, string columnName)
        {
            if (row == null) throw new ArgumentNullException("row");
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentException("column");

            return row.Table.Columns.Contains(columnName);
        }

        private static Dictionary<string, int> ResolveNames(DataRow dr)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>(dr.Table.Columns.Count);
            for (int i = 0; i < dr.Table.Columns.Count; i++)
                dict.Add(dr.Table.Columns[i].ColumnName, i);
            return dict;
        }

        private static Dictionary<string, int> ResolveNames(IDataReader dr)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>(dr.FieldCount);
            for (int i = 0; i < dr.FieldCount; i++)
                dict.Add(dr.GetName(i), i);
            return dict;
        }

        /// <summary>
        /// Binds a DataTable to a list of objects
        /// </summary>
        /// <typeparam name="T">The object Type</typeparam>
        /// <param name="list">The list to bind</param>
        /// <param name="dtbl">The DataTable that contians the data</param>
        //public static void Bind<T>(System.Collections.IList list, DataTable dtbl) where T : new()
        //{
        //    if (list == null) throw new ArgumentNullException("list");
        //    if (dtbl == null) throw new ArgumentNullException("dtbl");

        //    foreach (DataRow dr in dtbl.Rows)
        //    {
        //        T item = new T();
        //        Binder.Bind<T>(item, dr);
        //        list.Add(item);
        //    }
        //}

        /// <summary>
        /// Performs a binding from a DataTable to an IList
        /// </summary>
        /// <typeparam name="T">The type to bind</typeparam>
        /// <typeparam name="TReturn">The list type to bind to</typeparam>
        /// <param name="dtbl">The data table</param>
        /// <returns>IList with the data contained in the given DataTable</returns>
        //public static TReturn Bind<T, TReturn>(DataTable dtbl)
        //    where TReturn : System.Collections.IList, new()
        //    where T : new()
        //{
        //    TReturn list = new TReturn();
        //    Bind<T>(list, dtbl);
        //    return list;
        //}

        /// <summary>
        /// Binds a DataReader to a list of objects and closes the reader
        /// </summary>
        /// <typeparam name="T">The type to bind to</typeparam>
        /// <param name="list">The list to bind to</param>
        /// <param name="dr">The data reader to perform the binding</param>
        //public static void Bind<T>(System.Collections.IList list, IDataReader dr) where T : new()
        //{
        //    if (dr == null) throw new ArgumentNullException("dr");
        //    if (list == null) throw new ArgumentNullException("list");
        //    if (dr.IsClosed) throw new ArgumentException("dr");

        //    while (dr.Read())
        //    {
        //        T item = new T();
        //        Binder.Bind<T>(item, dr);
        //        list.Add(dr);
        //    }
        //    dr.Close();
        //}


        /// <summary>
        /// Performs a binding from a DataReader to an IList
        /// </summary>
        /// <typeparam name="T">The type to bind</typeparam>
        /// <typeparam name="TReturn">The list type to bind to</typeparam>
        /// <param name="dr">The data reader</param>
        /// <returns>IList with the data contained in the given data reader</returns>
        //public static TReturn Bind<T, TReturn>(IDataReader dr)
        //    where TReturn : System.Collections.IList, new()
        //    where T : new()
        //{
        //    TReturn list = new TReturn();
        //    Bind<T>(list, dr);
        //    return list;
        //}

        /// <summary>
        /// Binds an Entity Object's public properties with a data row
        /// </summary>
        /// <param name="value">The object to bind</param>
        /// <param name="row">The DataRow to bind</param>
        //public static void Bind<T>(T value, DataRow row)
        //{
        //    if(value == null) throw new ArgumentNullException("value");
        //    if(row == null) throw new ArgumentNullException("row");
        //    Type t = value.GetType();
        //    DynamicMethod dm;
        //    ILGenerator il;
        //    Type[] aType;
        //    Dictionary<string, int> cr = ResolveNames(row);
        //    bool isEntityObject = false;

        //    if (!_entityObjects.Contains(t.FullName))
        //    {
        //        Attribute[] atts = Attribute.GetCustomAttributes(t);
        //        foreach (Attribute att in atts)
        //            if (att is EntityObjectAttribute)
        //                isEntityObject = true;

        //        if (!isEntityObject) throw new NotAnEntityObjectException(t.Name);
        //        lock (_entityObjects)
        //        {
        //            _entityObjects.Add(t.FullName);
        //        }
        //    }

        //    EntityColumnAttribute col = null;

        //    List<PropertyEntityDefinition> properties = GetEntityProperties(t);
        //    foreach (PropertyEntityDefinition ped in properties)
        //    {
        //        PropertyInfo property = ped.Property;
        //        if (!property.CanWrite) continue;

        //        col = null;
        //        col = (EntityColumnAttribute)ped.EntityObject;

        //        if(cr.ContainsKey(col.ColumnName))
        //        {
        //            MethodInfo converter = string.IsNullOrEmpty(col.Converter) ? null : t.GetMethod(col.Converter);
                            
        //            if (col.DbType == DbType.Int64)
        //            {
        //                #region Int64 Emit Version
        //                long? temp = row[cr[col.ColumnName]] == DBNull.Value ? (long?)null : (long)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : long.MinValue }); 
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Binary)
        //            {
        //                #region Binary Emit Version
        //                byte[] temp = row[cr[col.ColumnName]] == DBNull.Value ? null : (byte[])row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, temp });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Boolean)
        //            {
        //                #region Boolean Emit Version
        //                bool? temp = row[cr[col.ColumnName]] == DBNull.Value ? (bool?)null : (bool)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : false });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.AnsiStringFixedLength || col.DbType == DbType.String || col.DbType == DbType.StringFixedLength || col.DbType == DbType.AnsiString)
        //            {
        //                #region String Emit Version
        //                string temp = row[cr[col.ColumnName]] == DBNull.Value ? null : (string)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, temp });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Date || col.DbType == DbType.DateTime || col.DbType == DbType.DateTime2)
        //            {
        //                #region DateTime Emit Version
        //                DateTime? temp = row[cr[col.ColumnName]] == DBNull.Value ? (DateTime?)null : (DateTime)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : DateTime.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.DateTimeOffset)
        //            {
        //                #region DateTimeOffset Emit Version
        //                DateTimeOffset? temp = row[cr[col.ColumnName]] == DBNull.Value ? (DateTimeOffset?)null : (DateTimeOffset)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : DateTimeOffset.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Decimal)
        //            {
        //                #region decimal Emit Version
        //                decimal? temp = row[cr[col.ColumnName]] == DBNull.Value ? (decimal?)null : (decimal)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : decimal.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Double)
        //            {
        //                #region double Emit Version
        //                double? temp = row[cr[col.ColumnName]] == DBNull.Value ? (double?)null : (double)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : double.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int32)
        //            {
        //                #region Int32 Emit Version
        //                int? temp = row[cr[col.ColumnName]] == DBNull.Value ? (int?)null : (int)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : int.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Single)
        //            {
        //                #region Single Emit Version
        //                Single? temp = row[cr[col.ColumnName]] == DBNull.Value ? (Single?)null : (Single)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : Single.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int16)
        //            {
        //                #region Int16 Emit Version
        //                short? temp = row[cr[col.ColumnName]] == DBNull.Value ? (short?)null : (short)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : short.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Time)
        //            {
        //                #region Time Emit Version
        //                TimeSpan? temp = row[cr[col.ColumnName]] == DBNull.Value ? (TimeSpan?)null : (TimeSpan)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : TimeSpan.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Byte)
        //            {
        //                #region byte Emit Version
        //                byte? temp = row[cr[col.ColumnName]] == DBNull.Value ? (byte?)null : (byte)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : byte.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Object)
        //            {
        //                #region Object Emit Version
        //                object temp = row[cr[col.ColumnName]] == DBNull.Value ? null : row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, temp });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Xml)
        //            {
        //                #region Xml Emit Version
        //                System.Xml.XmlDocument doc = row[cr[col.ColumnName]] == DBNull.Value ? null : new System.Xml.XmlDocument();
        //                if (doc != null && row[cr[col.ColumnName]] != DBNull.Value)
        //                    doc.LoadXml(Convert.ToString(row[cr[col.ColumnName]]));
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, doc });
        //                #endregion
        //            }
        //            else
        //                throw new Exception(col.DbType.ToString());
        //        }
        //        else
        //        {
        //            if (col.DbType == DbType.Int64)
        //            {
        //                #region Int64 Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (long?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, long.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Binary)
        //            {
        //                #region Binary Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, (byte[])null });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Boolean)
        //            {
        //                #region Boolean Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (bool?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, false });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.AnsiStringFixedLength || col.DbType == DbType.String || col.DbType == DbType.StringFixedLength || col.DbType == DbType.AnsiString)
        //            {
        //                #region String Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, (string)null });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Date || col.DbType == DbType.DateTime || col.DbType == DbType.DateTime2)
        //            {
        //                #region DateTime Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (DateTime?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, DateTime.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.DateTimeOffset)
        //            {
        //                #region DateTime Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (DateTimeOffset?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, DateTimeOffset.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Decimal)
        //            {
        //                #region DateTime Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (decimal?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, decimal.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Double)
        //            {
        //                #region Double Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (double?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, double.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int32)
        //            {
        //                #region Int32 Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (int?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, int.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Single)
        //            {
        //                #region Single Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (Single?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, Single.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int16)
        //            {
        //                #region Int16 Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (short?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, short.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Time)
        //            {
        //                #region TimeSpan Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (TimeSpan?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, TimeSpan.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Byte)
        //            {
        //                #region Byte Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (byte?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, byte.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Object)
        //            {
        //                #region String Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, null });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Xml)
        //            {
        //                #region String Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, (System.Xml.XmlDocument)null });
        //                #endregion
        //            }
        //            else
        //                throw new Exception(col.DbType.ToString());
        //        }
        //    }
        //}

        /// <summary>
        /// Binds an Entity Object's public properties with a data reader
        /// </summary>
        /// <param name="value">The object to bind</param>
        /// <param name="row">The DataReader to bind</param>
        //public static void Bind<T>(T value, IDataReader row)
        //{
        //    if (value == null) throw new ArgumentNullException("value");
        //    if (row == null) throw new ArgumentNullException("row");
        //    if (row.IsClosed) throw new ArgumentException("The Data reader is closed");

        //    if (value is System.Collections.IList)
        //    {
        //        Bind(value as System.Collections.IList, row);
        //        return;
        //    }

        //    Type t = value.GetType();
        //    DynamicMethod dm;
        //    ILGenerator il;
        //    Type[] aType;
        //    Dictionary<string, int> cr = ResolveNames(row);

        //    if (!_entityObjects.Contains(t.FullName))
        //    {
        //        bool isEntityObject = false;
        //        Attribute[] atts = Attribute.GetCustomAttributes(t);
        //        foreach (Attribute att in atts)
        //            if (att is EntityObjectAttribute)
        //                isEntityObject = true;

        //        if (!isEntityObject) throw new NotAnEntityObjectException(t.Name);
        //        lock (_entityObjects)
        //        {
        //            _entityObjects.Add(t.FullName);
        //        }
        //    }

        //    EntityColumnAttribute col = null;

        //    List<PropertyEntityDefinition> properties = GetEntityProperties(t);
        //    foreach (PropertyEntityDefinition ped in properties)
        //    {
        //        PropertyInfo property = ped.Property;
        //        col = null;
        //        col = ped.EntityObject;
        //        if (!property.CanWrite) continue;

        //        if (cr.ContainsKey(col.ColumnName))
        //        {
        //            MethodInfo converter = string.IsNullOrEmpty(col.Converter) ? null : t.GetMethod(col.Converter);

        //            if (col.DbType == DbType.Int64)
        //            {
        //                #region Int64 Emit Version
        //                long? temp = row[cr[col.ColumnName]] == DBNull.Value ? (long?)null : (long)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : long.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Binary)
        //            {
        //                #region Binary Emit Version
        //                byte[] temp = row[cr[col.ColumnName]] == DBNull.Value ? null : (byte[])row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, temp });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Boolean)
        //            {
        //                #region Boolean Emit Version
        //                bool? temp = row[cr[col.ColumnName]] == DBNull.Value ? (bool?)null : (bool)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : false });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.AnsiStringFixedLength || col.DbType == DbType.String || col.DbType == DbType.StringFixedLength || col.DbType == DbType.AnsiString)
        //            {
        //                #region String Emit Version
        //                string temp = row[cr[col.ColumnName]] == DBNull.Value ? null : (string)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, temp });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Date || col.DbType == DbType.DateTime || col.DbType == DbType.DateTime2)
        //            {
        //                #region DateTime Emit Version
        //                DateTime? temp = row[cr[col.ColumnName]] == DBNull.Value ? (DateTime?)null : (DateTime)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : DateTime.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.DateTimeOffset)
        //            {
        //                #region DateTimeOffset Emit Version
        //                DateTimeOffset? temp = row[cr[col.ColumnName]] == DBNull.Value ? (DateTimeOffset?)null : (DateTimeOffset)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : DateTimeOffset.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Decimal)
        //            {
        //                #region decimal Emit Version
        //                decimal? temp = row[cr[col.ColumnName]] == DBNull.Value ? (decimal?)null : (decimal)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : decimal.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Double)
        //            {
        //                #region double Emit Version
        //                double? temp = row[cr[col.ColumnName]] == DBNull.Value ? (double?)null : (double)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : double.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int32)
        //            {
        //                #region Int32 Emit Version
        //                int? temp = row[cr[col.ColumnName]] == DBNull.Value ? (int?)null : (int)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : int.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Single)
        //            {
        //                #region Single Emit Version
        //                Single? temp = row[cr[col.ColumnName]] == DBNull.Value ? (Single?)null : (Single)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : Single.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int16)
        //            {
        //                #region Int16 Emit Version
        //                short? temp = row[cr[col.ColumnName]] == DBNull.Value ? (short?)null : (short)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : short.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Time)
        //            {
        //                #region Time Emit Version
        //                TimeSpan? temp = row[cr[col.ColumnName]] == DBNull.Value ? (TimeSpan?)null : (TimeSpan)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : TimeSpan.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Byte)
        //            {
        //                #region byte Emit Version
        //                byte? temp = row[cr[col.ColumnName]] == DBNull.Value ? (byte?)null : (byte)row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, temp });
        //                else
        //                    dm.Invoke(null, new object[] { value, temp.HasValue ? temp.Value : byte.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Object)
        //            {
        //                #region Object Emit Version
        //                object temp = row[cr[col.ColumnName]] == DBNull.Value ? null : row[cr[col.ColumnName]];
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, temp });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Xml)
        //            {
        //                #region Xml Emit Version
        //                System.Xml.XmlDocument doc = row[cr[col.ColumnName]] == DBNull.Value ? null : new System.Xml.XmlDocument();
        //                if (doc != null && row[cr[col.ColumnName]] != DBNull.Value)
        //                    doc.LoadXml(Convert.ToString(row[cr[col.ColumnName]]));
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                if (converter != null)
        //                    il.EmitCall(OpCodes.Call, converter, null);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, doc });
        //                #endregion
        //            }
        //            else
        //                throw new Exception(col.DbType.ToString());
        //        }
        //        else
        //        {
        //            if (col.DbType == DbType.Int64)
        //            {
        //                #region Int64 Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (long?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, long.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Binary)
        //            {
        //                #region Binary Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, (byte[])null });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Boolean)
        //            {
        //                #region Boolean Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (bool?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, false });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.AnsiStringFixedLength || col.DbType == DbType.String || col.DbType == DbType.StringFixedLength || col.DbType == DbType.AnsiString)
        //            {
        //                #region String Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, (string)null });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Date || col.DbType == DbType.DateTime || col.DbType == DbType.DateTime2)
        //            {
        //                #region DateTime Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (DateTime?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, DateTime.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.DateTimeOffset)
        //            {
        //                #region DateTime Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (DateTimeOffset?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, DateTimeOffset.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Decimal)
        //            {
        //                #region DateTime Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (decimal?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, decimal.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Double)
        //            {
        //                #region Double Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (double?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, double.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int32)
        //            {
        //                #region Int32 Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (int?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, int.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Single)
        //            {
        //                #region Single Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (Single?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, Single.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Int16)
        //            {
        //                #region Int16 Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (short?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, short.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Time)
        //            {
        //                #region TimeSpan Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (TimeSpan?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, TimeSpan.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Byte)
        //            {
        //                #region Byte Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        //                    dm.Invoke(null, new object[] { value, (byte?)null });
        //                else
        //                    dm.Invoke(null, new object[] { value, byte.MinValue });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Object)
        //            {
        //                #region String Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, null });
        //                #endregion
        //            }
        //            else if (col.DbType == DbType.Xml)
        //            {
        //                #region String Emit Version
        //                aType = null;
        //                aType = new Type[] { t, property.PropertyType };
        //                dm = null;
        //                dm = new DynamicMethod("set_" + property.Name, null, aType, true);
        //                il = null;
        //                il = dm.GetILGenerator();
        //                il.Emit(OpCodes.Ldarg_0);
        //                il.Emit(OpCodes.Ldarg_1);
        //                il.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //                il.Emit(OpCodes.Ret);
        //                dm.Invoke(null, new object[] { value, (System.Xml.XmlDocument)null });
        //                #endregion
        //            }
        //            else
        //                throw new Exception(col.DbType.ToString());
        //        }
        //    }
        //}

        /// <summary>
        /// Binds an Entity Object's public properties with a data row
        /// </summary>
        /// <typeparam name="T">Any reference entity object type</typeparam>
        /// <param name="row">The DataRow to bind</param>
        /// <returns>New instance of T with its properties bound to the given data row</returns>
        //public static T Bind<T>(DataRow row) where T : new()
        //{
        //    if (row == null) throw new ArgumentNullException("row");

        //    T value = new T();
        //    Bind(value, row);
        //    return value;
        //}

        /// <summary>
        /// Binds an Entity Object's public properties with a data reader
        /// </summary>
        /// <typeparam name="T">Any reference entity object type</typeparam>
        /// <param name="row">The DataReader to bind</param>
        /// <returns>New instance of T with its properties bound to the given data row</returns>
        //public static T Bind<T>(IDataReader row) where T : new()
        //{
        //    if (row == null) throw new ArgumentNullException("row");

        //    T value = new T();
        //    Bind(value, row);
        //    return value;
        //}
    }
}