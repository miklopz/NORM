using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NORM
{
    public static class Engine
    {
        internal static SortedDictionary<string, int> _objects = new SortedDictionary<string,int>();
        internal static SortedDictionary<int, string> _entityObjectName = new SortedDictionary<int, string>();
        internal static SortedDictionary<int, DynamicMethod> _readerBindings = new SortedDictionary<int, DynamicMethod>();
        internal static SortedDictionary<int, DynamicMethod> _rowBindings = new SortedDictionary<int, DynamicMethod>();
        internal static SortedDictionary<int, string> _insertStatements = new SortedDictionary<int, string>();
        internal static SortedDictionary<int, string> _updateStatements = new SortedDictionary<int, string>();
        internal static SortedDictionary<int, string> _deleteStatements = new SortedDictionary<int, string>();
        internal static SortedDictionary<int, string> _selectStatements = new SortedDictionary<int, string>();

        public static string GetInsertStatement(Type t)
        {
            if (!_objects.ContainsKey(t.FullName))
                return null;
            return _insertStatements[_objects[t.FullName]];
        }

        private static void LoadCache(Type[] tArray)
        {
            if (tArray == null || tArray.Length == 0) return;

            string eObjectName = null;
            int id = 1;

            if (tArray != null)
            {
                foreach (Type t in tArray)
                {
                    eObjectName = null;
                    eObjectName = TypeHelper.GetEntityObjectName(t);
                    if (string.IsNullOrEmpty(eObjectName) || _objects.ContainsKey(t.FullName)) continue;
                    // Global Containers
                    _objects.Add(t.FullName, id);
                    // Global Object Names
                    _entityObjectName.Add(id, eObjectName);
                    _readerBindings.Add(id, TypeHelper.GetDataReaderBindingMethod(t));
                    _rowBindings.Add(id, TypeHelper.GetDataRowBindingMethod(t));
                    _insertStatements.Add(id, TypeHelper.GetInsertStatement(t));
                    _updateStatements.Add(id, TypeHelper.GetUpdateStatement(t));
                    _deleteStatements.Add(id, TypeHelper.GetDeleteStatement(t));
                    _selectStatements.Add(id, TypeHelper.GetSelectStatement(t));
                    id++;
                }
            }

            Console.WriteLine(_insertStatements[2]);
            Console.WriteLine(_updateStatements[2]);
            Console.WriteLine(_deleteStatements[2]);
            Console.WriteLine(_selectStatements[2]);
        }

        public static void Start()
        {
            LoadCache(TypeHelper.GetTypes());
            Console.WriteLine("There are a total of {0} entity types", _objects.Count);
        }
    }
}
