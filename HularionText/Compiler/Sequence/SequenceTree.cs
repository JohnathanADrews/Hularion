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

namespace HularionText.Compiler.Sequence
{
    /// <summary>
    /// A tree which contains meaningful sequences of objects.<SequenceType>
    /// </summary>
    /// <typeparam name="SequenceType">The type of the objects which are ordered.  e.g. a block of characters in text.</typeparam>
    public class SequenceTree<SequenceType>
    {
        /// <summary>
        /// The name of the tree.
        /// </summary>
        public string Name { get; set; } = "(Sequence Tree Name)";

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        public SequenceTreeNode<SequenceType> Root { get; private set; } = new SequenceTreeNode<SequenceType>() { IsRoot = true };

        /// <summary>
        /// Resolves one sequence value to another. For example, a case-insensitive language may use 'E' => 'e'. 
        /// </summary>
        public Func<SequenceType, SequenceType> ValueResolver { get; set; } = new Func<SequenceType, SequenceType>(x => x);

        private Dictionary<SequenceSymbol<SequenceType>, SequenceTreeNode<SequenceType>> symbols { get; set; } = new Dictionary<SequenceSymbol<SequenceType>, SequenceTreeNode<SequenceType>>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public SequenceTree()
        {
            Root.Tree = this;
        }

        /// <summary>
        /// Adds the symbol to the tree.
        /// </summary>
        /// <param name="symbol">The symbol to add.</param>
        public SequenceTreeNode<SequenceType> AddSymbol(SequenceSymbol<SequenceType> symbol, params object[] modes)
        {
            SequenceTreeNode<SequenceType> node;
            if (symbols.ContainsKey(symbol))
            {
                node = symbols[symbol];
            }
            else 
            {
                node = Root;
                for (int i = 0; i < symbol.Sequence.Length; i++)
                {
                    var s = symbol.Sequence[i];
                    if (!node.Nodes.ContainsKey(s))
                    {
                        var newNode = new SequenceTreeNode<SequenceType>();
                        newNode.Value = s;
                        newNode.Tree = this;
                        node.Nodes.Add(s, newNode);
                    }
                    node = node.Nodes[s];
                }
                node.Symbol = symbol;
                node.IsWaypoint = true;
                symbols.Add(symbol, node);
            }
            foreach (var mode in modes) { node.SequenceModes.Add(mode); }
            return node;
        }

        /// <summary>
        /// Adds the symbols to the tree.
        /// </summary>
        /// <param name="symbols">The symbols to add.</param>
        public IDictionary<SequenceSymbol<SequenceType>, SequenceTreeNode<SequenceType>> AddSymbols(IEnumerable<SequenceSymbol<SequenceType>> symbols, IEnumerable<object> modes)
        {
            var nodes = new Dictionary<SequenceSymbol<SequenceType>, SequenceTreeNode<SequenceType>>();
            foreach (var symbol in symbols) { nodes.Add(symbol, AddSymbol(symbol, modes.ToArray())); }
            return nodes;
        }

        /// <summary>
        /// Adds the symbols to the tree.
        /// </summary>
        /// <param name="symbols">The symbols to add.</param>
        public IDictionary<SequenceSymbol<SequenceType>, SequenceTreeNode<SequenceType>> AddSymbols(params SequenceSymbol<SequenceType>[] symbols)
        {
            return AddSymbols(symbols.ToList(), new List<object>());
        }

        /// <summary>
        /// Adds the symbols to the tree.
        /// </summary>
        /// <param name="modes">The modes that determine whether the symbols are active.</param>
        /// <param name="symbols">The symbols to add.</param>
        public IDictionary<SequenceSymbol<SequenceType>, SequenceTreeNode<SequenceType>> AddSymbols(IEnumerable<object> modes, params SequenceSymbol<SequenceType>[] symbols)
        {
            return AddSymbols(symbols.ToList(), modes.ToList());
        }

        /// <summary>
        /// Gets the next matches starting at startIndex. 
        /// </summary>
        /// <param name="sequence">The sequence to find matches on.</param>
        /// <param name="startIndex">The index at which to start finding matches.</param>
        /// <param name="context">The state context of the match process.</param>
        /// <returns>The match object which contains the matches from the starting index.</returns>
        public SequenceMatch<SequenceType> GetNextMatch(SequenceType[] sequence, int startIndex, ISequenceProcessContext context)
        {
            var match = new SequenceMatch<SequenceType>();
            match.Tree = this;
            match.Sequence = sequence;
            SequenceTreeNode<SequenceType> node = null;
            SequenceTreeNode<SequenceType> matchNode = null;
            object mode = null;


            for (var i = startIndex; i < sequence.Length; i++)
            {
                mode = context.Mode;
                node = Root;
                matchNode = node;
                var j = i;
                for (; j < sequence.Length; j++)
                {
                    node = node.Next(sequence[j]);
                    if (node == null) { break; }
                    if (node.IsWaypoint && mode!= null && node.SequenceModes.Contains(mode)) { match.StartIndex = i; match.Push(node); }
                }
                if (match.HasMatch) { break; }
            }
            return match;
        }

        /// <summary>
        /// Gets the next [match], [intermediate, match], or [intermediate] depending on whether last is null and there is an intermediate.
        /// </summary>
        /// <param name="sequence">The sequence to find matches on.</param>
        /// <param name="startIndex">The index at which to start finding matches.</param>
        /// <param name="last">The last found match or null if there is no previous match.</param>
        /// <param name="context">The state context of the match process.</param>
        /// <returns>[match], [intermediate, match], or [intermediate] depending on whether last is null and there is an intermediate.</returns>
        public SequenceMatch<SequenceType>[] GetNextMatches(SequenceType[] sequence, int startIndex, SequenceMatch<SequenceType> last, ISequenceProcessContext context)
        {
            var matches = new List<SequenceMatch<SequenceType>>();
            var match = GetNextMatch(sequence, startIndex, context);

            if (match.HasMatch)
            {
                if(last == null && match.StartIndex > 0) { matches.Add(SequenceMatch<SequenceType>.CreateIntermediate(this, sequence, startIndex, match.StartIndex - startIndex)); }
                if (last != null && last.NextIndex < match.StartIndex) { matches.Add(SequenceMatch<SequenceType>.CreateIntermediate(this, sequence, startIndex, match.StartIndex - last.NextIndex)); }
                matches.Add(match);
            }
            else
            {
                if (last == null) { matches.Add(SequenceMatch<SequenceType>.CreateIntermediate(this, sequence, 0, sequence.Length)); }
                else { matches.Add(SequenceMatch<SequenceType>.CreateIntermediate(this, sequence, last.NextIndex, sequence.Length - last.NextIndex)); }
            }
            return matches.ToArray();
        }

    }


}
