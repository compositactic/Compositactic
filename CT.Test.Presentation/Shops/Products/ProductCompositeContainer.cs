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

namespace CT.Test.Presentation.Shops.Products
{
    [DataContract]
    [ParentProperty(nameof(ProductCompositeContainer.Shop))]
    [CompositeDictionaryProperty(nameof(ProductCompositeContainer.Products))]
    public class ProductCompositeContainer : Composite
    {
        internal ProductCompositeContainer(ShopCompositeRoot shopCompositeRoot)
        {
            Shop = shopCompositeRoot;
            products = new CompositeDictionary<long, ProductComposite>();
            Products = new ReadOnlyCompositeDictionary<long, ProductComposite>(products);

            foreach (var product in Shop.ShopModel.Products.Values)
                products.Add(product.Id, new ProductComposite(product, this));
        }

        [NonSerialized]
        internal CompositeDictionary<long, ProductComposite> products;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, ProductComposite> Products { get; }

        public ShopCompositeRoot Shop { get; }

        [Command]
        public ProductComposite CreateNewProduct()
        {
            var newProduct = new ProductComposite(Shop.ShopModel.CreateNewProduct(), this)
            {
                State = CompositeState.New
            };

            products.Add(newProduct.Id, newProduct);
            return newProduct;
        }

        [Command]
        public ProductComposite CreateNewProduct(decimal price, DateTime? expirationDate, ProductUnit unit)
        {
            var newProduct = new ProductComposite(Shop.ShopModel.CreateNewProduct(), this)
            {
                Price = price,
                ExpirationDate = expirationDate,
                Unit = unit,
                State = CompositeState.New
            };

            products.Add(newProduct.Id, newProduct);
            return newProduct;
        }
    }
}
