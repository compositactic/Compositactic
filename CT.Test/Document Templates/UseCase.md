# Use Case - [Name]

##### Type: [Process/Command]

##### Substitute Actors: [Actor 1, Actor 2]

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

* Use Cases describe the required steps/actions taken by [Actors](Actor.md) as part of a [Story](Story.md). 
* Type is either Process or Command. Process denotes Use Cases that complete over an extended period, with starts and stops. Command denotes Use Cases that complete uninterrupted and correspond to Commands of a Composite.
* Multiple Use Cases may describe a single Use Case - a Process-level Use Case may contain multiple Command-level Use Cases 
* Steps describe successful actions taken by an [Actors](Actor.md) on Composites. Steps can be Sequential or Random
* Higher level Use Cases can have Steps that can identify other Use Cases
* Substitute [Actors](Actor.md) may perform this Use Case's Steps on behalf of the Use Case's [Actor](Actor.md)


First Step should describe [Actor's](Actor.md) action triggering the Use Case. Could be a Substitute [Actor](Actor.md).


##### Steps should:

* Use active verbs in present tense in active voice
* Mention the [Actor](Actor.md) triggering the Step 
* Mention the [Actor](Actor.md) who will perform the Step
* Mention the action performed by the [Actor](Actor.md) (ex. instructs, selects, displays, submits, assigns, creates, deletes, updates, processes, notifies, etc.) and Models the action should be performed on 
* Write the Step as a goal to be met and that advances the Use Case forward
* For Process Use Cases, mention the Actor performing another Use Case where appropriate

##### Step Exceptions should:

Alternate Step to perform when failure occurs performing a Step.


##### Rules should:

* Describes all [System](System.md) conditions that must be met at the beginning, during, and end of Use Case.
* Rules of parent Use Cases should be obeyed prior to Rules asserted in this Use Case.

---

<div style="page-break-after: always;"></div>