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

using CT.Hosting;
using CT.Test.Presentation.Properties;
using System;
using System.Runtime.Serialization;

namespace CT.Test.Presentation.Shops.Registers
{
    [DataContract]
    [ParentProperty(nameof(RegisterCompositeContainer.Shop))]
    [CompositeDictionaryProperty(nameof(RegisterCompositeContainer.Registers))]
    public class RegisterCompositeContainer : Composite
    {
        internal RegisterCompositeContainer(ShopCompositeRoot shopCompositeRoot)
        {
            Shop = shopCompositeRoot;
            registers = new CompositeDictionary<long, RegisterComposite>();
            Registers = new ReadOnlyCompositeDictionary<long, RegisterComposite>(registers);

            foreach (var register in Shop.ShopModel.Registers.Values)
                registers.Add(register.Id, new RegisterComposite(register, this));
        }

        [NonSerialized]
        internal CompositeDictionary<long, RegisterComposite> registers;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, RegisterComposite> Registers { get; }

        public ShopCompositeRoot Shop { get; }

        [Command]
        [Help(typeof(Resources), nameof(Resources.Shop_Setup))]
        [return: Help(typeof(Resources), nameof(Resources.Shop_Setup_ReturnValue))]
        public RegisterComposite CreateNewRegister(CompositeRootHttpContext context)
        {
            if (context.Request.UserName.ToLowerInvariant() == "admin")
            {
                var newRegister = new RegisterComposite(Shop.ShopModel.CreateNewRegister(), this)
                {
                    State = CompositeState.New
                };
                registers.Add(newRegister.Id, newRegister);
                return newRegister;
            }
            else
                throw new UnauthorizedAccessException();
        }

        [Command]
        public void ReloadRegisters()
        {
            registers.Clear();

            var registerService = CompositeRoot.GetService<IRegisterService>();

            foreach(var register in registerService.LoadDefaultRegisters())
            {
                var loadedRegister = new RegisterComposite(register, this);
                registers.Add(loadedRegister.Id, loadedRegister);
            }
        }
    }
}
