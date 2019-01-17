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

using CT.Test.Model.Shops.Cashiers;
using CT.Test.Model.Shops.Registers.Periods.Sales;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CT.Test.Model.Shops.Registers.Periods
{
    [DataContract]
    [ParentProperty(nameof(Period.Register))]
    [KeyProperty(nameof(Period.Id))]
    public class Period
    {
        private Period() { }

        internal Period(Register register)
        {
            RegisterId = register.Id;

            sales = new ConcurrentDictionary<long, Sale>();
            _sales = new ReadOnlyDictionary<long, Sale>(sales);

            Register = register ?? throw new ArgumentNullException(nameof(register));
            Register.periods.Load(this, _ => { return new long().NewId(); });
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _sales = new ReadOnlyDictionary<long, Sale>(sales);
        }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public long RegisterId { get; set; }
        public Register Register { get; internal set; }

        [DataMember]
        public long CashierId { get; set; }
        public Cashier Cashier { get; }

        [DataMember]
        public DateTimeOffset StartingTime { get; set; }

        [DataMember]
        public DateTimeOffset EndingTime { get; set; }

        [DataMember]
        internal ConcurrentDictionary<long, Sale> sales;
        private ReadOnlyDictionary<long, Sale> _sales;
        public IReadOnlyDictionary<long, Sale> Sales
        {
            get { return _sales; }
        }

        public Sale CreateNewSale()
        {
            return new Sale(this);
        }

        public void AddSale(Sale sale)
        {
            sale.Period = this;
            sales.Load(sale, _ => { return sale.Id; });
        }

        public void Remove()
        {
            Register.periods.TryRemove(Id, out Period removedValue);
        }
    }
}
