﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace NTypewriter.CodeModel.Functions
{
    /// <summary>
    /// Areas of the search
    /// </summary>
    [Flags]
    public enum SearchIn
    {
        /// <summary>
        /// Properties
        /// </summary>
        Properties = 1,
        /// <summary>
        /// Methods
        /// </summary>
        Methods = 2,
        /// <summary>
        /// Fields
        /// </summary>
        Fields = 4,
        /// <summary>
        /// BaseClass
        /// </summary>
        BaseClass = 8,
        /// <summary>
        /// All
        /// </summary>
        All = 127
    }

    /// <summary>
    /// Set of functions that operates on IType
    /// </summary>
    public static partial class TypeFunctions
    {
        /// <summary>
        /// Returns all types that are used in definition of a given type.
        /// </summary>
        public static IEnumerable<IType> AllReferencedTypes(this IType type, SearchIn searchIn = SearchIn.All)
        {
            var foundTypes = new HashSet<IType>(new TypeComparer());

            if (type is IClass @class)
            {
                if (searchIn.HasFlag(SearchIn.Properties))
                {
                    foreach (var property in @class.Properties)
                    {
                        InspectType(foundTypes, property.Type);
                    }
                }
                if (searchIn.HasFlag(SearchIn.Methods))
                {
                    foreach (var method in @class.Methods)
                    {
                        InspectType(foundTypes, method.ReturnType);
                        foreach (var parameter in method.Parameters)
                        {
                            if (IsNotFromServices(parameter))
                            {
                                InspectType(foundTypes, parameter.Type);
                            }
                        }
                    }
                }
                if (searchIn.HasFlag(SearchIn.Fields))
                {
                    foreach (var field in @class.Fields)
                    {
                        InspectType(foundTypes, field.Type);
                    }
                }
                if (searchIn.HasFlag(SearchIn.BaseClass))
                {
                    if (@class.HasBaseClass)
                    {
                        InspectType(foundTypes, @class.BaseClass);
                    }
                }
            }

            if (foundTypes.Contains(type))
            {
                foundTypes.Remove(type);
            }

            return foundTypes.ToList();
        }

        private static void InspectType(HashSet<IType> foundTypes, IType type)
        {
            if (foundTypes.Contains(type))
            {
                return;
            }
            if (type.IsPrimitive)
            {
                return;
            }
            if (type.IsGeneric)
            {
                foreach (var typeArgument in type.TypeArguments)
                {
                    InspectType(foundTypes, typeArgument);
                }
            }
            if (type.IsArray)
            {
                InspectType(foundTypes, type.ArrayType);
                return;
            }
            if (type.IsNullable)
            {
                return;
            }            
            if (type.Namespace.StartsWith("System.") || type.Namespace.StartsWith("Microsoft.") ||
                type.Namespace.Equals("System") || type.Namespace.Equals("Microsoft")
                )
            {
                return;
            }

            foundTypes.Add(type);
        }

        static bool IsNotFromServices(IParameter p)
        {
            if (p.Attributes.Any(x => x.Name == "FromServices"))
            {
                return false;
            }
            return true;
        }
    }

    class TypeComparer : IEqualityComparer<IType>
    {
        public bool Equals(IType x, IType y)
        {
            return x.FullName.Equals(y.FullName);
        }

        public int GetHashCode(IType obj)
        {
            return obj.FullName.GetHashCode();
        }
    }
}
