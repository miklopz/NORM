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
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace NORM
{
	internal static class TypeHelper
	{
        internal static EntityColumnAttribute GetEntityColumn(PropertyInfo i)
        {
            object[] att = Attribute.GetCustomAttributes(i);
            foreach (object item in att)
                if (item.GetType() == typeof(EntityColumnAttribute))
                    return (EntityColumnAttribute)item;
            return null;
        }

        internal static bool IsPrimaryKey(PropertyInfo i)
        {
            if (i == null) throw new ArgumentNullException("i");
            if (GetEntityColumn(i) == null) return false;
            object[] att = Attribute.GetCustomAttributes(i);
            foreach (object item in att)
                if (item.GetType() == typeof(PrimaryKeyAttribute))
                    return true;
            return false;
        }

        internal static bool IsIdentity(PropertyInfo i)
        {
            if (i == null) throw new ArgumentNullException("i");
            if (GetEntityColumn(i) == null) return false;
            object[] att = Attribute.GetCustomAttributes(i);
            foreach (object item in att)
                if (item.GetType() == typeof(IdentityAttribute))
                    return true;
            return false;
        }

        internal static bool IsReadonly(PropertyInfo i)
        {
            if (i == null) throw new ArgumentNullException("i");
            if (GetEntityColumn(i) == null) return false;
            object[] att = Attribute.GetCustomAttributes(i);
            foreach (object item in att)
                if (item.GetType() == typeof(ReadonlyAttribute))
                    return true;
            return false;
        }

        internal static SoftDeleteColumnAttribute GetSoftDeleteColumn(PropertyInfo i)
        {
            if (i == null) throw new ArgumentNullException("i");
            if (GetEntityColumn(i) == null) return null;
            object[] att = Attribute.GetCustomAttributes(i);
            foreach (object item in att)
                if (item.GetType() == typeof(SoftDeleteColumnAttribute))
                    return (SoftDeleteColumnAttribute)item;
            return null;
        }

        internal static List<PropertyInfo> GetPrimaryKeys(Type t)
        {
            if (t == null) throw new ArgumentException("t");
            List<PropertyInfo> properties = new List<PropertyInfo>(t.GetProperties());
            return new List<PropertyInfo>(properties.Where(c => IsPrimaryKey(c)));
        }

        internal static PropertyInfo GetIdentityProperty(Type t)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>(t.GetProperties());
            foreach (PropertyInfo p in properties)
                if (IsIdentity(p))
                    return p;
            return null;
        }

        internal static string GetEntityObjectName(Type t)
        {
            object[] att = Attribute.GetCustomAttributes(t);
            foreach (object item in att)
                if (item.GetType() == typeof(EntityObjectAttribute))
                    return ((EntityObjectAttribute)item).TableName;
            return null;
        }

        internal static bool IsSoftDeleteType(Type t)
        {
            object[] att = Attribute.GetCustomAttributes(t);
            foreach (object item in att)
                if (item.GetType() == typeof(SoftDeleteAttribute))
                    return true;
            return false;
        }

        internal static EntityObjectAttribute GetEntityObject(Type t)
        {
            object[] att = Attribute.GetCustomAttributes(t);
            foreach (object item in att)
                if (item.GetType() == typeof(EntityObjectAttribute))
                    return ((EntityObjectAttribute)item);
            return null;
        }

        internal static DynamicMethod GetDataReaderBindingMethod(Type t)
        {
            MethodInfo isClosed = typeof(IDataReader).GetMethod("get_IsClosed");
            MethodInfo getItem = typeof(IDataRecord).GetMethod("get_Item", new Type[] { typeof(string) });
            ConstructorInfo ctor = t.GetConstructor(new Type[] { });
            PropertyInfo[] properties = t.GetProperties();
            EntityColumnAttribute attr = null;
            FieldInfo dbNull = typeof(DBNull).GetField("Value");
            MethodInfo contains = typeof(DataHelper).GetMethod("Contains", new Type[] { typeof(IDataReader), typeof(string) });
            Type realType = null;
            bool isNullable;


            DynamicMethod dm = new DynamicMethod(t.FullName.Replace(".", "_") + "_Bind", typeof(void), new Type[] { t, typeof(IDataReader) });
            ILGenerator ilg = dm.GetILGenerator();
            Label IL_000f = ilg.DefineLabel();
            Label IL_0010 = ilg.DefineLabel();
            Label IL_001a = ilg.DefineLabel();
            Label IL_0140 = ilg.DefineLabel();
            Label IL_002c = ilg.DefineLabel();
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Brfalse_S, IL_000f);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.EmitCall(OpCodes.Callvirt, isClosed, null);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
            ilg.Emit(OpCodes.Br_S, IL_0010);
            ilg.MarkLabel(IL_000f);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.MarkLabel(IL_0010);
            ilg.Emit(OpCodes.Brtrue_S, IL_001a);
            ilg.Emit(OpCodes.Br, IL_0140);
            ilg.MarkLabel(IL_001a);
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldnull);
            ilg.Emit(OpCodes.Ceq);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
            ilg.Emit(OpCodes.Brtrue_S, IL_002c);
            ilg.Emit(OpCodes.Newobj, ctor);
            ilg.Emit(OpCodes.Starg_S, 0); 
            ilg.MarkLabel(IL_002c);

            foreach (PropertyInfo pi in properties)
            {
                if (pi == null) continue;
                attr = GetEntityColumn(pi);
                if (attr == null || string.IsNullOrEmpty(attr.ColumnName)) continue;
                Label IL_0056 = ilg.DefineLabel();
                Label IL_008b = ilg.DefineLabel();
                Label IL_005f = ilg.DefineLabel();
                Label NoColumn = ilg.DefineLabel();

                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Ldstr, attr.ColumnName);
                ilg.EmitCall(OpCodes.Call, contains, null);
                ilg.Emit(OpCodes.Brfalse_S, NoColumn);

                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Ldstr, attr.ColumnName);
                ilg.EmitCall(OpCodes.Callvirt, getItem, null);

                realType = Nullable.GetUnderlyingType(pi.PropertyType);
                isNullable = realType != null;
                if (!isNullable) { realType = pi.PropertyType; }

                // Stack Trace is 2

                ilg.Emit(OpCodes.Ldsfld, dbNull); // 2 -> 3
                if(realType.IsValueType)
                    ilg.Emit(OpCodes.Beq, IL_0056); // 3 -> 1
                else
                    ilg.Emit(OpCodes.Beq, IL_008b); // 3 -> 1

                ilg.Emit(OpCodes.Ldarg_1); // 1 -> 2
                ilg.Emit(OpCodes.Ldstr, attr.ColumnName); // 2 -> 3

                ilg.EmitCall(OpCodes.Callvirt, getItem, null); // 3 -> 1 -> 2


                if (realType.IsValueType)
                {
                    if (realType == typeof(float) || realType == typeof(double))
                        ilg.Emit(OpCodes.Unbox_Any, typeof(decimal));
                    ilg.Emit(OpCodes.Unbox_Any, realType); // 2
                    if (isNullable)
                        ilg.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(realType).GetConstructor(new Type[] { realType })); // 2
                    ilg.Emit(OpCodes.Br_S, IL_005f);
                    ilg.MarkLabel(IL_0056);
                    LocalBuilder lb = ilg.DeclareLocal(isNullable ? typeof(Nullable<>).MakeGenericType(realType) : realType);
                    ilg.Emit(OpCodes.Ldloca_S, lb); // 1 -> 2
                    ilg.Emit(OpCodes.Initobj, isNullable ? typeof(Nullable<>).MakeGenericType(realType) : realType); // 2 -> 1
                    ilg.Emit(OpCodes.Ldloc, lb); // 1 -> 2
                }
                else
                {
                    ilg.Emit(OpCodes.Castclass, pi.PropertyType); // 2
                    ilg.Emit(OpCodes.Br_S, IL_005f);
                    ilg.MarkLabel(IL_008b);
                    ilg.Emit(OpCodes.Ldnull); // 1 -> 2
                }
                ilg.MarkLabel(IL_005f);
                ilg.EmitCall(OpCodes.Callvirt, pi.GetSetMethod(), null);
                ilg.MarkLabel(NoColumn);

            }
            ilg.MarkLabel(IL_0140);
            ilg.Emit(OpCodes.Ret);

            return dm;
        }

        internal static DynamicMethod GetDataRowBindingMethod(Type t)
        {
            MethodInfo getItem = typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(string) });
            ConstructorInfo ctor = t.GetConstructor(new Type[] { });
            PropertyInfo[] properties = t.GetProperties();
            EntityColumnAttribute attr = null;
            FieldInfo dbNull = typeof(DBNull).GetField("Value");
            MethodInfo contains = typeof(DataHelper).GetMethod("Contains", new Type[] { typeof(DataRow), typeof(string) });
            Type realType = null;
            bool isNullable;


            DynamicMethod dm = new DynamicMethod(t.FullName.Replace(".", "_") + "_Bind", typeof(void), new Type[] { t, typeof(DataRow) });
            ILGenerator ilg = dm.GetILGenerator();
            Label IL_000f = ilg.DefineLabel();
            Label IL_0010 = ilg.DefineLabel();
            Label IL_001a = ilg.DefineLabel();
            Label IL_0140 = ilg.DefineLabel();
            Label IL_002c = ilg.DefineLabel();
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Brfalse, IL_0140);
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldnull);
            ilg.Emit(OpCodes.Ceq);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
            ilg.Emit(OpCodes.Brtrue_S, IL_002c);
            ilg.Emit(OpCodes.Newobj, ctor);
            ilg.Emit(OpCodes.Starg_S, 0);
            ilg.MarkLabel(IL_002c);

            foreach (PropertyInfo pi in properties)
            {
                if (pi == null) continue;
                attr = GetEntityColumn(pi);
                if (attr == null || string.IsNullOrEmpty(attr.ColumnName)) continue;
                
                Label IL_0056 = ilg.DefineLabel();
                Label IL_008b = ilg.DefineLabel();
                Label IL_005f = ilg.DefineLabel();
                Label NoColumn = ilg.DefineLabel();

                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Ldstr, attr.ColumnName);
                ilg.EmitCall(OpCodes.Call, contains, null); // 2 -> 0 -> 1

                ilg.Emit(OpCodes.Brfalse, NoColumn);

                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Ldstr, attr.ColumnName);
                ilg.EmitCall(OpCodes.Callvirt, getItem, null);


                realType = Nullable.GetUnderlyingType(pi.PropertyType);
                isNullable = realType != null;
                if (!isNullable) { realType = pi.PropertyType; }

            //    // Stack Trace is 2

                ilg.Emit(OpCodes.Ldsfld, dbNull); // 2 -> 3
                if (realType.IsValueType)
                    ilg.Emit(OpCodes.Beq, IL_0056); // 3 -> 1
                else
                    ilg.Emit(OpCodes.Beq, IL_008b); // 3 -> 1

                ilg.Emit(OpCodes.Ldarg_1); // 1 -> 2
                ilg.Emit(OpCodes.Ldstr, attr.ColumnName); // 2 -> 3

                ilg.EmitCall(OpCodes.Callvirt, getItem, null); // 3 -> 1 -> 2


                if (realType.IsValueType)
                {
                    if (realType == typeof(float) || realType == typeof(double))
                        ilg.Emit(OpCodes.Unbox_Any, typeof(decimal));
                    ilg.Emit(OpCodes.Unbox_Any, realType); // 2
                    if (isNullable)
                        ilg.Emit(OpCodes.Newobj, typeof(Nullable<>).MakeGenericType(realType).GetConstructor(new Type[] { realType })); // 2
                    ilg.Emit(OpCodes.Br_S, IL_005f);
                    ilg.MarkLabel(IL_0056);
                    LocalBuilder lb = ilg.DeclareLocal(isNullable ? typeof(Nullable<>).MakeGenericType(realType) : realType);
                    ilg.Emit(OpCodes.Ldloca_S, lb); // 1 -> 2
                    ilg.Emit(OpCodes.Initobj, isNullable ? typeof(Nullable<>).MakeGenericType(realType) : realType); // 2 -> 1
                    ilg.Emit(OpCodes.Ldloc, lb); // 1 -> 2
                }
                else
                {
                    ilg.Emit(OpCodes.Castclass, pi.PropertyType); // 2
                    ilg.Emit(OpCodes.Br_S, IL_005f);
                    ilg.MarkLabel(IL_008b);
                    ilg.Emit(OpCodes.Ldnull); // 1 -> 2
                }
                ilg.MarkLabel(IL_005f);
                ilg.EmitCall(OpCodes.Callvirt, pi.GetSetMethod(), null);
                ilg.MarkLabel(NoColumn);

            }
            ilg.MarkLabel(IL_0140);
            ilg.Emit(OpCodes.Ret);

            return dm;
        }

        internal static DynamicMethod GetInsertMethod(Type t)
        {
            EntityObjectAttribute eo = GetEntityObject(t);
            if (eo == null) return null;


            return null;
        }

        internal static Type[] GetTypes()
        {
            Assembly calling = Assembly.GetCallingAssembly();
            Assembly executing = Assembly.GetExecutingAssembly();
            Assembly entry = Assembly.GetEntryAssembly();

            Type[] callingTypes = calling == null ? null : calling.GetTypes();
            Type[] executingTypes = executing == null ? null : executing.GetTypes();
            Type[] entryTypes = entry == null ? null : entry.GetTypes();

            Type[] types = new Type[(callingTypes == null ? 0 : callingTypes.Length) +
                (executingTypes == null ? 0 : executingTypes.Length) +
                (entryTypes == null ? 0 : entryTypes.Length)];

            int i = 0;
            if (callingTypes != null && callingTypes.Length > 0)
            {
                foreach (Type t in callingTypes)
                {
                    types[i] = t;
                    i++;
                }
            }
            if (executingTypes != null && executingTypes.Length > 0)
            {
                foreach (Type t in executingTypes)
                {
                    types[i] = t;
                    i++;
                }
            }
            if (entryTypes != null && entryTypes.Length > 0)
            {
                foreach (Type t in entryTypes)
                {
                    types[i] = t;
                    i++;
                }
            }
            return types;
        }

        internal static string GetInsertStatement(Type t)
        {
            EntityObjectAttribute eo = GetEntityObject(t);
            if (eo == null)
                return null;
            StringBuilder sb = new StringBuilder(QueryEnum.INSERT_INTO);
            sb.Append(eo.TableName);
            bool requiresIdentity = false;
            StringBuilder sbCols = new StringBuilder();
            StringBuilder sbPars = new StringBuilder();
            List<PropertyInfo> properties = new List<PropertyInfo>(t.GetProperties());
            foreach (PropertyInfo p in properties)
            {
                if(p == null) continue;
                EntityColumnAttribute ec = GetEntityColumn(p);
                if (ec == null) continue;
                if (IsIdentity(p)) { requiresIdentity = true; continue; }
                if (IsReadonly(p)) continue;
                if(sbCols.Length > 0)
                    sbCols.Append(QueryEnum.COMMA);
                sbCols.Append(ec.ColumnName);
                if (sbPars.Length > 0)
                    sbPars.Append(QueryEnum.COMMA);
                sbPars.Append(QueryEnum.PARAMETER);
            }
            sb.Append(QueryEnum.OPEN_PARENTHESIS);
            sb.Append(sbCols);
            sb.Append(QueryEnum.CLOSE_PARENTHESIS);
            sb.Append(QueryEnum.VALUES);
            sb.Append(QueryEnum.OPEN_PARENTHESIS);
            sb.Append(sbPars);
            sb.Append(QueryEnum.CLOSE_PARENTHESIS);
            sb.Append(QueryEnum.SEMI_COLON);
            sb.Append(Environment.NewLine);
            if (requiresIdentity)
            {
                sb.Append(QueryEnum.SELECT_IDENTITY);
                sb.Append(QueryEnum.SEMI_COLON);
            }
            return sb.ToString();
        }

        internal static string GetUpdateStatement(Type t)
        {
            EntityObjectAttribute eo = GetEntityObject(t);
            if (eo == null)
                return null;
            StringBuilder sb = new StringBuilder(QueryEnum.UPDATE);
            sb.Append(eo.TableName);
            sb.Append(QueryEnum.SPACE);
            StringBuilder sbWhere = new StringBuilder();
            StringBuilder sbSets = new StringBuilder();
            List<PropertyInfo> properties = new List<PropertyInfo>(t.GetProperties());
            foreach (PropertyInfo p in properties)
            {
                if (p == null) continue;
                EntityColumnAttribute ec = GetEntityColumn(p);
                if (ec == null) continue;
                if (IsPrimaryKey(p))
                {
                    if (sbWhere.Length > 0)
                        sbWhere.Append(QueryEnum.AND);
                    sbWhere.Append(ec.ColumnName);
                    sbWhere.Append(QueryEnum.EQUAL);
                    sbWhere.Append(QueryEnum.PARAMETER);
                }
                if (IsIdentity(p)) continue;
                if (IsReadonly(p)) continue;
                if (sbSets.Length > 0)
                    sbSets.Append(QueryEnum.COMMA);
                else
                    sbSets.Append(QueryEnum.SET);
                sbSets.Append(ec.ColumnName);
                sbSets.Append(QueryEnum.EQUAL);
                sbSets.Append(QueryEnum.PARAMETER);
            }
            sb.Append(sbSets);
            if (sbWhere.Length > 0)
            {
                sb.Append(QueryEnum.SPACE);
                sb.Append(QueryEnum.WHERE);
                sb.Append(sbWhere);
            }
            sb.Append(QueryEnum.SEMI_COLON);
            return sb.ToString();
        }

        internal static string GetDeleteStatement(Type t)
        {
            if (IsSoftDeleteType(t))
                return GetSoftDeleteStatement(t);
            return GetHardDeleteStatement(t);
        }

        private static string GetHardDeleteStatement(Type t)
        {
            EntityObjectAttribute eo = GetEntityObject(t);
            if (eo == null)
                return null;
            StringBuilder sb = new StringBuilder(QueryEnum.DELETE);
            sb.Append(eo.TableName);
            sb.Append(QueryEnum.SPACE);
            List<PropertyInfo> pks = GetPrimaryKeys(t);
            if (pks != null && pks.Count > 0)
            {
                sb.Append(QueryEnum.WHERE);
                bool needsAnd = false;
                foreach (PropertyInfo pk in pks)
                {
                    EntityColumnAttribute ec = GetEntityColumn(pk);
                    if(ec == null) continue;
                    if (needsAnd)
                        sb.Append(QueryEnum.AND);
                    sb.Append(ec.ColumnName);
                    sb.Append(QueryEnum.EQUAL);
                    sb.Append(QueryEnum.PARAMETER);
                    needsAnd = true;
                }
            }
            sb.Append(QueryEnum.SEMI_COLON);
            return sb.ToString();
        }

        private static string GetSoftDeleteStatement(Type t)
        {
            EntityObjectAttribute eo = GetEntityObject(t);
            if (eo == null)
                return null;
            StringBuilder sb = new StringBuilder(QueryEnum.UPDATE);
            sb.Append(eo.TableName);
            List<PropertyInfo> props = new List<PropertyInfo>(t.GetProperties());
            StringBuilder sbSets = new StringBuilder();
            StringBuilder sbWhere = new StringBuilder();
            foreach (PropertyInfo prop in props)
            {
                EntityColumnAttribute ec;
                SoftDeleteColumnAttribute sdc = null;
                if (prop == null || (ec = GetEntityColumn(prop)) == null) continue;

                sdc = GetSoftDeleteColumn(prop);
                if (sdc != null && !IsIdentity(prop) && !IsReadonly(prop) && !IsPrimaryKey(prop))
                {
                    if (sbSets.Length > 0)
                        sbSets.Append(QueryEnum.COMMA);
                    sbSets.Append(ec.ColumnName);
                    sbSets.Append(QueryEnum.EQUAL);
                    sbSets.Append(QueryEnum.PARAMETER);
                }
                if (IsPrimaryKey(prop))
                {
                    if (sbWhere.Length > 0)
                        sbWhere.Append(QueryEnum.AND);
                    sbSets.Append(ec.ColumnName);
                    sbSets.Append(QueryEnum.EQUAL);
                    sbSets.Append(QueryEnum.PARAMETER);
                }
            }
            if (sbSets.Length == 0) return null;
            sb.Append(QueryEnum.SET);
            sb.Append(sbSets);
            if (sbWhere.Length > 0)
            {
                sb.Append(QueryEnum.WHERE);
                sb.Append(sbWhere);
            }
            sb.Append(QueryEnum.SEMI_COLON);
            return sb.ToString();
        }

        internal static string GetSelectStatement(Type t)
        {
            EntityObjectAttribute eo = GetEntityObject(t);
            if (eo == null) return null;

            StringBuilder sb = new StringBuilder();
            sb.Append(QueryEnum.SELECT);
            StringBuilder cols = new StringBuilder();
            List<PropertyInfo> pi = new List<PropertyInfo>(t.GetProperties());
            if (pi != null && pi.Count > 0)
            {
                foreach (PropertyInfo p in pi)
                {
                    if (p == null) continue;
                    EntityColumnAttribute ec = GetEntityColumn(p);
                    if (ec == null) continue;

                    if (cols.Length > 0)
                        cols.Append(QueryEnum.COMMA);
                    cols.Append(ec.ColumnName);
                }
            }
            sb.Append(cols);
            sb.Append(QueryEnum.SPACE);
            sb.Append(QueryEnum.FROM);
            sb.Append(eo.TableName);
            return sb.ToString();
        }

        internal static OleDbType ConvertFromDbType(DbType t)
        {
            switch (t)
            {
                case DbType.AnsiString:
                    return OleDbType.VarChar;
                case DbType.AnsiStringFixedLength:
                    return OleDbType.Char;
                case DbType.Binary:
                    return OleDbType.VarBinary;
                case DbType.Boolean:
                    return OleDbType.Boolean;
                case DbType.Byte:
                    return OleDbType.UnsignedTinyInt;
                case DbType.Currency:
                    return OleDbType.Currency;
                case DbType.Date:
                    return OleDbType.DBDate;
                case DbType.DateTime:
                    return OleDbType.DBTimeStamp;
                case DbType.DateTime2:
                    return OleDbType.DBTimeStamp;
                case DbType.Decimal:
                    return OleDbType.Decimal;
                case DbType.Double:
                    return OleDbType.Double;
                case DbType.Guid:
                    return OleDbType.Guid;
                case DbType.Int16:
                    return OleDbType.SmallInt;
                case DbType.Int32:
                    return OleDbType.Integer;
                case DbType.Int64:
                    return OleDbType.BigInt;
                case DbType.String:
                    return OleDbType.VarWChar;
                case DbType.StringFixedLength:
                    return OleDbType.WChar;
                case DbType.Time:
                    return OleDbType.DBTime;
                case DbType.UInt16:
                    return OleDbType.UnsignedSmallInt;
                case DbType.UInt32:
                    return OleDbType.UnsignedInt;
                case DbType.UInt64:
                    return OleDbType.UnsignedBigInt;
                case DbType.SByte:
                    return OleDbType.TinyInt;
                case DbType.VarNumeric:
                    return OleDbType.Numeric;
                case DbType.Xml:
                    return OleDbType.BSTR;
                default:
                    return OleDbType.Variant;
            }
        }

        internal static DynamicMethod GenerateInsert(Type t)
        {
            if(t == null) return null;
            EntityObjectAttribute eo = GetEntityObject(t);
            if(eo == null)
                return null;
            List<PropertyInfo> properties = new List<PropertyInfo>(t.GetProperties());
            if(properties == null || properties.Count == 0)
                return null;

            Type tOleDbConnection = typeof(OleDbConnection);
            Type tOleDbCommand = typeof(OleDbCommand);
            Type tType = typeof(Type);
            Type tEngine = typeof(NORM.Engine);
            Type tString = typeof(string);
            Type tOleDbParameterCollection = typeof(OleDbParameterCollection);
            Type tOleDbType = typeof(OleDbType);
            Type tOleDbParameter = typeof(OleDbParameter);
            Type tDecimal = typeof(decimal);

            // Constructors
            ConstructorInfo oleDbConnectionCtor = t.GetConstructor(new Type[] { typeof(string) });

            // Type methods
            MethodInfo getType = tType.GetMethod("GetTypeFromHandle");
            MethodInfo open = tOleDbConnection.GetMethod("Open");
            MethodInfo close = tOleDbConnection.GetMethod("Close");
            MethodInfo createConnection = tOleDbConnection.GetMethod("CreateConnection");
            MethodInfo executeScalar = tOleDbCommand.GetMethod("ExecuteScalar");
            MethodInfo executeNonQuery = tOleDbCommand.GetMethod("ExecuteNonQuery");
            MethodInfo getInsertStatement = tEngine.GetMethod("GetInsertStatement");
            MethodInfo isNullOrEmpty = tString.GetMethod("IsNullOrEmpty");
            MethodInfo addNumber = tOleDbParameterCollection.GetMethod("Add", new Type[] { tString, tOleDbType });
            MethodInfo addString = tOleDbParameterCollection.GetMethod("Add", new Type[] { tString, tOleDbType, typeof(int) });
            MethodInfo dispose = typeof(IDisposable).GetMethod("Dispose");
            

            // Properties
            PropertyInfo commandText = tOleDbCommand.GetProperty("CommandText");
            PropertyInfo commandType = tOleDbCommand.GetProperty("CommandType");
            PropertyInfo parameters = tOleDbCommand.GetProperty("Parameters");
            PropertyInfo pValue = tOleDbParameter.GetProperty("Value");

            // Fields

            
            FieldInfo dbNull = typeof(DBNull).GetField("Value");


            DynamicMethod dm = new DynamicMethod(t.FullName.Replace(".", "_") + "_Insert",
                typeof(bool), new Type[] { t });

            ILGenerator il = dm.GetILGenerator();
            
            LocalBuilder cs_4_0001 = il.DeclareLocal(typeof(bool));

            Label IL_0056 = il.DefineLabel();
            Label IL_027e = il.DefineLabel();
            Label IL_0239 = il.DefineLabel();

            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldstr, ConfigurationManager.ConnectionStrings[eo.Connection].ConnectionString);
            il.Emit(OpCodes.Newobj, oleDbConnectionCtor);
            il.Emit(OpCodes.Stloc_0);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, open, null);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, createConnection, null);
            il.Emit(OpCodes.Stloc_1);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldtoken, t);
            il.EmitCall(OpCodes.Call, getType, null);
            il.EmitCall(OpCodes.Call, getInsertStatement, null);
            il.EmitCall(OpCodes.Callvirt, commandText.GetSetMethod(), null);
            il.Emit(OpCodes.Ldloc_1);
            il.EmitCall(OpCodes.Callvirt, commandText.GetGetMethod(), null);
            il.EmitCall(OpCodes.Call, isNullOrEmpty, null);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue_S, IL_0056);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, close, null);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_3);
            il.Emit(OpCodes.Leave, IL_027e);
            il.MarkLabel(IL_0056);
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldc_I4_1);
            il.EmitCall(OpCodes.Callvirt, commandType.GetSetMethod(), null);

            foreach(PropertyInfo prop in properties)
            {
                EntityColumnAttribute ec = GetEntityColumn(prop);
                if(ec == null) continue;
                if(IsReadonly(prop)) continue;
                if(IsIdentity(prop)) continue;

                Label IL_008d = il.DefineLabel();

                il.Emit(OpCodes.Ldloc_1);
                il.EmitCall(OpCodes.Callvirt, parameters.GetGetMethod(), null);
                il.Emit(OpCodes.Ldstr, QueryEnum.AT + ec.ColumnName);
                il.Emit(OpCodes.Ldc_I4, (int)ConvertFromDbType(ec.DbType));
                if (ec.DbType == DbType.AnsiStringFixedLength ||
                    ec.DbType == DbType.AnsiString ||
                    ec.DbType == DbType.Binary ||
                    ec.DbType == DbType.Object ||
                    ec.DbType == DbType.DateTimeOffset ||
                    ec.DbType == DbType.VarNumeric ||
                    ec.DbType == DbType.String ||
                    ec.DbType == DbType.StringFixedLength
                )
                {
                    il.Emit(OpCodes.Ldc_I4_S, ec.Size);
                    il.EmitCall(OpCodes.Callvirt, addString, null);
                }
                else
                    il.EmitCall(OpCodes.Callvirt, addNumber, null);
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCall(OpCodes.Callvirt, prop.GetGetMethod(), null);
                if(prop.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, prop.PropertyType);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue, IL_008d);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldsfld, dbNull);
                il.MarkLabel(IL_008d);
                il.EmitCall(OpCodes.Callvirt, pValue.GetSetMethod(), null);
            }

            PropertyInfo identity = GetIdentityProperty(t);
            if (identity == null)
            {
                Label afterevals = il.DefineLabel();
                il.Emit(OpCodes.Ldloc_1);
                il.EmitCall(OpCodes.Callvirt, executeNonQuery, null);
                il.Emit(OpCodes.Stloc_S, 4);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Stloc_3);
                il.Emit(OpCodes.Ldloc_3);
                il.Emit(OpCodes.Brtrue, afterevals);
                il.Emit(OpCodes.Ldloc_S, 4);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Cgt);
                il.Emit(OpCodes.Stloc_3);
                il.MarkLabel(afterevals);
            }
            else
            {
                il.Emit(OpCodes.Ldloc_1);
                il.EmitCall(OpCodes.Callvirt, executeScalar, null);
                il.Emit(OpCodes.Stloc_2);
                il.Emit(OpCodes.Ldloc_2);
                il.Emit(OpCodes.Ldsfld, dbNull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Stloc_S, cs_4_0001);
                il.Emit(OpCodes.Ldloc_S, cs_4_0001);
                il.Emit(OpCodes.Brtrue_S, IL_0239);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc_3);
                il.Emit(OpCodes.Leave, IL_027e);
                il.MarkLabel(IL_0239);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_2);
                il.Emit(OpCodes.Unbox_Any, typeof(decimal));
                IEnumerable<MethodInfo> casters = tDecimal.GetMethods().Where(mi => mi.Name.Equals("op_Explicit") && mi.ReturnType == identity.PropertyType && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == tDecimal);
                MethodInfo op_Explicit = casters.Count<MethodInfo>() > 0 ? casters.First<MethodInfo>() : null;
                ConstructorInfo identity_const = identity.PropertyType.GetConstructor(new Type[] { identity.PropertyType });
                il.EmitCall(OpCodes.Call, op_Explicit, null);
                il.Emit(OpCodes.Newobj, identity_const);
                il.EmitCall(OpCodes.Callvirt, identity.GetSetMethod(), null);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc_3);
                il.Emit(OpCodes.Leave, IL_027e);
            }
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            Label IL_0263 = il.DefineLabel();
            il.Emit(OpCodes.Brtrue_S, IL_0263);
            il.Emit(OpCodes.Ldloc_1);
            il.EmitCall(OpCodes.Callvirt, dispose, null);
            il.Emit(OpCodes.Endfinally);
            il.EndExceptionBlock();
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            Label IL_0277 = il.DefineLabel();
            il.Emit(OpCodes.Brtrue_S, IL_0263);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, dispose, null);
            il.Emit(OpCodes.Endfinally);
            il.BeginCatchBlock(typeof(object));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_3);
            il.Emit(OpCodes.Leave_S, IL_027e);
            il.EndExceptionBlock();
            il.MarkLabel(IL_027e);
            il.Emit(OpCodes.Ldloc_3);
            il.Emit(OpCodes.Ret);
            return dm;
        }

        internal static DynamicMethod GenerateListInsert(Type t)
        {
            if (t == null) return null;
            if (!typeof(IList<>).IsAssignableFrom(t)) return null;

            Type actualType = t.GetGenericArguments()[0];
            
            EntityObjectAttribute eo = GetEntityObject(actualType);
            if (eo == null)
                return null;
            List<PropertyInfo> properties = new List<PropertyInfo>(actualType.GetProperties());

            PropertyInfo identity = GetIdentityProperty(actualType);
            if (properties == null || properties.Count == 0)
                return null;

            Type oleDbConnection = typeof(OleDbConnection);
            Type tString = typeof(string);
            Type tTypeHelper = typeof(TypeHelper);
            Type tType = typeof(Type);
            Type tDbCommand = typeof(System.Data.Common.DbCommand);
            Type tOleDbType = typeof(OleDbType);
            Type tOleDbParameter = typeof(OleDbParameter);
            Type tOleDbParameterCollection = typeof(OleDbParameterCollection);
            Type tIEnumerableOfT = typeof(IEnumerable<>).MakeGenericType(actualType);
            Type tIEnumeratorOfT = typeof(IEnumerator<>).MakeGenericType(actualType);
            Type tDbParameter = typeof(System.Data.Common.DbParameter);
            Type tObject = typeof(object);
            Type tDecimal = typeof(decimal);
            Type tBool = typeof(bool);
            Type tIEnumerator = typeof(IEnumerator);
            Type tIDisposable = typeof(IDisposable);
            Type tOleDbCommand = typeof(OleDbCommand);

            
            FieldInfo dbNull = typeof(DBNull).GetField("Value");

            // Method Info
            MethodInfo createCommand = oleDbConnection.GetMethod("CreateCommand");
            MethodInfo open = oleDbConnection.GetMethod("Open");
            MethodInfo close = oleDbConnection.GetMethod("Close");
            MethodInfo beginTran = oleDbConnection.GetMethod("BeginTransaction");
            MethodInfo rollback = oleDbConnection.GetMethod("RollbackTransaction");
            MethodInfo commit = oleDbConnection.GetMethod("CommitTransaction");
            MethodInfo getTypeFromHandler = tType.GetMethod("GetTypeFromHandler");
            MethodInfo getInsertStatement = tTypeHelper.GetMethod("GetInsertStatement");
            MethodInfo setCommandText = tDbCommand.GetProperty("CommandText").GetSetMethod();
            MethodInfo prepare = tDbCommand.GetMethod("Prepare");
            MethodInfo setCommandType = tDbCommand.GetProperty("CommandType").GetSetMethod();
            MethodInfo setCommandTimeout = tDbCommand.GetProperty("CommandTimeout").GetSetMethod();
            MethodInfo setTransaction = tDbCommand.GetProperty("Transaction").GetSetMethod();
            MethodInfo getParameters = tDbCommand.GetProperty("Parameters").GetGetMethod();
            MethodInfo addNumber = tOleDbParameterCollection.GetMethod("Add", new Type[] { tString, tOleDbType });
            MethodInfo addString = tOleDbParameterCollection.GetMethod("Add", new Type[] { tString, tOleDbType, typeof(int) });
            MethodInfo getEnumerator = tIEnumerableOfT.GetMethod("GetEnumerator");
            MethodInfo getCurrent = tIEnumeratorOfT.GetMethod("GetCurrent");
            MethodInfo getItem = tOleDbParameter.GetMethod("get_Item", new Type[] { tString });
            MethodInfo setValue = tDbParameter.GetMethod("set_Value", new Type[] { tObject });
            MethodInfo executeScalar = tDbCommand.GetMethod("ExecuteScalar");
            MethodInfo opExplicit = tDecimal.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(c => c.Name == "op_Expliti" && c.GetParameters().Count() == 0 && c.GetParameters()[0].ParameterType == tDecimal && c.ReturnType == typeof(int)).First();
            MethodInfo moveNext = tIEnumerator.GetMethod("MoveNext");
            MethodInfo dispose = tIDisposable.GetMethod("Dispose");
            MethodInfo cmdClose = tOleDbCommand.GetMethod("Close");

            // Constructors
            ConstructorInfo oleDbConnectionCtor = oleDbConnection.GetConstructor(new Type[] { tString });


            DynamicMethod dm = new DynamicMethod(t.FullName.Replace(".", "_") + "_Insert", tBool, new Type[] { typeof(IList<>).MakeGenericType(t) });

            ILGenerator il = dm.GetILGenerator();

            LocalBuilder cs_1_0000 = il.DeclareLocal(tBool);
            LocalBuilder cs_4_0001 = il.DeclareLocal(tBool);
            LocalBuilder cs_5_0002 = il.DeclareLocal(tIEnumeratorOfT);
            LocalBuilder result = il.DeclareLocal(tObject);

            Label IL_0060 = il.DefineLabel();
            Label IL_014D = il.DefineLabel();
            Label IL_0168 = il.DefineLabel();
            Label IL_031F = il.DefineLabel();
            Label IL_0338 = il.DefineLabel();
            Label IL_035D = il.DefineLabel();
            Label IL_035E = il.DefineLabel();
            Label IL_0383 = il.DefineLabel();
            Label IL_03B9 = il.DefineLabel();
            Label IL_03C1 = il.DefineLabel();

            il.Emit(OpCodes.Ldstr, ConfigurationManager.ConnectionStrings[eo.Connection].ConnectionString);
            il.Emit(OpCodes.Newobj, oleDbConnectionCtor);
            il.Emit(OpCodes.Stloc_0);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, open, null);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, beginTran, null);
            il.Emit(OpCodes.Stloc_1);
            il.BeginExceptionBlock();
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, createCommand, null);
            il.Emit(OpCodes.Stloc_2);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldtoken, t);
            il.EmitCall(OpCodes.Call, getTypeFromHandler, null);
            il.EmitCall(OpCodes.Call, getInsertStatement, null);
            il.EmitCall(OpCodes.Callvirt, setCommandText, null);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue_S, IL_0060);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, close, null);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_S, cs_1_0000);
            il.Emit(OpCodes.Leave, IL_03C1);
            il.MarkLabel(IL_0060);
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldc_I4_S, 60);
            il.EmitCall(OpCodes.Callvirt, setCommandTimeout, null);
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldloc_1);
            il.EmitCall(OpCodes.Callvirt, setTransaction, null);
            il.Emit(OpCodes.Ldloc_2);
            il.EmitCall(OpCodes.Callvirt, setCommandType, null);
            for (int i = 0; i < properties.Count; i++)
            {
                PropertyInfo prop = properties[i];
                EntityColumnAttribute ec = GetEntityColumn(prop);
                if (ec == null) continue;
                if (IsReadonly(prop)) continue;
                if (IsIdentity(prop)) continue;

                if (i > 0)
                    il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldloc_2);
                il.EmitCall(OpCodes.Callvirt, getParameters, null);
                il.Emit(OpCodes.Ldstr, QueryEnum.AT + ec.ColumnName);
                il.Emit(OpCodes.Ldc_I4, (int)ConvertFromDbType(ec.DbType));
                if (ec.DbType == DbType.AnsiStringFixedLength ||
                    ec.DbType == DbType.AnsiString ||
                    ec.DbType == DbType.Binary ||
                    ec.DbType == DbType.Object ||
                    ec.DbType == DbType.DateTimeOffset ||
                    ec.DbType == DbType.VarNumeric ||
                    ec.DbType == DbType.String ||
                    ec.DbType == DbType.StringFixedLength
                )
                {
                    il.Emit(OpCodes.Ldc_I4_S, ec.Size);
                    il.EmitCall(OpCodes.Callvirt, addString, null);
                }
                else
                    il.EmitCall(OpCodes.Callvirt, addNumber, null);
            }

            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldloc_2);
            il.EmitCall(OpCodes.Callvirt, prepare, null);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Callvirt, getEnumerator, null);
            il.Emit(OpCodes.Stloc_S, cs_5_0002);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Br, IL_0338);
            il.MarkLabel(IL_014D);
            il.Emit(OpCodes.Ldloc_S, cs_5_0002);
            il.EmitCall(OpCodes.Callvirt, getCurrent, null);
            il.Emit(OpCodes.Stloc_3);
            il.Emit(OpCodes.Ldloc_3);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue_S, IL_0168);
            il.Emit(OpCodes.Br, IL_0338);
            il.MarkLabel(IL_0168);
            foreach (PropertyInfo prop in properties)
            {
                Label IL_0187 = il.DefineLabel();
                EntityColumnAttribute ec = GetEntityColumn(prop);
                il.Emit(OpCodes.Ldloc_2);
                il.EmitCall(OpCodes.Callvirt, getParameters, null);
                il.Emit(OpCodes.Ldstr, QueryEnum.AT + ec.ColumnName);
                il.EmitCall(OpCodes.Callvirt, getItem, null);
                il.Emit(OpCodes.Ldloc_3);
                il.EmitCall(OpCodes.Callvirt, prop.GetGetMethod(), null);

                // WARNING: This might better if you just check if it's a value type
                // or reference type
                if (!(ec.DbType == DbType.AnsiStringFixedLength ||
                    ec.DbType == DbType.AnsiString ||
                    ec.DbType == DbType.Binary ||
                    ec.DbType == DbType.Object ||
                    ec.DbType == DbType.DateTimeOffset ||
                    ec.DbType == DbType.VarNumeric ||
                    ec.DbType == DbType.String ||
                    ec.DbType == DbType.StringFixedLength))
                {
                    il.Emit(OpCodes.Box, prop.PropertyType);
                }
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, IL_0187);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldsfld, dbNull);
                il.MarkLabel(IL_0187);
                il.EmitCall(OpCodes.Callvirt, setValue, null);
            }
            il.Emit(OpCodes.Ldloc_2);
            il.EmitCall(OpCodes.Callvirt, executeScalar, null); // WARNING: WHAT-IF NO IDENTITY
            il.Emit(OpCodes.Stloc_S, result);
            il.Emit(OpCodes.Ldloc_S, result);
            il.Emit(OpCodes.Ldsfld, dbNull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue_S, IL_031F);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_S, cs_1_0000);
            il.Emit(OpCodes.Leave, IL_03C1);
            il.MarkLabel(IL_031F);
            il.Emit(OpCodes.Ldloc_3); // From here onwards it assumes you have an identity
            il.Emit(OpCodes.Ldloc_S, result);
            il.Emit(OpCodes.Unbox_Any, typeof(decimal));
            il.EmitCall(OpCodes.Call, opExplicit, null);
            il.Emit(OpCodes.Newobj, identity.PropertyType.GetConstructor(new Type[] { }));
            il.EmitCall(OpCodes.Callvirt, identity.GetSetMethod(), null);
            il.MarkLabel(IL_0338);
            il.Emit(OpCodes.Ldloc_S, cs_5_0002);
            il.EmitCall(OpCodes.Callvirt, moveNext, null);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue, IL_014D);
            il.Emit(OpCodes.Leave_S, IL_035E);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc_S, cs_5_0002);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue_S, IL_035D);
            il.Emit(OpCodes.Ldloc_S, cs_5_0002);
            il.EmitCall(OpCodes.Callvirt, dispose, null);
            il.Emit(OpCodes.Endfinally);
            il.EndExceptionBlock();
            il.MarkLabel(IL_035E);
            il.Emit(OpCodes.Ldloc_1);
            il.EmitCall(OpCodes.Callvirt, commit, null);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, close, null);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc_S, cs_1_0000);
            il.Emit(OpCodes.Leave_S, IL_03C1);
            il.EndExceptionBlock();
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue, IL_0383);
            il.Emit(OpCodes.Ldloc_2);
            il.EmitCall(OpCodes.Callvirt, dispose, null);
            il.Emit(OpCodes.Endfinally);
            il.EndExceptionBlock();
            il.BeginCatchBlock(tObject);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldloc_1);
            il.EmitCall(OpCodes.Callvirt, rollback, null);
            il.Emit(OpCodes.Ldloc_0);
            il.EmitCall(OpCodes.Callvirt, close, null);
            il.Emit(OpCodes.Rethrow);
            il.EndExceptionBlock();
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Callvirt, dispose);
            il.Emit(OpCodes.Endfinally);
            il.EndExceptionBlock();
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Stloc_0, cs_4_0001);
            il.Emit(OpCodes.Ldloc_S, cs_4_0001);
            il.Emit(OpCodes.Brtrue_S, IL_03B9);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Callvirt, dispose);
            il.Emit(OpCodes.Endfinally);
            il.EndExceptionBlock();
            il.BeginCatchBlock(tObject);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0, cs_1_0000);
            il.Emit(OpCodes.Leave_S, IL_03C1);
            il.EndExceptionBlock();
            il.MarkLabel(IL_03C1);
            il.Emit(OpCodes.Ldloc_S, cs_1_0000);
            il.Emit(OpCodes.Ret);
            return dm;
        }
    }
}