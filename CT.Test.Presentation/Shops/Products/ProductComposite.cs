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
    [KeyProperty(nameof(ProductComposite.Id))]
    [IdProperty(nameof(ProductComposite.Id))]
    [ParentProperty(nameof(ProductComposite.AllProducts))]
    public class ProductComposite : Composite
    {
        internal Product ProductModel;

        internal ProductComposite(Product product, ProductCompositeContainer productCompositeContainer)
        {
            ProductModel = product;
            AllProducts = productCompositeContainer;
        }

        public ProductCompositeContainer AllProducts { get; }

        [DataMember]
        public long Id
        {
            get { return ProductModel.Id; }
        }

        [DataMember]
        public decimal Price
        {
            get { return ProductModel.Price; }
            set
            {
                ProductModel.Price = value;
                NotifyPropertyChanged(nameof(ProductComposite.Price));
            }
        }

        [DataMember]
        public DateTime? ExpirationDate
        {
            get { return ProductModel.ExpirationDate; }
            set
            {
                ProductModel.ExpirationDate = value;
                NotifyPropertyChanged(nameof(ProductComposite.ExpirationDate));
            }
        }

        [DataMember]
        public ProductUnit Unit
        {
            get { return ProductModel.Unit; }
            set
            {
                ProductModel.Unit = value;
                NotifyPropertyChanged(nameof(ProductComposite.Unit));
            }
        }

        [Command]
        public void Remove()
        {
            AllProducts.products.Remove(Id);
        }
    }
}
