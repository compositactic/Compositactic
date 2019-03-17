using System;
using System.Runtime.Serialization;

namespace CT.Blogs.Model.Blogs.BlogUsers
{
    [DataContract]
    [ParentProperty(nameof(BlogUser.Blog))]
    [KeyProperty(nameof(BlogUser.Id))]
    public class BlogUser
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public long BlogId { get; set; }

        public Blog Blog { get; internal set; }

        [DataMember]
        public long UserId { get; internal set; }

        public BlogUser(Blog blog, long userId)
        {
            BlogId = blog.Id;
            UserId = userId;

            Blog = blog ?? throw new ArgumentNullException(nameof(blog));
            Blog.blogUsers.Load(this, _ => { return new long().NewId(); });
        }

        public void Remove()
        {
            Blog.blogUsers.TryRemove(Id, out BlogUser removedValue);
        }
    }
}
