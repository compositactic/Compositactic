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
using CT.Blogs.Presentation.Properties;
using System;
using System.Runtime.Serialization;
using CT.Blogs.Model.Blogs;

namespace CT.Blogs.Presentation.BlogApplications.Blogs
{
    [DataContract]
    [ParentProperty(nameof(BlogCompositeContainer.BlogApplication))]
    [CompositeContainer(nameof(BlogCompositeContainer.Blogs))]
    public class BlogCompositeContainer : Composite
    {
        public BlogApplicationCompositeRoot BlogApplication { get; private set; }

        internal BlogCompositeContainer(BlogApplicationCompositeRoot blogApplicationCompositeRoot)
        {
            this.InitializeCompositeContainer(blogs, blogApplicationCompositeRoot);
        }

        [NonSerialized]
        internal CompositeDictionary<long, BlogComposite> blogs;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, BlogComposite> Blogs { get; private set; }

        [Command]
        [Help(typeof(Resources), nameof(Resources.BlogCompositeContainer_CreateNewBlog_CommandHelp))]
        [return: Help(typeof(Resources), nameof(Resources.BlogCompositeContainer_CreateNewBlog_ReturnValueHelp))]
        public BlogComposite CreateNewBlog(CompositeRootHttpContext context)
        {
            if (context.Request.UserName.ToLowerInvariant() == "admin")
            {
                var newBlog = new BlogComposite(new Blog(), this)
                {
                    State = CompositeState.New
                };
                blogs.Add(newBlog.Id, newBlog);
                return newBlog;
            }
            else
                throw new UnauthorizedAccessException();
        }
    }
}
