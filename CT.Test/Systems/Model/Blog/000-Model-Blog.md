# Model - Blog
The Blog represents a [User](../User/000-Model-User.md)'s published content viewable by a [Reader](../../Actors/Reader/000-Actor-Reader.md)

|Property|Type|Description
|:-|:-|:-
|Name|Text|Represents the name of the Blog
|Posts|Collection|[Post](Post/000-Model-Post.md)s belonging to the Blog
|BlogUsers|Collection|References to Users of the Blog 
