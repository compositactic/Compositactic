using CT.Blogs.Model.Blogs.BlogUsers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CT.Blogs.Presentation.BlogApplications.Blogs.BlogUsers
{
    [DataContract]
    [ParentProperty(nameof(BlogUserCompositeContainer.Blog))]
    [CompositeContainer(nameof(BlogUserCompositeContainer.BlogUsers), nameof(Model.Blogs.Blog.BlogUsers))]
    public class BlogUserCompositeContainer : Composite
    {

        public BlogComposite Blog { get; }
        internal BlogUserCompositeContainer(BlogComposite blog)
        {
            this.InitializeCompositeContainer(out blogUsers, blog);
        }

        [NonSerialized]
        internal CompositeDictionary<long, BlogUserComposite> blogUsers;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, BlogUserComposite> BlogUsers { get; private set; }

        [Command]
        public BlogUserComposite AddBlogSubscriber(long userId)
        {
            var newBlogUser = new BlogUserComposite(new BlogUser(Blog.BlogModel, userId), this)
            {
                State = CompositeState.New
            };

            blogUsers.Add(newBlogUser.Id, newBlogUser);
            return newBlogUser;
        }
    }
}
