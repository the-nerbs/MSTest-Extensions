using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Attribute used to describe a set of values to pass for an argument to a combinatorial test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class CombinatorialEnumAttribute : BaseCombinatorialArgumentAttribute
    {
        /// <summary>
        /// Constructs a new instance of <see cref="CombinatorialEnumAttribute"/>.
        /// </summary>
        public CombinatorialEnumAttribute()
        { }


        /// <inheritdoc />
        /// <see cref="BaseCombinatorialArgumentAttribute.GetValues"/>
        public override IReadOnlyList<object> GetValues(ITestMethod testMethod, ParameterInfo parameter)
        {
            Type enumType = parameter.ParameterType;

            if (!enumType.GetTypeInfo().IsEnum)
            {
                throw new Exception(
                    $"Parameter {parameter.Name} is not a valid target for {nameof(CombinatorialEnumAttribute)} as it does not have an enum type."
                );
            }

            return Enum.GetValues(enumType)
                       .Cast<object>()
                       .ToArray();
        }
    }
}
