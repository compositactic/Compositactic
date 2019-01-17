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
using CT.Test.Model.Shops.Products;
using CT.Test.Model.Shops.Registers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CT.Test.Model.Shops
{
    [DataContract]
    public class Shop
    {
        public Shop()
        {
            Id = new long().NewId();

            cashiers = new ConcurrentDictionary<long, Cashier>();
            _cashiers = new ReadOnlyDictionary<long, Cashier>(cashiers);

            products = new ConcurrentDictionary<long, Product>();
            _products = new ReadOnlyDictionary<long, Product>(products);

            registers = new ConcurrentDictionary<long, Register>();
            _registers = new ReadOnlyDictionary<long, Register>(registers);

            var defaultCashier = CreateNewCashier();
        }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        internal ConcurrentDictionary<long, Cashier> cashiers;
        private ReadOnlyDictionary<long, Cashier> _cashiers;
        public IReadOnlyDictionary<long, Cashier> Cashiers
        {
            get { return _cashiers; }
        }

        public Cashier CreateNewCashier()
        {
            return new Cashier(this);
        }

        public void AddCashier(Cashier cashier)
        {
            cashier.Shop = this;
            cashiers.Load(cashier, _ => { return cashier.Id; });
        }

        [DataMember]
        internal ConcurrentDictionary<long, Product> products;
        private ReadOnlyDictionary<long, Product> _products;
        public IReadOnlyDictionary<long, Product> Products
        {
            get { return _products; }
        }

        public Product CreateNewProduct()
        {
            return new Product(this);
        }

        public void AddProduct(Product product)
        {
            product.Shop = this;
            products.Load(product, _ => { return product.Id; });
        }

        [DataMember]
        internal ConcurrentDictionary<long, Register> registers;
        private ReadOnlyDictionary<long, Register> _registers;
        public IReadOnlyDictionary<long, Register> Registers
        {
            get { return _registers; }
        }

        public Register CreateNewRegister()
        {
            return new Register(this);
        }

        public void AddRegister(Register register)
        {
            register.Shop = this;
            registers.Load(register, _ => { return register.Id; });
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.RestoreParentReferences();
            _cashiers = new ReadOnlyDictionary<long, Cashier>(cashiers);
            _products = new ReadOnlyDictionary<long, Product>(products);
            _registers = new ReadOnlyDictionary<long, Register>(registers);
        }
    }
}
