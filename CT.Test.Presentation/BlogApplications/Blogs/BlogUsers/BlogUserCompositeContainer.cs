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

using CT.Blogs.Model.Blogs.BlogUsers;
using System;
using System.Runtime.Serialization;

namespace CT.Blogs.Presentation.BlogApplications.Blogs.BlogUsers
{
    [DataContract]
    [ParentProperty(nameof(BlogUserCompositeContainer.Blog))]
    [CompositeContainer(nameof(BlogUserCompositeContainer.BlogUsers), nameof(Model.Blogs.Blog.BlogUsers))]
    public class BlogUserCompositeContainer : Composite
    {

        public BlogComposite Blog { get; private set; }
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
