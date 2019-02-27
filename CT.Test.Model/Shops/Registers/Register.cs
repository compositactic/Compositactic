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

using CT.Test.Model.Shops.Registers.Periods;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CT.Test.Model.Shops.Registers
{
    [DataContract]
    [ParentProperty(nameof(Register.Shop))]
    [KeyProperty(nameof(Register.Id))]
    public class Register
    {
        private Register() { }

        internal Register(Shop shop)
        {
            ShopId = shop.Id;

            periods = new ConcurrentDictionary<long, Period>();
            _periods = new ReadOnlyDictionary<long, Period>(periods);

            Shop = shop ?? throw new ArgumentNullException(nameof(shop));
            Shop.registers.Load(this, _ => { return new long().NewId(); });
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _periods = new ReadOnlyDictionary<long, Period>(periods);
        }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public long ShopId { get; set; }

        public Shop Shop { get; internal set; }

        public void Remove()
        {
            Shop.registers.TryRemove(Id, out Register removedValue);
        }

        public Period CreateNewPeriod()
        {
            return new Period(this);
        }

        public void AddPeriod(Period period)
        {
            period.Register = this;
            periods.Load(period, _ => { return period.Id; });
        }

        [DataMember]
        public string SerialNumber { get; set; }

        [DataMember]
        internal ConcurrentDictionary<long, Period> periods;
        private ReadOnlyDictionary<long, Period> _periods;
        public IReadOnlyDictionary<long, Period> Periods
        {
            get { return _periods; }
        }
    }
}
