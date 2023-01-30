#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionCore.Structure;
using HularionMesh.Domain;
using HularionMesh.MeshType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HularionMesh
{
    /// <summary>
    /// Evaluates leaf where nodes against a provided real value.
    /// </summary>
    public static class WhereComparisonEvaluator
    {
        private static Type int8Type = typeof(byte);
        private static Type int16Type = typeof(short);
        private static Type int32Type = typeof(int);
        private static Type int64Type = typeof(long);
        private static Type floatType = typeof(float);
        private static Type doubleType = typeof(double);
        private static Type decimalType = typeof(decimal);
        private static Type boolType = typeof(bool);
        private static Type stringType = typeof(string);
        private static Type keyType = typeof(IMeshKey);

        private static Table<Type, Type> equalTable = new Table<Type, Type>();
        private static Table<Type, Type> notEqualTable = new Table<Type, Type>();
        private static Table<Type, Type> greaterThanTable = new Table<Type, Type>();
        private static Table<Type, Type> greaterThanOrEqualToTable = new Table<Type, Type>();
        private static Table<Type, Type> lessThanTable = new Table<Type, Type>();
        private static Table<Type, Type> lessThanOrEqualToTable = new Table<Type, Type>();
        private static Table<Type, Type> likeTable = new Table<Type, Type>();
        private static Table<Type, Type> inTable = new Table<Type, Type>();

        private static Dictionary<string, Regex> likeRegex = new Dictionary<string, Regex>();

        private static HashSet<Type> keyTypes = new HashSet<Type>(new Type[] { keyType });

        static WhereComparisonEvaluator()
        {
            var intTypes = new Type[] { int8Type, int16Type, int32Type, int64Type };
            var floatTypes = new Type[] { floatType, doubleType };

            foreach (var type1 in intTypes)
            {
                foreach (var type2 in intTypes)
                {
                    equalTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => x == y));
                    notEqualTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => x != y));
                    greaterThanTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (long)x > (long)y));
                    greaterThanOrEqualToTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (long)x >= (long)y));
                    lessThanTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (long)x < (long)y));
                    lessThanOrEqualToTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (long)x <= (long)y));
                    //Setup the In collection as a hash set prior to call.
                    inTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => ((HashSet<long>)x).Contains((long)y)));
                }
            }

            foreach (var type1 in floatTypes)
            {
                foreach (var type2 in floatTypes)
                {
                    equalTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => x == y));
                    notEqualTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => x != y));
                    greaterThanTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x > (double)y));
                    greaterThanOrEqualToTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x >= (double)y));
                    lessThanTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x < (double)y));
                    lessThanOrEqualToTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x <= (double)y));
                    //Setup the In collection as a hash set prior to call.
                    inTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => ((HashSet<double>)x).Contains((double)y)));
                }
            }

            foreach (var type1 in intTypes)
            {
                foreach (var type2 in floatTypes)
                {
                    equalTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => x == y));
                    equalTable.SetValue(type2, type1, new Func<object, object, bool>((x, y) => x == y));
                    notEqualTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => x != y));
                    notEqualTable.SetValue(type2, type1, new Func<object, object, bool>((x, y) => x != y));
                    greaterThanTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x > (double)y));
                    greaterThanTable.SetValue(type2, type1, new Func<object, object, bool>((x, y) => (double)x > (double)y));
                    greaterThanOrEqualToTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x >= (double)y));
                    greaterThanOrEqualToTable.SetValue(type2, type1, new Func<object, object, bool>((x, y) => (double)x >= (double)y));
                    lessThanTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x < (double)y));
                    lessThanTable.SetValue(type2, type1, new Func<object, object, bool>((x, y) => (double)x < (double)y));
                    lessThanOrEqualToTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => (double)x <= (double)y));
                    lessThanOrEqualToTable.SetValue(type2, type1, new Func<object, object, bool>((x, y) => (double)x <= (double)y));
                    //Setup the In collection as a hash set prior to call.
                    inTable.SetValue(type1, type2, new Func<object, object, bool>((x, y) => ((HashSet<double>)x).Contains((double)y)));
                    inTable.SetValue(type2, type1, new Func<object, object, bool>((x, y) => ((HashSet<double>)x).Contains((double)y)));
                }
            }

            foreach (var type in intTypes)
            {
                equalTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((long)x) == (decimal)y));
                equalTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x == new decimal((long)y)));
                notEqualTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((long)x) != (decimal)y));
                notEqualTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x != new decimal((long)y)));
                greaterThanTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((long)x) > (decimal)y));
                greaterThanTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x > new decimal((long)y)));
                greaterThanOrEqualToTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((long)x) >= (decimal)y));
                greaterThanOrEqualToTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x >= new decimal((long)y)));
                lessThanTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((long)x) < (decimal)y));
                lessThanTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x < new decimal((long)y)));
                lessThanOrEqualToTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((long)x) <= (decimal)y));
                lessThanOrEqualToTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x <= new decimal((long)y)));
                //Setup the In collection as a hash set prior to call.
                inTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => ((HashSet<decimal>)x).Contains((decimal)y)));
                inTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => ((HashSet<decimal>)y).Contains((decimal)x)));
            }

            foreach (var type in floatTypes)
            {
                equalTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((double)x) == (decimal)y));
                equalTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x == new decimal((double)y)));
                notEqualTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((double)x) != (decimal)y));
                notEqualTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x != new decimal((double)y)));
                greaterThanTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((double)x) > (decimal)y));
                greaterThanTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x > new decimal((double)y)));
                greaterThanOrEqualToTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((double)x) >= (decimal)y));
                greaterThanOrEqualToTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x >= new decimal((double)y)));
                lessThanTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((double)x) < (decimal)y));
                lessThanTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x < new decimal((double)y)));
                lessThanOrEqualToTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => new decimal((double)x) <= (decimal)y));
                lessThanOrEqualToTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => (decimal)x <= new decimal((double)y)));
                //Setup the In collection as a hash set prior to call.
                inTable.SetValue(type, decimalType, new Func<object, object, bool>((x, y) => ((HashSet<decimal>)x).Contains((decimal)y)));
                inTable.SetValue(decimalType, type, new Func<object, object, bool>((x, y) => ((HashSet<decimal>)y).Contains((decimal)x)));
            }

            equalTable.SetValue(stringType, stringType, new Func<object, object, bool>((x, y) => String.Equals(x,y)));
            notEqualTable.SetValue(stringType, stringType, new Func<object, object, bool>((x, y) => !String.Equals(x, y)));
            //TODO: Setup a regular expression to deal with like
            likeTable.SetValue(stringType, stringType, new Func<object, object, bool>((x, y) =>
            {
                if (!likeRegex.ContainsKey((string)x))
                {
                    lock (likeRegex)
                    {
                        if (!likeRegex.ContainsKey((string)x))
                        {
                            var z = new StringBuilder();
                            z.Append("^");
                            z.Append(((string)x).Replace("%", ".*").Replace('_', '.'));
                            z.Append("$");
                            likeRegex.Add((string)x, new Regex(z.ToString()));
                        }
                    }
                }
                return likeRegex[(string)x].Match((string)y).Length > 0;
            }));
            inTable.SetValue(stringType, stringType, new Func<object, object, bool>((x, y) => ((HashSet<object>)x).Contains(y)));


            equalTable.SetValue(keyType, keyType, new Func<object, object, bool>((x, y) => x == y));
            notEqualTable.SetValue(keyType, keyType, new Func<object, object, bool>((x, y) => x != y)); 
            inTable.SetValue(keyType, keyType, new Func<object, object, bool>((x, y) => ((HashSet<object>)x).Contains(y)));

        }

        /// <summary>
        /// Evaluates the node using its comparison and value.
        /// </summary>
        /// <param name="node">The node to evaluate.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The result of the evaluation.</returns>
        public static bool EvaluateComparison(WhereExpressionNode node, object value)
        {


            if(value == null) { return node.Negated; }
            bool result = false;
            var actual = node.Value;
            if(node.ValueProvider != null) { actual = node.ValueProvider.Provide(); }
            if (node.Comparison == DataTypeComparison.In)
            {
                var valueType = node.Value.GetType();
                if (typeof(IEnumerable<byte>).IsAssignableFrom(valueType)) { actual = new HashSet<long>(((IEnumerable<byte>)node.Value).Select(x => (long)x)); }
                if (typeof(IEnumerable<short>).IsAssignableFrom(valueType)) { actual = new HashSet<long>(((IEnumerable<short>)node.Value).Select(x => (long)x)); }
                if (typeof(IEnumerable<int>).IsAssignableFrom(valueType)) { actual = new HashSet<long>(((IEnumerable<int>)node.Value).Select(x => (long)x)); }
                if (typeof(IEnumerable<long>).IsAssignableFrom(valueType)) { actual = new HashSet<long>((IEnumerable<long>)node.Value); }
                if (typeof(IEnumerable<float>).IsAssignableFrom(valueType)) { actual = new HashSet<double>(((IEnumerable<float>)node.Value).Select(x => (double)x)); }
                if (typeof(IEnumerable<double>).IsAssignableFrom(valueType)) { actual = new HashSet<double>(((IEnumerable<double>)node.Value).Select(x => (double)x)); }
                if (node.Mode == WhereExpressionNodeValueMode.Key && valueType == typeof(object[])) { actual = new HashSet<IMeshKey>(((IEnumerable<object>)node.Value).Select(x => MeshKey.Parse(x.ToString()))); }
            }

            switch (node.Comparison)
            {
                case DataTypeComparison.Equal:
                    if (actual == null || value == null) { result = (actual == value); break; }
                    result = equalTable.GetValue<Func<object, object, bool>>(actual.GetType(), value.GetType())(actual, value);
                    break;
                case DataTypeComparison.NotEqual:
                    if(actual == null || value == null) { result = (actual != value); break; }
                    result = notEqualTable.GetValue<Func<object, object, bool>>(actual.GetType(), value.GetType())(actual, value);
                    break;
                case DataTypeComparison.GreaterThan:
                    if (actual == null || value == null) { result = false; break; }
                    result = greaterThanTable.GetValue<Func<object, object, bool>>(actual.GetType(), value.GetType())(actual, value);
                    break;
                case DataTypeComparison.GreaterThanOrEqualTo:
                    if (actual == null || value == null) { result = false; break; }
                    result = greaterThanOrEqualToTable.GetValue<Func<object, object, bool>>(actual.GetType(), value.GetType())(actual, value);
                    break;
                case DataTypeComparison.LessThan:
                    if (actual == null || value == null) { result = false; break; }
                    result = lessThanTable.GetValue<Func<object, object, bool>>(actual.GetType(), value.GetType())(actual, value);
                    break;
                case DataTypeComparison.LessThanOrEqualTo:
                    if (actual == null || value == null) { result = false; break; }
                    result = lessThanOrEqualToTable.GetValue<Func<object, object, bool>>(actual.GetType(), value.GetType())(actual, value);
                    break;
                case DataTypeComparison.Like:
                    if (actual == null || value == null) { result = false; break; }
                    result = likeTable.GetValue<Func<object, object, bool>>(actual.GetType(), value.GetType())(actual, value);
                    break;
                case DataTypeComparison.In:
                    if (actual == null || value == null) { result = false; break; }
                    var actualType = actual.GetType();
                    //if (actualType.IsArray) { actualType = actualType.GetElementType(); }
                    Type argumentType;
                    HashSet<object> values;
                    if (actualType.IsArray)
                    {
                        values = new HashSet<object>((object[])actual);
                        if (values.Count() > 0) { actualType = values.First().GetType(); }
                        else { actualType = actualType.GetElementType(); }
                    }
                    else
                    {
                        values = new HashSet<object>((IEnumerable<object>)actual);
                        argumentType = actualType.GetGenericArguments()[0];
                    }
                    var valueType = value.GetType();
                    if (keyType.IsAssignableFrom(valueType))
                    {
                        result = inTable.GetValue<Func<object, object, bool>>(keyType, keyType)(values, value);
                    }
                    else
                    {
                        
                        //result = inTable.GetValue<Func<object, object, bool>>(actual.GetType(), actual.GetType())(values, value);
                        result = inTable.GetValue<Func<object, object, bool>>(actualType, value.GetType())(values, value);
                    }
                    break;
            }
            result ^= node.Negated;
            return result;
        }

    }
}
