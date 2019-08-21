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

using CT.Blogs.Model.Blogs.Posts;
using CT.Data.MicrosoftSqlServer;
using CT.Hosting;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;

namespace CT.Blogs.Presentation.BlogApplications.Blogs.Posts
{
    [DataContract]
    [ParentProperty(nameof(PostCompositeContainer.Blog))]
    [CompositeContainer(nameof(PostCompositeContainer.Posts), nameof(Model.Blogs.Blog.Posts))]
    public class PostCompositeContainer : Composite
    {
        public BlogComposite Blog { get; private set; }

        internal PostCompositeContainer(BlogComposite blogComposite)
        {
            this.InitializeCompositeContainer(out posts, blogComposite);
        }

        [NonSerialized]
        internal CompositeDictionary<long, PostComposite> posts;
        [DataMember]
        public ReadOnlyCompositeDictionary<long, PostComposite> Posts { get; private set; }

        [Command]
        public PostComposite CreateNewPost(CompositeRootHttpContext context)
        {
            var newPost = new PostComposite(Blog.BlogModel.CreateNewPost(), this)
            {
                State = CompositeState.New
            };

            posts.Add(newPost.Id, newPost);
            return newPost;
        }

        [Command]
        public void LoadPosts()
        {
            var blogApplication = CompositeRoot as BlogApplicationCompositeRoot;
            var repository = blogApplication.GetService<IMicrosoftSqlServerRepository>();

            using (var connection = repository.OpenConnection(blogApplication.BlogDbConnectionString))
            {
                posts.AddRange(repository.Load<Post>(connection, null,
                    @"
                        SELECT * 
                        FROM Post 
                        WHERE BlogId = @BlogId
                    ",
                    new SqlParameter[] { new SqlParameter("@BlogId", Blog.Id) })
                    .Select(p => new PostComposite(p, this)));
            }
        }

        [Command]
        public void LoadPosts(int pageStart, int pageEnd)
        {
            var blogApplication = CompositeRoot as BlogApplicationCompositeRoot;
            var repository = blogApplication.GetService<IMicrosoftSqlServerRepository>();

            using (var connection = repository.OpenConnection(blogApplication.BlogDbConnectionString))
            {
                posts.AddRange(repository.Load<Post>(connection, null,

                    @"
                      WITH Posts AS
                      (
                        SELECT ROW_NUMBER() OVER(ORDER BY ID DESC) AS RowNumber, *
                        FROM Post 
                        WHERE BlogId = @BlogId
                      )
                      SELECT *
                      FROM Posts
                      WHERE RowNumber BETWEEN @pageStart AND @pageEnd
                    ",

                    new SqlParameter[] 
                    {
                        new SqlParameter("@BlogId", Blog.Id),
                        new SqlParameter("@pageStart", pageStart),
                        new SqlParameter("@pageEnd", pageEnd)
                    })
                    .Select(p => new PostComposite(p, this)));
            }
        }
    }
}
