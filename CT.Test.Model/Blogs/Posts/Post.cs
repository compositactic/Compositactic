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


using CT.Blogs.Model.Blogs.Posts.Attachments;
using CT.Blogs.Model.Blogs.Posts.Comments;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;


namespace CT.Blogs.Model.Blogs.Posts
{
    [DataContract]
    [ParentProperty(nameof(Post.Blog))]
    [KeyProperty(nameof(Post.Id))]
    public class Post
    {
        public Post() { }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public long BlogId { get; set; }

        public Blog Blog { get; internal set; }

        internal Post(Blog blog)
        {
            BlogId = blog.Id;

            comments = new ConcurrentDictionary<long, Comment>();
            _comments = new ReadOnlyDictionary<long, Comment>(comments);

            attachments = new ConcurrentDictionary<long, Attachment>();
            _attachments = new ReadOnlyDictionary<long, Attachment>(attachments);

            Blog = blog ?? throw new ArgumentNullException(nameof(blog));
            Blog.posts.Load(this, _ => { return new long().NewId(); });
        }

        public void Remove()
        {
            Blog.posts.TryRemove(Id, out _);
        }

        public Comment CreateNewComment()
        {
            return new Comment(this);
        }

        [DataMember]
        internal ConcurrentDictionary<long, Comment> comments;
        private ReadOnlyDictionary<long, Comment> _comments;
        public IReadOnlyDictionary<long, Comment> Comments
        {
            get { return _comments; }
        }

        public Attachment CreateNewAttachment()
        {
            return new Attachment(this);
        }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Text { get; set; }

        [DataMember]
        internal ConcurrentDictionary<long, Attachment> attachments;
        private ReadOnlyDictionary<long, Attachment> _attachments;
        public IReadOnlyDictionary<long, Attachment> Attachments
        {
            get { return _attachments; }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _comments = new ReadOnlyDictionary<long, Comment>(comments);
            _attachments = new ReadOnlyDictionary<long, Attachment>(attachments);
        }
    }
}
