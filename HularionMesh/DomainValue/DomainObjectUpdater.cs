#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HularionMesh.DomainValue
{
    /// <summary>
    /// Used to update the values of domain objects.
    /// </summary>
    public class DomainObjectUpdater
    {
        /// <summary>
        /// The property values to update.
        /// </summary>
        public IDictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The meta values to update.
        /// </summary>
        public IDictionary<string, object> Meta { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The where clause used to locate the nodes to update.
        /// </summary>
        public WhereExpressionNode Where { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DomainObjectUpdater()
        {
        }

        /// <summary>
        /// Assignes this updater using the provided value.
        /// </summary>
        /// <param name="value">The value containing the update information.</param>
        public DomainObjectUpdater(DomainObject value)
        {
            this.Values = value.Values.ToDictionary(x => x.Key, x => x.Value);
            this.Meta = value.Meta.ToDictionary(x => x.Key, x => x.Value);
            Where = WhereExpressionNode.CreateKeysIn(value.Key);
        }

        /// <summary>
        /// Derives the updater from the provided value.
        /// </summary>
        /// <typeparam name="T">The type of the value from which the updater is derived.</typeparam>
        /// <param name="value">The value containing the update information.</param>
        /// <returns>The domain updater.</returns>
        public static DomainObjectUpdater Derive<T>(T value)
        {
            return new DomainObjectUpdater(DomainObject.Derive(value));
        }

    }
}
