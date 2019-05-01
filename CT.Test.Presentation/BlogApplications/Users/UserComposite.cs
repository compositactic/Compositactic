using CT.Blogs.Model.Users;
using System.Runtime.Serialization;

namespace CT.Blogs.Presentation.BlogApplications.Users
{
    [DataContract]
    public class UserComposite : Composite
    {
        private readonly User _user;
        internal UserComposite(User user)
        {
            _user = user;
        }

        [DataMember]
        public long Id
        {
            get { return _user.Id; }
        }

            
        [DataMember]
        public string Name
        {
            get { return _user.Name; }
            set
            {
                _user.Name = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }
    }
}
