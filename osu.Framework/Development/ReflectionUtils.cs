// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Development
{
    internal static class ReflectionUtils
    {
        // taken from https://github.com/nunit/nunit/blob/73dbcce0896a6897a2add4281cc48734eca546a2/src/NUnitFramework/framework/Internal/Reflect.cs
        // was removed in nunit 3.13.1

        private const BindingFlags all_members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>
        /// Returns all methods declared by the specified fixture type that have the specified attribute, optionally
        /// including base classes. Methods from a base class are always returned before methods from a class that
        /// inherits from it.
        /// </summary>
        /// <param name="fixtureType">The type to examine.</param>
        /// <param name="attributeType">Only methods to which this attribute is applied will be returned.</param>
        /// <param name="inherit">Specifies whether to search the fixture type inheritance chain.</param>
        internal static MethodInfo[] GetMethodsWithAttribute(Type fixtureType, Type attributeType, bool inherit)
        {
            if (!inherit)
            {
                return fixtureType
                       .GetMethods(all_members | BindingFlags.DeclaredOnly)
                       .Where(method => method.IsDefined(attributeType, inherit: false))
                       .ToArray();
            }

            var methodsByDeclaringType = fixtureType
                                         .GetMethods(all_members | BindingFlags.FlattenHierarchy) // FlattenHierarchy is complex to replicate by looping over base types with DeclaredOnly.
                                         .Where(method => method.IsDefined(attributeType, inherit: true))
                                         .ToLookup(method => method.DeclaringType);

            return typeAndBaseTypes(fixtureType)
                   .Reverse()
                   .SelectMany(declaringType => methodsByDeclaringType[declaringType])
                   .ToArray();
        }

        private static IEnumerable<Type> typeAndBaseTypes(Type type)
        {
            for (; type != null; type = type.GetTypeInfo().BaseType)
            {
                yield return type;
            }
        }
    }
}
