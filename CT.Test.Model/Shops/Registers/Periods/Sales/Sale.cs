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

using CT.Test.Model.Shops.Registers.Periods.Sales.LineItems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CT.Test.Model.Shops.Registers.Periods.Sales
{
    [DataContract]
    [ParentProperty(nameof(Sale.Period))]
    [KeyProperty(nameof(Sale.Id))]
    public class Sale
    { 
        private Sale() { }

        internal Sale(Period period)
        {
            PeriodId = period.Id;

            Period = period ?? throw new ArgumentNullException(nameof(period));

            lineItems = new ConcurrentDictionary<long, LineItem>();
            _lineItems = new ReadOnlyDictionary<long, LineItem>(lineItems);

            Period.sales.Load(this, _ => { return new long().NewId(); });
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _lineItems = new ReadOnlyDictionary<long, LineItem>(lineItems);
        }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public long PeriodId { get; set; }
        public Period Period { get; internal set; }

        [DataMember]
        public DateTimeOffset TransactionDate { get; set; }

        [DataMember]
        public decimal AmountDue { get; }

        public LineItem CreateNewLineItem()
        {
            return new LineItem(this);
        }

        public void AddLineItem(LineItem lineItem)
        {
            lineItem.Sale = this;
            lineItems.Load(lineItem, _ => { return lineItem.Id; });
        }

        [DataMember]
        internal ConcurrentDictionary<long, LineItem> lineItems;
        private ReadOnlyDictionary<long, LineItem> _lineItems;
        public IReadOnlyDictionary<long, LineItem> LineItems
        {
            get { return _lineItems; }
        }
    }
}
