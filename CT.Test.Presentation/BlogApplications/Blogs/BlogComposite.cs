﻿// Compositactic - Made in the USA - Indianapolis, IN  - Copyright (c) 2017 Matt J. Crouch

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

using CT.Blogs.Model.Blogs;
using CT.Blogs.Presentation.BlogApplications.Blogs.Posts;
using CT.Data.MicrosoftSqlServer;
using System.Runtime.Serialization;

namespace CT.Blogs.Presentation.BlogApplications.Blogs
{
    [DataContract]
    [KeyProperty(nameof(BlogComposite.Id))]
    [ParentProperty(nameof(BlogComposite.AllBlogs))]
    [CompositeModel(nameof(BlogModel))]
    public class BlogComposite : Composite
    {
        public BlogCompositeContainer AllBlogs { get; }

        internal Blog BlogModel;

        internal BlogComposite(Blog blog, BlogCompositeContainer blogCompositeContainer)
        {
            BlogModel = blog;
            AllBlogs = blogCompositeContainer;
            AllPosts = new PostCompositeContainer(this);
        }

        [DataMember]
        public PostCompositeContainer AllPosts { get; }

        [DataMember]
        public long Id
        {
            get { return BlogModel.Id; }
        }

        [DataMember]
        public string Name
        {
            get { return BlogModel.Name; }
            set
            {
                BlogModel.Name = value;
                NotifyPropertyChanged(nameof(BlogComposite.Name));
            }
        }

        [Command]
        public void Remove()
        {
            AllBlogs.blogs.Remove(Id);
        }

        [Command]
        public void Save()
        {
            var blogApplication = CompositeRoot as BlogApplicationCompositeRoot;
            var repository = blogApplication.GetService<IMicrosoftSqlServerRepository>();

            using (var connection = repository.OpenConnection(blogApplication.BlogDbConnectionString))
            using (var transaction = repository.BeginTransaction(connection))
            {
                repository.Save(connection, transaction, this);
                transaction.Commit();
            }
        }
    }
}
