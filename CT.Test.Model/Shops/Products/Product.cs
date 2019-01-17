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

namespace CT.Test.Model.Shops.Products
{
    [DataContract]
    [ParentProperty(nameof(Product.Shop))]
    [KeyProperty(nameof(Product.Id))]
    public class Product
    {
        private Product() {}

        internal Product(Shop shop)
        {
            ShopId = shop.Id;

            Shop = shop ?? throw new ArgumentNullException(nameof(shop));
            Shop.products.Load(this, _ => { return new long().NewId(); });
        }

        [DataMember]
        public long Id { get; private set; }

        public Shop Shop { get; internal set; }

        [DataMember]
        public long ShopId { get; set; }

        [DataMember]
        public decimal Price { get; set; }

        [DataMember]
        public ProductUnit Unit { get; set; }

        [DataMember]
        public DateTime? ExpirationDate { get; set; }

        [DataMember]
        public bool Discounted { get; set; }

        [DataMember]
        public string Name { get; set; }

        public void Remove()
        {
            Shop.products.TryRemove(Id, out Product removedValue);
        }
    }
}
