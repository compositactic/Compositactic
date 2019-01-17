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

using CT.Properties;
using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading;

namespace CT.Hosting
{
    [DataContract]
    [KeyProperty(nameof(Token))]
    [ParentProperty(nameof(ActiveSessions))]
    public class CompositeRootSession : Composite, IDisposable
    {
        internal CompositeRootSession(CompositeRootSessionContainer activeSessions)
        {
            ActiveSessions = activeSessions;
            Initialize();
        }

        private void Initialize()
        {
            _events = new BlockingCollection<CompositeEvent>(new ConcurrentQueue<CompositeEvent>());
            EventsDone = new ManualResetEvent(true);
        }

        public CompositeRootSessionContainer ActiveSessions { get; }

        [Command]
        public void Remove()
        {
            ActiveSessions.sessions.Remove(Token);
        }

        internal CompositeRootMode mode;
        [DataMember]
        public CompositeRootMode Mode
        {
            get
            {
                return mode;
            }
            internal set
            {
                if (value == CompositeRootMode.None)
                    throw new ArgumentException(string.Format(Resources.CompositeRootModeParameterCannotBeNone, nameof(value), value.ToString()), nameof(value), null);

                mode = value;
                NotifyPropertyChanged(nameof(CompositeRootSession.Mode));
            }
        }

        internal TimeSpan expiration;
        [DataMember]
        public TimeSpan Expiration
        {
            get { return expiration; }
            set
            {
                expiration = value;
                NotifyPropertyChanged(nameof(CompositeRootSession.Expiration));
            }
        }

        internal DateTime lastAccessed;
        [DataMember]
        public DateTime LastAccessed
        {
            get { return lastAccessed; }
            internal set
            {
                lastAccessed = value;
                NotifyPropertyChanged(nameof(CompositeRootSession.LastAccessed));
            }
        }

        internal string userName;
        [DataMember]
        public string UserName
        {
            get { return userName; }
            set
            {
                userName = value;
                NotifyPropertyChanged(nameof(CompositeRootSession.UserName));
            }
        }

        internal string token;
        [DataMember]
        public string Token
        {
            get { return token; }
            internal set
            {
                token = value;
                NotifyPropertyChanged(nameof(CompositeRootSession.Token));
            }
        }

        internal bool IsEventStreamWriting { get; set; }

        [NonSerialized]
        internal ManualResetEvent EventsDone;

        [NonSerialized]
        private BlockingCollection<CompositeEvent> _events;

        internal CompositeEvent TakeEvent()
        {
            if (_events == null)
                throw new InvalidOperationException();

            return _events.Take();
        }

        internal void AddEvent(CompositeEvent newEvent)
        {
            if (_events == null)
                return;

            if (newEvent == null)
                throw new ArgumentNullException(nameof(newEvent));

            _events.Add(newEvent);
        }

        bool disposed = false;
        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _events.Dispose();
                EventsDone.Dispose();
            }

            disposed = true;
        }

        ~CompositeRootSession()
        {
            Dispose(false);
        }
    }
}
