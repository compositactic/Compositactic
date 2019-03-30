# Use Case - [Name]

##### Type: [Process/Command]

##### Other Actors: [Actor 1, Actor 2]

##### Steps - [Sequential/Random]

1. [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]
2. ...

##### Step Exceptions

+ other random failure1
    + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]
    + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]
+ Other random failure2

1. Actor failures (for Step 1)
    + Failure Condition 1 description
        + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]
        + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]
    + Failure Condition 2 description
2. Actor failures (for Step 2)
    + Failure Condition 1 description
        + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]
        + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)] 
    + Failure Condition 2 description
        + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]
        + [Actor] [performs some action] [preposition] [Actor(s)/Composite(s)]

##### Rules

+ [Condition that must be met at beginning, during, end of use case]
+ [another Condition that must be met]

---
##### Instructions for using this template (Remove this section when no longer needed):

Use Cases describe the required steps/actions taken by [Actors](Actor.md) as part of a [Story](Story.md). Multiple Use Cases may describe a single Use Case with increasing levels of detail.



Type is either Process or Command. Process denotes Use Cases that complete over an extended period, with starts and stops. Command denotes Use Cases that complete uninterrupted and correspond to Commands of a Composite.
Steps describe successful actions taken by an [Actors](Actor.md) on Composites. Steps can be Sequential or Random
Higher level Use Cases can have Steps that can identify other Use Cases (hyperlink)



Substitute [Actors](Actor.md) may perform this Use Case's Steps on behalf of the Use Case's Actor.



First Step (Trigger Step) should describe [Actor's](Actor.md) action triggering the Use Case. Could be a Substitute [Actor](Actor.md).



Use Case Steps should:

Use active verbs in present tense in active voice
Mention the Actor triggering the Step (process/interaction)
Mention the Actor who will perform the Step
Mention the action performed by the Actor (ex. instructs, selects, displays, submits, assigns, creates, deletes, updates, processes, notifies, etc.) and Entities the action should be performed on 
Write the Step as an Goal to be met and that advances the Use Case forward
For Process Use Cases, mention the Actor performing another Use Case where appropriate
Example Formatted: [Actor] [verb] to/with/for/in [Entity/Entities]



Step Exception:

Alternate Step to perform when failure occurs performing a Step.



Rules:

Describes pre/post constraints, conditions, and state required by all Entities involved in Use Case while Actor performs the Use Case. Rules of parent Use Cases should be obeyed prior to Rules asserted in this Use Case.


