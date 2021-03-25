AboditStateMachine
====

A state machine for .NET that implements easily serializable, async, hierarchical state machines with timing. There are plenty of .NET State Machines out there, so what's different about this one?

Firstly, it's async so the entry, exit or transition actions can use `async await`. 

Secondly, it's hierarchcical which means [you don't have to write as many rules](https://www.cis.upenn.edu/~lee/06cse480/lec-HSM.pdf).

Thirdly, it includes timing features that work in a 'generative' fashion. For example, a recurring timed event only tracks the one next execution time. You can register any combination of `At`, `Every` or `After` timed events. Externally the state machine presents a single `NextTimedEventAt` property. This enables persistence of the state and timing information to a database and easy retrieval of any state machines that need to be run next. The async `Tick` call on the state machine advances to the next time and runs any exit, entry and other actions.

Fourthly, it has a fluent easy to write syntax but also aims to be strongly-typed, both States and Events can be made specific to one state machine, they aren't just enums or strings.

![State machine](https://user-images.githubusercontent.com/347540/112415752-a1804380-8ce1-11eb-8897-2221d33769c8.png)

Another novel aspect of the timing code is that it uses a `TimeProvider` which makes time 'mockable' for testing. A `ManualTimeProvider` can be set to any date/time and then advanced to trigger actions for testing. The default time provider runs on `DateTimeOffset.Now`.

There are three components to the state machine:

Events
---

You can use any class that implements `IEquatable<T>` to implement these (or the provided `Event` class). Events can be shared across state machines. Alternatively you can keep the Events private to the state machine implementation and expose them only as methods on the StateMachine itself. e.g.

````csharp
private static Event eUserVerifiedEmail = new Event("User verified email");

public void VerifiesEmail()
{
    this.EventHappens(eUserVerifiedEmail);
}
````

States
---

These belong to a given state machine, they can be hierarchical. They must be created by calling `AddState` but can be exposed externally. The `Is` method on a `State` makes it easy to test within the hierarchy of states.

e.g.

````csharp
public static readonly State UnVerified = AddState("UnVerified");
````

Hierarchical states make it much easier to model complex systems, for example, an oven can be `Off` or `On` and within `On` it can be `On.Heating` or `On.Ready`. Transitioning to `Off` from any `On` state when the user turns it off can now be expressed as a single rule.

StateMachine
---

The static definition of the finite state machine specifying how to transition between states when events happen. 

e.g.

````csharp
VerifiedRecently
    .When(eBeenHereAWhile, async (m, s, e) =>
    {
        Trace.WriteLine("User has now been a member for over 24 hours");
        // give them additional priviledges perhaps
        await ...
        // and transition to a new state
        return VerifiedAWhileAgo;
    });
````

The `When` method takes the state machine instance, the current state and the event and executes a method with can operate on any of these objects and then returns the new state (for a transition) or `s` the current state if no transition happens.

A state machine class is defined using a self-referencing generic an Event type and a Context type:

````csharp
public class DemoStatemachine : StateMachine<DemoStatemachine, Event, Context>
{
    ...
}
````

The state machine instance should not contain additional properties, put these in Context. 
By serializing the CurrentState and the TimedEvents you can save and load the state machine from 
a persistent store as needed to handle events or at the time it suggests as the next execution time.

You can execute a `Task` when a state is entered or when it is left. Hierarchical states are entered and left in the appropriate order (reversed on leaving).

Whilst you can include complex business logic in the state machine itself for such transitions, I find it better to instead keep that outside and expose additional C# events that fire when the business logic needs to be executed. This keeps the state machine implementation 'pure', dealing only with  the definition of states and transitions between states and not the logic that happens when a state transitions.

Compared to the Wikipedia definition of a hierarchical state machine there is one further addition which is a set of methods and properties to handle time-based events and recurring events using an efficient 'next-occurrence' approach. The exposed property `NextTimedEventAt` can be mapped to a database column with an index allowing your program to easily find any state machines that need to be re-loaded and to have their `Tick` method called.

An example of the use of this temporal capability would be the implementation of user messaging flow for a website. The user creates an account, verifies their email, uses the system for a while, then deletes their account. After creating their account a reminder Event is added to send them a welcome email, a week later a follow up 'how is it going' email and so on. When the state machine loads from the database it can check that the user has not deleted their account, can fire an event to send the appropriate email and can schedule the next email.

Please take a look at the tests for examples of simple, hierarchical and compound state machines.
