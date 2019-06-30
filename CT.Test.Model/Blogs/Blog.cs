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
using CT.Blogs.Model.Blogs.Posts;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CT.Blogs.Model.Blogs
{
    [DataContract]
    [KeyProperty(nameof(Blog.Id))]
    public class Blog
    {
        [DataMember]
        public long Id { get; set; }

        public Blog()
        {
            Id = new long().NewId();

            posts = new ConcurrentDictionary<long, Post>();
            _posts = new ReadOnlyDictionary<long, Post>(posts);

            blogUsers = new ConcurrentDictionary<long, BlogUser>();
            _blogUsers = new ReadOnlyDictionary<long, BlogUser>(blogUsers);
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        internal ConcurrentDictionary<long, Post> posts;
        private ReadOnlyDictionary<long, Post> _posts;
        public IReadOnlyDictionary<long, Post> Posts
        {
            get { return _posts; }
        }

        public Post CreateNewPost()
        {
            return new Post(this);
        }

        [DataMember]
        internal ConcurrentDictionary<long, BlogUser> blogUsers;
        private readonly ReadOnlyDictionary<long, BlogUser> _blogUsers;
        public IReadOnlyDictionary<long, BlogUser> BlogUsers
        {
            get { return _blogUsers; }
        }

        public BlogUser CreateNewBlogUser(long userId)
        {
            return new BlogUser(this, userId);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _posts = new ReadOnlyDictionary<long, Post>(posts);
        }
    }
}
