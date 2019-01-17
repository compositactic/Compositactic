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
using CT.Test.Presentation.Properties;
using System;
using System.Runtime.Serialization;

namespace CT.Test.Presentation.Shops.Cashiers
{
    [DataContract]
    [ParentProperty(nameof(CashierCompositeContainer.Shop))]
    [CompositeDictionaryProperty(nameof(CashierCompositeContainer.Cashiers))]
    public class CashierCompositeContainer : Composite
    {
        internal CashierCompositeContainer(ShopCompositeRoot shopCompositeRoot)
        {
            Shop = shopCompositeRoot;
            cashiers = new CompositeDictionary<long, CashierComposite>();
            Cashiers = new ReadOnlyCompositeDictionary<long, CashierComposite>(cashiers);

            foreach (var cashier in Shop.ShopModel.Cashiers.Values)
                cashiers.Add(cashier.Id, new CashierComposite(cashier, this));
        }

        [NonSerialized]
        internal CompositeDictionary<long, CashierComposite> cashiers;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, CashierComposite> Cashiers { get; }

        public ShopCompositeRoot Shop { get; }

        [Command]
        [Help(typeof(Resources), nameof(Resources.Cashier_CreateNewCashier))]
        [return: Help("Upon success, returns a newly created " + nameof(Cashier))]
        public CashierComposite CreateNewCashier(
            [Help(typeof(Resources), nameof(Resources.Cashier_CreateNewCashier_ParameterName))] string name,
            [Help(typeof(Resources), nameof(Resources.Cashier_CreateNewCashier_ParameterAuthority))] Authority authority)
        {
            var newCashier = new CashierComposite(Shop.ShopModel.CreateNewCashier(), this)
            {
                State = CompositeState.New
            };

            newCashier.PersonnelInfo.Name = name;

            return newCashier;
        }
    }
}
