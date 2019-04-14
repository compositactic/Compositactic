using CT.Blogs.Model.Blogs.BlogUsers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CT.Blogs.Presentation.BlogApplications.Blogs.BlogUsers
{
    [DataContract]
    public class BlogUserComposite : Composite
    {

        public BlogUserCompositeContainer SubscribedUsers { get; }

        internal BlogUser BlogUserModel;

        internal BlogUserComposite(BlogUser blogUser, BlogUserCompositeContainer blogUserCompositeContainer)
        {
            BlogUserModel = blogUser;
            SubscribedUsers = blogUserCompositeContainer;
        }

        [DataMember]
        public long Id
        {
            get { return BlogUserModel.Id; }
        }

        [Command]
        public void Remove()
        {
            SubscribedUsers.blogUsers.Remove(Id);
        }
    }
}
