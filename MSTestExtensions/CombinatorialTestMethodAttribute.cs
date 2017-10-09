using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Attribute for a combinatorial data test method.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Test methods marked with this attribute will be called with all combinations
    ///     of the values given by each of the combinatorial argument attributes.
    ///   </para>
    ///   <para>
    ///     To customize the values that tests are run with, you can extend the
    ///     <see cref="BaseCombinatorialArgumentAttribute"/> class.  A set of standard
    ///     combinatorial arguments attributes are already defined:
    ///   </para>
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Attribute</term>
    ///       <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///       <term><see cref="CombinatorialAttribute"/></term>
    ///       <description>Utilizes a set of given values for the argument.</description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="CombinatorialEnumAttribute"/></term>
    ///       <description>Utilizes all defined values of an enumeration for the argument.</description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="CombinatorialFactoryAttribute"/></term>
    ///       <description>Retrieves the argument's values from a static method on the test class.</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    /// <seealso cref="BaseCombinatorialArgumentAttribute"/>
    /// <seealso cref="CombinatorialAttribute"/>
    /// <seealso cref="CombinatorialEnumAttribute"/>
    /// <seealso cref="CombinatorialFactoryAttribute"/>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CombinatorialTestMethodAttribute : TestMethodAttribute
    {
        /// <summary>
        /// Executes a combinatorial test method.
        /// </summary>
        /// <param name="testMethod">The test method to execute.</param>
        /// <returns>An array of TestResult objects that represent the outcomes of the test.</returns>
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            try
            {
                ValidateAttributes(testMethod);

                object[][] args = GetArgumentsInOrder(testMethod);

                ValidateArguments(testMethod, args);

                // get the count of tests so we know how to format the # in the test name.
                int totalTestsDigits = ComputeTotalTestCountDigits(args);

                var indexes = new int[args.Length];
                bool done;

                var results = new List<TestResult>();
                do
                {
                    // run the test with the current values.
                    object[] argsToPass = Enumerable.Range(0, args.Length)
                                                    .Select(i => args[i][indexes[i]])
                                                    .ToArray();

                    TestResult result = testMethod.Invoke(argsToPass);

                    // In case the results are alphabetized (I'm looking at you
                    // VS test explorer), prepend the test # padded with 0s.
                    string number = (results.Count + 1).ToString().PadLeft(totalTestsDigits, '0');
                    result.DisplayName = $"#{number}: {GetTestDisplayName(testMethod, argsToPass)}";
                    results.Add(result);

                    // update the current indexes.
                    int updateIdx = indexes.Length - 1;

                    for (; updateIdx >= 0; updateIdx--)
                    {
                        indexes[updateIdx]++;

                        if (indexes[updateIdx] < args[updateIdx].Length)
                        {
                            // still values left for this parameter.
                            break;
                        }

                        // carry to the next slot
                        indexes[updateIdx] = 0;
                    }

                    // If we carried off the end of the array, we're done
                    done = (updateIdx < 0);

                } while (!done);

                return results.ToArray();
            }
            catch (Exception ex)
            {
                // something went wrong - fail the test with the exception info.
                // note: I believe the MSTest executor will catch any exceptions that escape
                // tests, and return a failed TestResult (if it wasn't an ExpectedException).
                return new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Error,
                        TestFailureException = ex
                    }
                };
            }
        }


        /// <summary>
        /// Validates that each of the test method parameters have a combinatorial argument attribute.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        /// <exception cref="Exception">An error is found with the attributes.</exception>
        /// <devdoc>
        /// The MSTest executor seems to do some special handling for base Exception type, where it
        /// will omit the exception type.
        /// </devdoc>
        private static void ValidateAttributes(ITestMethod testMethod)
        {
            ParameterInfo[] parameters = testMethod.MethodInfo.GetParameters();

            foreach (var parm in parameters)
            {
                if (parm.GetCustomAttribute<BaseCombinatorialArgumentAttribute>(inherit: true) == null)
                {
                    throw new Exception(
                        $"Parameter {parm.Name} is missing a {nameof(BaseCombinatorialArgumentAttribute)}."
                    );
                }
            }
        }

        /// <summary>
        /// Gets all the values for each parameter in the order they need to be passed to the function.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        /// <returns>
        /// A list of all parameters' values.  Each sub-array corresponds to a parameter, so
        /// <c>argArrays[0]</c> will yield the values to pass to the first parameter. Additionally,
        /// <c>argArrays[0][0]</c> will yield the first value to pass to the first parameter.
        /// </returns>
        private static object[][] GetArgumentsInOrder(ITestMethod testMethod)
        {
            var values = new List<object[]>();

            foreach (var p in testMethod.MethodInfo.GetParameters())
            {
                var attr = p.GetCustomAttribute<BaseCombinatorialArgumentAttribute>();
                IReadOnlyList<object> paramValues = attr.GetValues(testMethod, p);

                values.Add(paramValues.ToArray());
            }

            return values.ToArray();
        }

        /// <summary>
        /// Validates the values being passed to each parameter.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        /// <param name="arguments">The set of values for each parameter, as returned by <see cref="GetArgumentsInOrder"/>.</param>
        private static void ValidateArguments(ITestMethod testMethod, object[][] arguments)
        {
            ParameterInfo[] allParameters = testMethod.MethodInfo.GetParameters();

            for (int i = 0; i < arguments.Length; i++)
            {
                object[] values = arguments[i];
                ParameterInfo parameter = allParameters[i];

                for (int j = 0; j < values.Length; j++)
                {
                    object v = values[i];

                    if (!parameter.ParameterType.GetTypeInfo().IsAssignableFrom(v.GetType()))
                    {
                        throw new ArgumentException(
                            $"Argument value ({GetValueString(v)}) has a type that is incompatible with parameter {parameter.Name}."
                        );
                    }
                }
            }
        }


        /// <summary>
        /// Creates a name for the test as run using the given arguments.
        /// </summary>
        /// <param name="method">The test method.</param>
        /// <param name="arguments">The argument values.</param>
        /// <returns>A name for the test as run with the given arguments.</returns>
        private static string GetTestDisplayName(ITestMethod method, object[] arguments)
        {
            StringBuilder name = new StringBuilder(method.TestMethodName);
            name.Append('(');

            bool first = true;

            foreach (var value in arguments)
            {
                if (!first)
                {
                    name.Append(", ");
                }

                name.Append(GetValueString(value));

                first = false;
            }

            name.Append(')');
            return name.ToString();
        }

        /// <summary>
        /// Converts the given value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The string form of the given value.</returns>
        private static string GetValueString(object value)
        {
            switch (value)
            {
                case null:
                    return "null";

                case string str:
                    return "\"" + str + "\"";

                case object enumVal
                when enumVal.GetType().GetTypeInfo().IsEnum:
                    return enumVal.GetType().Name + "." + value;

                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Computes the total number of digits in the test count.
        /// Used to figure out how much to pad the test # for <see cref="TestResult.DisplayName"/>.
        /// </summary>
        /// <param name="arguments">The set of values for each parameter, as returned by <see cref="GetArgumentsInOrder"/>.</param>
        /// <returns>The number of digits in the test count.</returns>
        private static int ComputeTotalTestCountDigits(object[][] arguments)
        {
            try
            {
                long totalTests = 1;

                checked
                {
                    foreach (object[] list in arguments)
                    {
                        totalTests *= list.Length;
                    }
                }

                return (int)Math.Ceiling(Math.Log10(totalTests));
            }
            catch (OverflowException)
            {
                return 1;
            }
        }
    }
}
