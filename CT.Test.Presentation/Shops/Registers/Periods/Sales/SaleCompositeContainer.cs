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

using System;
using System.Runtime.Serialization;

namespace CT.Test.Presentation.Shops.Registers.Periods.Sales
{
    [DataContract]
    [ParentProperty(nameof(SaleCompositeContainer.Period))]
    [CompositeDictionaryProperty(nameof(SaleCompositeContainer.Sales))]
    public class SaleCompositeContainer : Composite
    {
        internal SaleCompositeContainer(PeriodComposite period)
        {
            Period = period;
            sales = new CompositeDictionary<long, SaleComposite>();
            Sales = new ReadOnlyCompositeDictionary<long, SaleComposite>(sales);

            foreach (var sale in Period.PeriodModel.Sales.Values)
                sales.Add(sale.Id, new SaleComposite(sale, this));
        }

        [NonSerialized]
        internal CompositeDictionary<long, SaleComposite> sales;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, SaleComposite> Sales { get; }

        public PeriodComposite Period { get; }

        [Command]
        public SaleComposite CreateNewSale()
        {
            var newSale = new SaleComposite(Period.PeriodModel.CreateNewSale(), this)
            {
                State = CompositeState.New
            };

            sales.Add(newSale.Id, newSale);
            return newSale;
        }
    }
}
