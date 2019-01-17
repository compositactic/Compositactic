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

namespace CT.Test.Presentation.Shops.Registers.Periods
{
    [DataContract]
    [ParentProperty(nameof(PeriodCompositeContainer.Register))]
    [CompositeDictionaryProperty(nameof(PeriodCompositeContainer.Periods))]
    public class PeriodCompositeContainer : Composite
    {
        internal PeriodCompositeContainer(RegisterComposite registerComposite)
        {
            Register = registerComposite;
            periods = new CompositeDictionary<long, PeriodComposite>();
            Periods = new ReadOnlyCompositeDictionary<long, PeriodComposite>(periods);

            foreach (var period in Register.RegisterModel.Periods.Values)
                periods.Add(period.Id, new PeriodComposite(period, this));
        }

        [NonSerialized]
        internal CompositeDictionary<long, PeriodComposite> periods;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, PeriodComposite> Periods { get; }

        public RegisterComposite Register { get; }

        [Command]
        public PeriodComposite CreateNewPeriod()
        {
            var newPeriod = new PeriodComposite(Register.RegisterModel.CreateNewPeriod(), this)
            {
                State = CompositeState.New
            };

            periods.Add(newPeriod.Id, newPeriod);
            return newPeriod;
        }
    }
}
