using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Abodit.StateMachine;

namespace AboditStateMachineTests
{
    /// <summary>
    /// Context object. Put additonal 'state' here beyond the state name, and not in the state machine. (SRP)
    /// </summary>
    public class DemoContext
    {
        public string UserName { get; set; }

        // Could pass in other useful context here

        public DateTime DateTimeLastCommunicated { get; set; }

        public int MessageCounterForTesting { get; set; }
    }

    [Serializable]
    public class DemoStatemachine : StateMachineAsync<DemoStatemachine, Event, DemoContext>
    {
        protected override void OnStateChanging(DemoContext context, State oldState, State newState)
        {
            Trace.WriteLine($"Left state {oldState.Name}, entered state {newState.Name}");
        }

        public static async Task SendReminderEmail(DemoStatemachine m, State state, Event e, DemoContext c)
        {
            Trace.WriteLine("You need to verify your email " + state + " via " + e);
            c.MessageCounterForTesting++;
            // Send email to ask them to verify their account
            await Task.Delay(10);
            c.DateTimeLastCommunicated = DateTime.UtcNow;
        }

        public static async Task CompletedEntered(DemoStatemachine m, Event e, State state, DemoContext c)
        {
            // No longer need to be on a scheduled check routine
            m.CancelScheduledEvent(eScheduledCheck);
            Trace.WriteLine("Use has completed email verification " + state + " via " + e);
            await Task.Delay(10);  // simulate write to database
        }

        // Unlike events, states belong to a particular state machine and should all be defined as static readonly properties like this:

        public static readonly State UnVerified = AddState("UnVerified");

        public static readonly State Verified = AddState("Verified");

        // States are hierarchical.  If you are in state VerifiedRecently you are also in is parent state Verified.

        public static readonly State VerifiedRecently = AddState("Verified recently", parent: Verified);
        public static readonly State VerifiedAWhileAgo = AddState("Verified a while ago", parent: Verified);

        // You can use any class that implements IEquatableofT as an Event, there is also an Event class provided which you
        // can use instead of defining one each time.

        private static readonly Event eUserVerifiedEmail = new Event("User verified email");
        private static readonly Event eScheduledCheck = new Event("Scheduled Check");
        private static readonly Event eBeenHereAWhile = new Event("Been here a while");

        /// <summary>
        /// A static constructor in your state machine is where you define it.
        /// That way it is only ever defined once per program activation.
        ///
        /// Each transition you define takes as an argument the state machine instance (m),
        /// the state (s), the event (e) and the context (c).
        ///
        /// </summary>
        static DemoStatemachine()
        {
            UnVerified
                    .OnEnter((m, s, e, c) =>
                        {
                            Trace.WriteLine("States can execute code when they are entered or when they are left");
                            Trace.WriteLine("In this case we start a timer to bug the user until they confirm their email");
                            m.Every(new TimeSpan(hours: 10, minutes: 0, seconds: 0), eScheduledCheck);

                            Trace.WriteLine("You can also set a reminder to happen at a specific time, or after a given interval just once");
                            m.At(new DateTime(DateTime.Now.Year + 1, 1, 1), eScheduledCheck);
                            m.After(new TimeSpan(hours: 24, minutes: 0, seconds: 0), eScheduledCheck);

                            Trace.WriteLine("All necessary timing information is serialized with the state machine.");
                            Trace.WriteLine("The serialized state machine also exposes a property showing when it next needs to be woken up.");
                            Trace.WriteLine("External code will need to call the Tick(utc) method at that time to trigger the next temporal event");

                            return Task.CompletedTask;
                        })
                    .When(eScheduledCheck, (m, s, e, c) =>
                    {
                        Trace.WriteLine("Here is where we would send a message to the user asking them to verify their email");
                        // We return the current state 's' rather than 'UnVerified' in case we are in a child state of 'Unverified'
                        // This makes it easy to handle hierarchical states and to either change to a different state or stay in the same state
                        return Task.FromResult(s);
                    })
                    .When(eUserVerifiedEmail, (m, s, e, c) =>
                    {
                        Trace.WriteLine("The user has verified their email address, we are done (almost)");
                        // Kill the scheduled check event, we no longer need it
                        m.CancelScheduledEvent(eScheduledCheck);
                        // Start a timer for one last transition
                        m.After(new TimeSpan(hours: 24, minutes: 0, seconds: 0), eBeenHereAWhile);
                        return Task.FromResult(VerifiedRecently);
                    });

            VerifiedRecently
                    .When(eBeenHereAWhile, (m, s, e, c) =>
                    {
                        Trace.WriteLine("User has now been a member for over 24 hours - give them additional priviledges for example");
                        // No need to cancel the eBeenHereAWhile event because it wasn't auto-repeating
                        //m.CancelScheduledEvent(eBeenHereAWhile);
                        return Task.FromResult(VerifiedAWhileAgo);
                    });

            Verified.OnEnter((m, s, e, c) =>
                {
                    Trace.WriteLine("The user is now fully verified");
                    return Task.CompletedTask;
                });

            VerifiedAWhileAgo.OnEnter((m, s, e, c) =>
                {
                    Trace.WriteLine("The user has been verified for over 24 hours");
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Default constructor - used only when deserializing (does not start the clock)
        /// </summary>
        public DemoStatemachine()
            : base(UnVerified)
        {
        }

        /// <summary>
        /// An alternate constructor with an initial state
        /// </summary>
        public DemoStatemachine(State initial)
            : base(initial)
        {
            // Manually start the clock since we are not being deserialized
            this.Every(new TimeSpan(hours: 10, minutes: 0, seconds: 0), eScheduledCheck);
        }

        // Instead of exposing our Events we might expose methods on the state machine that fire the events
        // I think this is preferable, some of the events might be purely internal, like the timer tick ones

        public async Task VerifiesEmail(DemoContext context)
        {
            await this.EventHappens(eUserVerifiedEmail, context);
        }

        public override void Start(State initial)
        {
            base.Start(initial);
        }
    }
}