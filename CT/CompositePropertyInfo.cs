﻿// Compositactic - Made in the USA - Indianapolis, IN  - Copyright (c) 2017 Matt J. Crouch

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN 
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace CT
{
    [DataContract]
    [Serializable]
    public class CompositePropertyInfo 
    {
        internal CompositePropertyInfo(string propertyName, Type propertyType, bool isReadOnly, string helpText)
        {
            PropertyName = propertyName;

            Match m;
            if ((m = Regex.Match(propertyType.FullName, @"CT.ReadOnlyCompositeDictionary[^[]*\[\[(?'key_type'[^,]+).+?(?=\])],\[(?'value_type'[^,]+)")).Success)
                PropertyType = $"CT.ReadOnlyCompositeDictionary<{m.Groups["key_type"].Value}, {m.Groups["value_type"].Value}>";
            else
                PropertyType = propertyType.FullName;

            PropertyEnumValues = propertyType.GetTypeEnumValues();

            IsReadOnly = isReadOnly;
            HelpText = helpText;
        }

        [DataMember]
        public string HelpText { get; }

        [DataMember]
        public string[] PropertyEnumValues { get; } 

        [DataMember]
        public string PropertyName { get; }

        [DataMember]
        public string PropertyType { get; }

        [DataMember]
        public bool IsReadOnly { get; }
    }
}
