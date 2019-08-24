// Compositactic - Made in the USA - Indianapolis, IN  - Copyright (c) 2017 Matt J. Crouch

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

using CT.Hosting;
using System;

namespace CT
{
    [Serializable]
    public class CompositeEvent
    {
        internal CompositeEvent(CompositeEventType eventType, string path, object data, string sessionToken, CompositeRootMode mode)
        {
            EventType = eventType;
            Data = data;
            SessionToken = sessionToken;
            Mode = mode;
            Path = path;
        }

        public CompositeEvent(CompositeEventType eventType, string path, object data)
        {
            EventType = eventType;
            Data = data;
            Path = path;
        }

        [NonSerialized]
        internal readonly CompositeRootMode Mode;

        [NonSerialized]
        internal readonly string SessionToken;

        public string Path;
        public readonly CompositeEventType EventType;
        public readonly object Data;

        public override bool Equals(object obj)
        {
            var compositeEvent = obj as CompositeEvent;
            if (compositeEvent == null)
                return false;

            return Path.Equals(compositeEvent.Path) &&
                    EventType.Equals(compositeEvent.EventType) &&
                    Data.Equals(compositeEvent.Data);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const int hashingBase = (int)2166136261;
                const int HashingMultiplier = 16777619;

                int hash = hashingBase;
                hash = (hash * HashingMultiplier) ^ (Path is object ? Path.GetHashCode() : 0);
                hash = (hash * HashingMultiplier) ^ (EventType.GetHashCode());
                hash = (hash * HashingMultiplier) ^ (Data is object ? Data.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
