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

using CT.Blogs.Model.Blogs.Posts.Comments;
using CT.Blogs.Model.Users;
using CT.Blogs.Presentation.BlogApplications.Users;
using CT.Data.MicrosoftSqlServer;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Linq;

namespace CT.Blogs.Presentation.BlogApplications.Blogs.Posts.Comments
{
    [DataContract]
    [KeyProperty(nameof(CommentComposite.Id))]
    [ParentProperty(nameof(CommentComposite.AllComments))]
    [CompositeModel(nameof(CommentComposite.CommentModel))]
    public class CommentComposite : Composite
    {
        internal CommentComposite(Comment comment, CommentCompositeContainer commentCompositeContainer)
        {
            CommentModel = comment;
            AllComments = commentCompositeContainer;
        }


        public CommentCompositeContainer AllComments { get; private set; }

        public Comment CommentModel { get; }

        [DataMember]
        public long Id
        {
            get { return CommentModel.Id; }
        }


        [DataMember]
        public string Text
        {
            get { return CommentModel.Text; }
            set
            {
                CommentModel.Text = value;
                NotifyPropertyChanged(nameof(Text));
            }
        }


        private UserComposite _user;

        [DataMember]
        public UserComposite User
        {
            get
            {
                var repository = CompositeRoot.GetService<IMicrosoftSqlServerRepository>();
                var connectionString = ((BlogApplicationCompositeRoot)CompositeRoot).BlogDbConnectionString;
                using (var connection = repository.OpenConnection(connectionString))
                {
                    var user = repository
                        .Load(connection,
                                null,
                                "SELECT * FROM \"User\" WHERE Id = @userId",
                                new SqlParameter[] 
                                {
                                    new SqlParameter("@userId", CommentModel.UserId)
                                },
                                typeof(User)).FirstOrDefault() as User;


                    _user = new UserComposite(user) ?? null;
                }
   
                return _user;
            }
        }


        [Command]
        public void Remove()
        {
            AllComments.comments.Remove(Id);
        }
    }
}
