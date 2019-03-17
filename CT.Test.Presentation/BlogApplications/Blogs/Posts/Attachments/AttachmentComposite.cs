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

using CT.Blogs.Model.Blogs.Posts.Attachments;
using System.Runtime.Serialization;

namespace CT.Blogs.Presentation.BlogApplications.Blogs.Posts.Attachments
{
    [DataContract]
    [KeyProperty(nameof(AttachmentComposite.Id))]
    [ParentProperty(nameof(AttachmentComposite.AllAttachments))]
    [CompositeModel(nameof(AttachmentComposite.AttachmentModel))]
    public class AttachmentComposite : Composite
    {
        public Attachment AttachmentModel { get; }
        public AttachmentCompositeContainer AllAttachments { get; }

        internal AttachmentComposite(Attachment attachment, AttachmentCompositeContainer attachmentCompositeContainer)
        {
            AttachmentModel = attachment;
            AllAttachments = attachmentCompositeContainer;
        }

        [DataMember]
        public long Id
        {
            get { return AttachmentModel.Id; }
        }

        [Command]
        public void Remove()
        {
            AllAttachments.attachments.Remove(Id);
        }
    }
}
