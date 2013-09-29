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
using System.Data;

namespace NORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EntityColumnAttribute : Attribute
    {
        #region string ColumnName
        private string _columnName;
        public string ColumnName
        {
            get { return _columnName; }
            set { _columnName = value; }
        } 
        #endregion
        #region DbType DbType
        private DbType _dbType;
        public DbType DbType
        {
            get { return _dbType; }
            set { _dbType = value; }
        } 
        #endregion
        #region int Size
        private int _size;
        public int Size
        {
            get { return _size; }
            set { _size = value; }
        } 
        #endregion

        public EntityColumnAttribute(string columnName, DbType dbType)
        {
            _columnName = columnName;
            _dbType = dbType;
            _size = 0;
        }

        public EntityColumnAttribute(string columnName, DbType dbType, int size)
        {
            _columnName = columnName;
            _dbType = dbType;
            _size = size;
        }
    }
}