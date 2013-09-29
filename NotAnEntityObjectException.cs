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

namespace NORM
{
	public sealed class NotAnEntityObjectException : Exception
    {
        public NotAnEntityObjectException() : base("Type is not an entity object") { }
        public NotAnEntityObjectException(string typeName) : base(typeName + " is not an entity object") { }
    }
}