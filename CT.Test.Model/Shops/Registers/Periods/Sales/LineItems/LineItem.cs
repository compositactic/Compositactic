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

using CT.Test.Model.Shops.Products;
using System;
using System.Runtime.Serialization;

namespace CT.Test.Model.Shops.Registers.Periods.Sales.LineItems
{
    [DataContract]
    [ParentProperty(nameof(LineItem.Sale))]
    [KeyProperty(nameof(LineItem.Id))]
    public class LineItem
    {
        private LineItem() { }

        internal LineItem(Sale sale)
        {
            Sale = sale ?? throw new ArgumentNullException(nameof(sale));
            Sale.lineItems.Load(this, _ => { return new long().NewId(); });
        }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public long SaleId { get; set; }
        public Sale Sale { get; internal set; }

        [DataMember]
        public long ProductId { get; set; }
        public Product Product { get; internal set; }

        [DataMember]
        public int Quantity { get; set; }

        public void Remove()
        {
            Sale.lineItems.TryRemove(Id, out LineItem removedValue);
        }
    }
}
