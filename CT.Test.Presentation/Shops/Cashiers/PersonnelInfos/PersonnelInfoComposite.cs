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
using CT.Test.Model.Shops.Cashiers.PersonnelInfos;
using System;
using System.Runtime.Serialization;
using System.Linq;
using CT.Test.Model.Shops.Cashiers;

namespace CT.Test.Presentation.Shops.Cashiers.PersonnelInfos
{
    [DataContract]
    [ParentProperty(nameof(Cashier), nameof(CashierComposite.PersonnelInfo))]
    public class PersonnelInfoComposite : Composite
    {
        internal PersonnelInfo PersonnelInfoModel;

        internal PersonnelInfoComposite(CashierComposite cashier)
        {
            PersonnelInfoModel = cashier.CashierModel.PersonnelInfo;
            Cashier = cashier;
        }

        public CashierComposite Cashier { get; }

        [DataMember]
        public string Name
        {
            get { return PersonnelInfoModel.Name; }
            set
            {
                try
                {
                    PersonnelInfoModel.Name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
                catch (Exception e)
                {
                    ((ShopCompositeRoot)CompositeRoot).ErrorMessage = e.Message;
                }
            }
        }

        [DataMember]
        public DateTimeOffset HireDate
        {
            get { return PersonnelInfoModel.HireDate; }
        }

        [DataMember]
        public bool IsActive
        {
            get { return PersonnelInfoModel.IsActive; }
            set
            {
                PersonnelInfoModel.IsActive = value;
                NotifyPropertyChanged(nameof(PersonnelInfoComposite.IsActive));
            }
        }

        [DataMember]
        public Authority Authority
        {
            get { return PersonnelInfoModel.Authority; }
            set
            {
                PersonnelInfoModel.Authority = value;
                NotifyPropertyChanged(nameof(PersonnelInfoComposite.Authority));
            }
        }

        [Command]
        public byte[] GetPicture(CompositeRootHttpContext context)
        {
            var personnelInfoService = CompositeRoot.GetService<IPersonnelInfoService>();
            context.Response.ContentType = "image/jpeg";
            return personnelInfoService.GetPicture(PersonnelInfoModel.PicturePath);
        }

        [Command]
        public void AddPicture(CompositeRootHttpContext context)
        {
            var personnelInfoService = CompositeRoot.GetService<IPersonnelInfoService>();
            personnelInfoService.SavePicture(context.Request.UploadedFiles.FirstOrDefault(), PersonnelInfoModel.PicturePath);
        }
    }
}
