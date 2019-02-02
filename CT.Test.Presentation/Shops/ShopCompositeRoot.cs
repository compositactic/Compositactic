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

using CT.Data.MicrosoftSqlServer;
using CT.Hosting.Configuration;
using CT.Test.Model.Shops;
using CT.Test.Presentation.Properties;
using CT.Test.Presentation.Shops.Cashiers;
using CT.Test.Presentation.Shops.Products;
using CT.Test.Presentation.Shops.Registers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace CT.Test.Presentation.Shops
{
    [DataContract]
    [CompositeModel(nameof(ShopModel))]
    public class ShopCompositeRoot : CompositeRoot
    {
        internal Shop ShopModel { get; private set; }

        public ShopCompositeRoot(CompositeRootConfiguration configuration) : base(configuration)
        {
            Initialize(new Shop());
        }

        public ShopCompositeRoot(CompositeRootConfiguration configuration, params IService[] services) : base(configuration, services)
        {
            Initialize(new Shop());
        }

        public ShopCompositeRoot(CompositeRootConfiguration configuration, IEnumerable<Assembly> serviceAssemblies) : base(configuration, serviceAssemblies)
        {
            Initialize(new Shop());
        }

        private void Initialize(Shop shop)
        {
            ShopModel = shop;
            AllRegisters = new RegisterCompositeContainer(this);
            AllProducts = new ProductCompositeContainer(this);
            AllCashiers = new CashierCompositeContainer(this);
        }

        [DataMember]
        [Help(typeof(Resources), nameof(Resources.Shop_AllRegisters))]
        public RegisterCompositeContainer AllRegisters { get; private set; }

        [DataMember]
        [Help(typeof(Resources), nameof(Resources.Shop_AllProducts))]
        public ProductCompositeContainer AllProducts { get; private set; }

        [DataMember]
        [Help(typeof(Resources), nameof(Resources.Shop_AllCashiers))]
        public CashierCompositeContainer AllCashiers { get; private set; }

        private string _errorMessage;
        [DataMember]
        [Help(typeof(Resources), nameof(Resources.Shop_ErrorMessage))]
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                NotifyPropertyChanged(nameof(ErrorMessage));
            }
        }

        [DataMember]
        [Help(typeof(Resources), nameof(Resources.Shop_Name))]
        public string Name
        {
            get { return ShopModel.Name; }
            set
            {
                try
                {
                    ShopModel.Name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
                catch (Exception e)
                {
                    ErrorMessage = e.Message;
                }
            }
        }

        [Command]
        public void Save()
        {
            var repository = GetService<IMicrosoftSqlServerRepository>();

            var connectionString = Configuration.CustomSettings["ConnectionString"];

            using (var connection = repository.OpenConnection(connectionString))
            using (var transaction = repository.BeginTransaction(connection))
            {     
                repository.Save(connection, transaction, this);
                repository.CommitTransaction(transaction);
            }
        }
    }
}
