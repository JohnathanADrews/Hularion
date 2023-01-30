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
using HularionText.Compiler;
using HularionText.Compiler.Sequence;
using HularionText.Language.Json.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace HularionText.Language.Json
{
    /// <summary>
    /// A language parser for JSON that can convert JSON string into a JsonDocument.
    /// </summary>
    public class JsonLanguage 
    {
        /// <summary>
        /// The name of the language.
        ///// </summary>
        //public string Name { get { return "JSON"; } }
        //public bool IsCaseSensitive { get; } = true;

        public SequenceTree<char> SymbolTree { get; set; } = new SequenceTree<char>() { Name = "Symbol Tree" };

        static SymbolGroup boundaryGroup = new SymbolGroup() { Name = "Boundary Operator" };
        static SymbolGroup textGroup = new SymbolGroup() { Name = "Text Operator" };
        //static SymbolGroup ignoreGroup = new SymbolGroup() { Name = "Ignore" };
        //static SymbolGroup spaceGroup = new SymbolGroup() { Name = "Empty Space", CombineMembers = true };
        //static SymbolGroup newLineGroup = new SymbolGroup() { Name = "New Line" };

        TextSymbol openObjectSymbol = new TextSymbol() { Sequence = "{".ToCharArray(), Group = boundaryGroup };
        TextSymbol closeObjectSymbol = new TextSymbol() { Sequence = "}".ToCharArray(), Group = boundaryGroup };
        TextSymbol openArraySymbol = new TextSymbol() { Sequence = "[".ToCharArray(), Group = boundaryGroup };
        TextSymbol closeArraySymbol = new TextSymbol() { Sequence = "]".ToCharArray(), Group = boundaryGroup };
        TextSymbol doubleQuoteSymbol = new TextSymbol() { Sequence = "\"".ToCharArray(), Group = textGroup };
        TextSymbol colonSymbol = new TextSymbol() { Sequence = ":".ToCharArray(), Group = boundaryGroup };
        TextSymbol commaSymbol = new TextSymbol() { Sequence = ",".ToCharArray(), Group = boundaryGroup };

        TextSymbol escapeDoubleQuoteSymbol = new TextSymbol() { Sequence = "\\\"".ToCharArray(), Group = textGroup };
        //TextSymbol ignoreReturnSymbol = new TextSymbol() { Sequence = "\r".ToCharArray(), Group = ignoreGroup };
        //TextSymbol tab = new TextSymbol() { Sequence = "\t".ToCharArray(), Group = spaceGroup };
        //TextSymbol space = new TextSymbol() { Sequence = " ".ToCharArray(), Group = spaceGroup };
        //TextSymbol newline = new TextSymbol() { Sequence = "\n".ToCharArray(), Group = newLineGroup };

        private Table<SequenceTreeNode<char>, OperationMode> actionTable = new Table<SequenceTreeNode<char>, OperationMode>();
        Action<JsonContext> intermediateAction = new Action<JsonContext>(context => { });

        OperationMode startMode = new OperationMode() { Name = "Start" };
        OperationMode beginObjectMode = new OperationMode() { Name = "Begin Object" };
        OperationMode endObjectMode = new OperationMode() { Name = "End Object" };
        OperationMode beginArrayMode = new OperationMode() { Name = "Begin Array" };
        OperationMode endArrayMode = new OperationMode() { Name = "EndArrayMode" };
        OperationMode stringKeyReadMode = new OperationMode() { Name = "String Key Read" };
        OperationMode stringKeyCompleteMode = new OperationMode() { Name = "String Key Complete" };
        OperationMode stringValueReadMode = new OperationMode() { Name = "String Value Read" };
        OperationMode stringValueCompleteMode = new OperationMode() { Name = "String Value Complete" };
        OperationMode objectValueReadMode = new OperationMode() { Name = "Object Value Read" };
        OperationMode objectValueCompleteMode = new OperationMode() { Name = "Object Value Read Complete" };
        OperationMode colonMode = new OperationMode() { Name = "Colon" };
        OperationMode commaMode = new OperationMode() { Name = "Comma" };

        string nullString = "null";

        public JsonLanguage()
        {

            #region OpenObject
            AddSymbolMode(startMode, openObjectSymbol, new Action<JsonContext>(context => //JSON text opens with an object.
            {
                PushObject(context);
            }));
            AddSymbolMode(beginArrayMode, openObjectSymbol, new Action<JsonContext>(context => //The first element of an array, which is an object.
            {
                PushObject(context);
            }));
            AddSymbolMode(colonMode, openObjectSymbol, new Action<JsonContext>(context => //The begining of an object, which is a value within another object.
            {
                PushObject(context);
            }));
            AddSymbolMode(commaMode, openObjectSymbol, new Action<JsonContext>(context =>  //The begining of an object within an array.
            {
                PushObject(context);
            }));
            #endregion

            #region CloseObject
            AddSymbolMode(beginObjectMode, closeObjectSymbol, new Action<JsonContext>(context => //empty object.
            {
                PopObject(context);
            }));
            AddSymbolMode(endObjectMode, closeObjectSymbol, new Action<JsonContext>(context => //end of one object and then the end of the next.
            {
                PopObject(context);
            }));
            AddSymbolMode(endArrayMode, closeObjectSymbol, new Action<JsonContext>(context => //The end of an array, which is also the end of an object.
            {
                PopObject(context);
            }));
            AddSymbolMode(commaMode, closeObjectSymbol, new Action<JsonContext>(context => //The begining of a non-string, non-object, non-array value to its end at the end of an object.
            {
                AddValueNode(context);
                PopObject(context);
            }));
            AddSymbolMode(stringValueCompleteMode, closeObjectSymbol, new Action<JsonContext>(context =>
           {
               AddValueNode(context);
               context.ModeStack.Pop(); //pop stringValueCompleteMode
                                        //context.ModeStack.Pop(); //pop valueMode
                PopObject(context);
           }));
            #endregion

            #region OpenArray
            AddSymbolMode(startMode, openArraySymbol, new Action<JsonContext>(context => //JSON text opens with an array.
            {
                PushArray(context);
            }));
            AddSymbolMode(beginArrayMode, openArraySymbol, new Action<JsonContext>(context => //Array within array.
            {
                PushArray(context);
            }));
            AddSymbolMode(colonMode, openArraySymbol, new Action<JsonContext>(context => //The begining of an array, which is a value within an object.
            {
                PushArray(context);
            }));
            AddSymbolMode(commaMode, openArraySymbol, new Action<JsonContext>(context => //The beginning of an array within an array.
            {
                PushArray(context);
            }));
            #endregion

            #region CloseArray
            AddSymbolMode(beginArrayMode, closeArraySymbol, new Action<JsonContext>(context => //An empty array or an array with a single value.
            {
                context.ValueEndIndex = context.Match.StartIndex;
                if (!context.ValueAlreadyAdded) { AddValueNode(context); }
                PopArray(context);
            }));
            AddSymbolMode(endArrayMode, closeArraySymbol, new Action<JsonContext>(context => //The end of an array, followed by the end of the parent array.
            {
                PopArray(context);
            }));
            AddSymbolMode(commaMode, closeArraySymbol, new Action<JsonContext>(context => //From the separator of the previous value to the end.
            {
                AddValueNode(context);
                PopArray(context);
            }));
            AddSymbolMode(stringValueCompleteMode, closeArraySymbol, new Action<JsonContext>(context => //Indicates the last value is a string.
            {
                AddValueNode(context);
                PopArray(context);
            }));
            #endregion

            #region DoubleQuote
            AddSymbolMode(beginObjectMode, doubleQuoteSymbol, new Action<JsonContext>(context => // beginning of a value's key.
            {
                context.ModeStack.Push(stringKeyReadMode);
                context.NameStartIndex = context.Match.NextIndex;
            }));
            AddSymbolMode(beginArrayMode, doubleQuoteSymbol, new Action<JsonContext>(context => //The first element of an array, which is a string.
            {
                context.ModeStack.Push(stringValueReadMode);
                context.ValueStartIndex = context.Match.NextIndex;
            }));
            AddSymbolMode(colonMode, doubleQuoteSymbol, new Action<JsonContext>(context => //The begining of an string value.
            {
                context.ModeStack.Push(stringValueReadMode);
                context.ValueStartIndex = context.Match.NextIndex;
            }));
            AddSymbolMode(commaMode, doubleQuoteSymbol, new Action<JsonContext>(context => //The beginning of and object's member name.
            {
                context.ModeStack.Push(stringValueReadMode);
                context.ValueStartIndex = context.Match.NextIndex;
            }));
            AddSymbolMode(stringKeyReadMode, doubleQuoteSymbol, new Action<JsonContext>(context => //The beginning of a string name or value to its end.
            {
                context.NameEndIndex = context.Match.StartIndex;
                context.NodeName = StringPrinter(context.SourceText.Substring(context.NameStartIndex, context.Match.StartIndex - context.NameStartIndex));
                context.ModeStack.Pop();
                context.ModeStack.Push(stringKeyCompleteMode);
            }));
            AddSymbolMode(stringValueReadMode, doubleQuoteSymbol, new Action<JsonContext>(context =>
            {
                context.ModeStack.Pop();
                context.ValueEndIndex = context.Match.StartIndex;
                context.NodeType = JsonElementType.String;
                AddValueNode(context);
                //context.Node.AddNode(new JsonString() { Name = context.NodeName, Value = context.SourceText.Substring(context.ValueStartIndex, context.ValueEndIndex - context.ValueStartIndex) });
                context.ValueAlreadyAdded = true;
            }));
            AddSymbolMode(objectValueReadMode, doubleQuoteSymbol, new Action<JsonContext>(context =>
            {
                context.ModeStack.Push(stringValueReadMode);
                context.ValueStartIndex = context.Match.NextIndex;
            }));
            AddSymbolMode(objectValueReadMode, commaSymbol, new Action<JsonContext>(context =>
            {
                context.ModeStack.Pop();
                if (context.ValueAlreadyAdded)
                {
                    context.ValueAlreadyAdded = false;
                    return;
                }
                context.NodeType = JsonElementType.Unknown;
                context.ValueEndIndex = context.Match.StartIndex;
                AddValueNode(context);

            }));
            AddSymbolMode(objectValueReadMode, closeObjectSymbol, new Action<JsonContext>(context =>
            {
                context.ModeStack.Pop();
                if (context.ValueAlreadyAdded)
                {
                    context.ValueAlreadyAdded = false;
                    PopObject(context);
                    return;
                }
                context.NodeType = JsonElementType.Unknown;
                context.ValueEndIndex = context.Match.StartIndex;
                AddValueNode(context);
                PopObject(context);
            }));
            AddSymbolMode(objectValueReadMode, openArraySymbol, new Action<JsonContext>(context =>
            {
                PushArray(context);
            }));
            AddSymbolMode(objectValueReadMode, openObjectSymbol, new Action<JsonContext>(context =>
            {
                PushObject(context);
            }));
            AddSymbolMode(stringKeyReadMode, escapeDoubleQuoteSymbol, new Action<JsonContext>(context =>
            {
                //Do nothing. Special characters are handled in post-processing.
            }));
            AddSymbolMode(stringValueReadMode, escapeDoubleQuoteSymbol, new Action<JsonContext>(context =>
            {
                //Do nothing. Special characters are handled in post-processing.
            }));
            #endregion

            #region Colon
            AddSymbolMode(stringKeyCompleteMode, colonSymbol, new Action<JsonContext>(context => //key/value break
            {
                context.ModeStack.Pop();
                context.ModeStack.Push(objectValueReadMode);
                context.ValueStartIndex = context.Match.NextIndex; //If not a string, we need to set the start index.
            }));
            #endregion

            #region Comma
            AddSymbolMode(stringValueCompleteMode, commaSymbol, new Action<JsonContext>(context =>
           {
               AddValueNode(context);
               context.ModeStack.Pop();
           }));
            AddSymbolMode(beginArrayMode, commaSymbol, new Action<JsonContext>(context => //The first element of an array, which is a string.
            {
                context.ValueEndIndex = context.Match.StartIndex;
                if (!context.ValueAlreadyAdded) { AddValueNode(context); }
                context.ValueAlreadyAdded = false;
                context.ValueStartIndex = context.Match.NextIndex;
            }));
            #endregion

        }

        /// <summary>
        /// Parses the text string into a JsonDocument.
        /// </summary>
        /// <param name="text">The text to parse/</param>
        /// <returns>A JsonDocument.</returns>
        public JsonDocument Parse(string text)
        {
            var context = new JsonContext() { SourceText = text };
            context.ModeStack.Push(startMode);
            var sequence = text.ToCharArray();
            var matches = new SequenceMatch<char>[] { };
            int index = 0;

            while (index < sequence.Length)
            {
                matches = SymbolTree.GetNextMatches(sequence, index, matches.LastOrDefault(), context);
                for (var i = 0; i < matches.Length; i++)
                {
                    var match = matches[i];
                    context.PreviousMatch = context.Match;
                    context.Match = match;
                    var symbolText = new string(match.Value.Sequence);
                    if (match.IsIntermediate)
                    {
                        intermediateAction(context);
                    }
                    else if (actionTable.ContainsValue(match.Match, (OperationMode)context.Mode))
                    {
                        var action = actionTable.GetValue<Action<JsonContext>>(match.Match, (OperationMode)context.Mode);
                        action(context);
                    }
                    index = matches[i].NextIndex;
                }
            }
            return context.Document;
        }

        private string StringPrinter(string value)
        {
            var printResult = new StringBuilder((int)(1.2 * value.Length));
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if(c != '\\') { printResult.Append(c); continue; }
                i++;
                if(i >= value.Length) { break; }
                c = value[i];

                if (c == '"') { printResult.Append('\"'); continue; }
                if (c == '\\') { printResult.Append('\\'); continue; }
                if (c == '/') { printResult.Append('/'); continue; }
                if (c == 'b') { printResult.Append('\b'); continue; }
                if (c == 'f') { printResult.Append('\f'); continue; }
                if (c == 'n') { printResult.Append('\n'); continue; }
                if (c == 'r') { printResult.Append('\r'); continue; }
                if (c == 't') { printResult.Append('\t'); continue; }
                if (c == 'u')
                {
                    if(i + 4 >= value.Length) { break; }
                    int ic1;
                    ic1 = (int)value[i + 1];
                    ic1 = (ic1 >= 97 ? (ic1 - 97) : (ic1 - 48));
                    int ic2;
                    ic2 = (int)value[i + 2];
                    ic2 = (ic2 >= 97 ? (ic2 - 97) : (ic2 - 48));
                    int ic3;
                    ic3 = (int)value[i + 3];
                    ic3 = (ic3 >= 97 ? (ic3 - 97) : (ic3 - 48));
                    int ic4;
                    ic4 = (int)value[i + 4];
                    ic4 = (ic4 >= 97 ? (ic4 - 87) : (ic4 - 48));

                    printResult.Append((char)((ic1 << 12) | (ic2 << 8) | (ic3 << 4) | ic4));
                    i += 4;
                    continue;
                }
            }
            return printResult.ToString();
        }

        private void AddValueNode(JsonContext context)
        {
            JsonElement node = null;
            if (context.NodeType == JsonElementType.String)
            {
                node = new JsonString() { Name = context.NodeName, Value = StringPrinter(context.SourceText.Substring(context.ValueStartIndex, context.ValueEndIndex - context.ValueStartIndex)) };
            }
            if (context.NodeType == JsonElementType.Unknown)
            {
                var valueString = context.SourceText.Substring(context.ValueStartIndex, context.ValueEndIndex - context.ValueStartIndex);
                if (String.IsNullOrWhiteSpace(valueString)) { return; }
                if (IsNullString(valueString)) { node = new JsonUnknown() { Name = context.NodeName }; }
                if (node == null)
                {
                    bool boolValue;
                    if (bool.TryParse(valueString, out boolValue)) { node = new JsonBool() { Value = boolValue }; }
                    else { node = new JsonNumber() { Value = String.Format("{0}", valueString).Trim() }; }
                }
                if (node != null) { node.Name = context.NodeName; }
            }
            if (node != null)
            {
                node.Parent = context.Node;
                context.NodeType = JsonElementType.Unknown;
                context.Node.AddNode(node);
            }
        }

        private void AddSymbolMode(OperationMode mode, TextSymbol symbol, Action<JsonContext> action)
        {
            var sequenceNode = SymbolTree.AddSymbol(symbol, mode);
            actionTable.AddColumn(sequenceNode);
            actionTable.AddRow(mode);
            actionTable.SetValue(sequenceNode, mode, action);
        }

        private void PushObject(JsonContext context)
        {
            context.ModeStack.Push(beginObjectMode);
            var node = new JsonObject() { Parent = context.Node, Name = context.NodeName };
            context.Node.AddNode(node);
            context.Node = node;
            context.ValueStartIndex = context.Match.NextIndex;
        }

        private void PopObject(JsonContext context)
        {
            context.ModeStack.Pop();
            context.Node = context.Node.Parent;
            context.ValueAlreadyAdded = true;
        }

        private void PushArray(JsonContext context)
        {
            context.ModeStack.Push(beginArrayMode);
            var node = new JsonArray() { Parent = context.Node, Name = context.NodeName };
            context.Node.AddNode(node);
            context.Node = node;
            context.ValueStartIndex = context.Match.NextIndex;
        }

        private void PopArray(JsonContext context)
        {
            context.ModeStack.Pop();
            context.Node = context.Node.Parent;
            context.ValueAlreadyAdded = true;
        }


        private bool IsNullString(string value)
        {
            return value.Trim().ToLower() == nullString;
        }


        private class JsonContext : SequenceProcessContext
        {

            public JsonDocument Document { get; set; } = new JsonDocument();

            public SequenceMatch<char> Match { get; set; }

            public SequenceMatch<char> PreviousMatch { get; set; }

            public JsonElement Node { get; set; }

            public int NameStartIndex { get; set; }

            public int NameEndIndex { get; set; }

            public string NodeName { get; set; }

            public int ValueStartIndex { get; set; }

            public int ValueEndIndex { get; set; }

            public string NodeValue { get; set; }

            public JsonElementType NodeType { get; set; } = JsonElementType.Unknown;

            public string SourceText { get; set; }

            public bool ValueAlreadyAdded { get; set; } = false;

            public JsonContext()
            {
                Node = Document.Root;
            }
        }

        private class OperationMode
        {
            public string Name { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
