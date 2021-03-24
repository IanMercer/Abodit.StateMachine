using System;
using System.Diagnostics;
using Abodit.StateMachine;
using System.IO;
using System.Xml.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AboditStateMachineTests
{
    // Might have a User with multiple StateMachines
    //   when they log in their logged in / logged out state machine changes state
    //   when they verify their account their account verified state machine changes state
    //   when a certain time passes a state change may happen automatically
    [Serializable]
    public class User
    {
        public VerifyingStatemachine VerifyingStateMachine { get; set; }

        public LoginOutStatemachine LoginoutStateMachine { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Used to be part of state machine, now always in context
        /// </summary>
        public DateTime DateTimeLastCommunicated { get; set; }

        /// <summary>
        /// Example of an extra property
        /// </summary>
        public int MessageCounterForTesting { get; set; }

        public VerifyingStatemachine.State VState => this.VerifyingStateMachine.CurrentState;

        public LoginOutStatemachine.State LState => this.LoginoutStateMachine.CurrentState;

        public User()
        {
            this.VerifyingStateMachine = new VerifyingStatemachine(VerifyingStatemachine.Starting);
            this.LoginoutStateMachine = new LoginOutStatemachine(LoginOutStatemachine.Initial);
        }

        public User(string name)
            : this()
        {
            this.Name = name;
        }

        public async Task Created()
        {
            await this.VerifyingStateMachine.Starts();
        }

        public async Task LogsIn()
        {
            await this.LoginoutStateMachine.LogsIn();
        }

        public async Task LogsOut()
        {
            await this.LoginoutStateMachine.LogsOut();
        }

        public async Task VerifiesEmail()
        {
            await this.VerifyingStateMachine.VerifiesEmail();
        }

        public async Task VerifiesSMS()
        {
            await this.VerifyingStateMachine.VerifiesSMS();
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            return "[" + this.Name + "]";
        }

        internal async Task Tick(DateTimeOffset now)
        {
            await this.VerifyingStateMachine.Tick(now, this);
        }
    }

    [Serializable]
    public class VerifyingStatemachine : StateMachineAsync<VerifyingStatemachine, Event, User>
    {
        // ? What about nested State machines?  Do we get this communicated date from somewhere else
        // that coordinates overall communication?

        public static Task ReportEnter(VerifyingStatemachine m, Event e, State state, User context)
        {
            Trace.WriteLine(context + " entered state " + state + " via " + e);
            return Task.CompletedTask;
        }

        public static Task ReportLeave(VerifyingStatemachine m, State state, Event e, User context)
        {
            Trace.WriteLine(context + " left state " + state + " via " + e);
            return Task.CompletedTask;
        }

        public static Task SendReminderEmail(VerifyingStatemachine m, State state, Event e, User user)
        {
            // Might want to handle state in here using this class' member variables
            // instead of trying to create separate events for each repeating event
            // that way you can easily prioritize the communications you want to do
            // when there are several possible ones that could fire
            if (state.Is(UnVerified))               // You can use the Is method or the overloaded == operator
            {
                Trace.WriteLine("You need to verify your email and SMS" + state + " via " + e);
                user.MessageCounterForTesting++;
                // Send email to ask them for an SMS
                user.DateTimeLastCommunicated = TimeProvider.Current.Now.DateTime;
            }
            else if (state.Is(VerifiedEmail))       // Is is generally better because it handles State inheritance
            {
                Trace.WriteLine("You need to verify your SMS" + state + " via " + e);
                user.MessageCounterForTesting++;
                user.DateTimeLastCommunicated = TimeProvider.Current.Now.DateTime;
            }
            else if (state == VerifiedSMS)
            {
                Trace.WriteLine("You need to verify your email" + state + " via " + e);
                user.MessageCounterForTesting++;
                user.DateTimeLastCommunicated = TimeProvider.Current.Now.DateTime;
            }
            else
            {
                Trace.WriteLine("Odd, the timer should NOT be running " + state + " via " + e);
            }
            return Task.CompletedTask;
        }

        public static Task CompletedEntered(VerifyingStatemachine m, Event e, State state, User user)
        {
            // No longer need to be on a scheduled check routine
            m.CancelScheduledEvent(eScheduledCheck);
            Trace.WriteLine(user + " has completed email and SMS verification " + state + " via " + e);
            return Task.CompletedTask;
        }

        public static State Starting = AddState("Starting");

        public static State UnVerified = AddState("UnVerified", async
                (m, s, e, c) =>
                {
                    m.Every(new TimeSpan(hours: 0, minutes: 1, seconds: 0), eScheduledCheck);
                    await ReportEnter(m, s, e, c);
                },
                ReportLeave);

        public static readonly State Verified = AddState("Verified", ReportEnter, ReportLeave);

        // Not a good example since you could be both of these (which is really a different kind of state machine)
        public static readonly State VerifiedEmail = AddState("Verified by email", ReportEnter, ReportLeave, Verified);

        public static readonly State VerifiedSMS = AddState("Verified by SMS", ReportEnter, ReportLeave, Verified);
        public static readonly State Completed = AddState("Completed: Verified by Email And SMS", CompletedEntered, ReportLeave, Verified);

        private static readonly Event eStart = new Event("Start the state machine");
        private static readonly Event eVerifyEmail = new Event("Verify Email");
        private static readonly Event eVerifySMS = new Event("Verify SMS");
        private static readonly Event eScheduledCheck = new Event("Scheduled Check");

        static VerifyingStatemachine()
        {
            // On startup we transition immediately to starting
            // but we want an event call to do this so we aren't doing any work
            // in the constructor, and so the initialization only happens when it's
            // a true 'cold start' not a 'warm start' from some database state

            Starting
                .When(eStart, (m, s, e, c) =>
                {
                    // Set the timer running and move to state Unverified
                    return Task.FromResult(UnVerified);
                });

            UnVerified
                    .When(eVerifyEmail, (m, s, e, c) =>
                    {
                        Trace.WriteLine("Verifying Email " + c);
                        // Schedule a task for some time later to email them to ask for an SMS confirmation
                        //m.At(DateTime.UtcNow.AddMinutes(10), eScheduledCheck);
                        return Task.FromResult(VerifiedEmail);
                    })
                    .When(eScheduledCheck, (m, s, e, c) => { SendReminderEmail(m, s, e, c); return Task.FromResult(s); })
                    .When(eVerifySMS, (m, s, e, c) => { Trace.WriteLine("Verifying SMS " + c); return Task.FromResult(VerifiedSMS); });

            VerifiedEmail
                    .When(eVerifySMS, (m, s, e, c) =>
                    {
                        Trace.WriteLine("Verifying SMS " + c);
                        return Task.FromResult(Completed);
                    })
                    .When(eScheduledCheck, (m, s, e, c) =>
                    {
                        SendReminderEmail(m, s, e, c);
                        return Task.FromResult(s);
                    });

            VerifiedSMS
                    .When(eVerifyEmail, (m, s, e, c) =>
                    {
                        Trace.WriteLine("Verifying Email " + c);
                        // Schedule a task for some time later to email them to ask for an SMS confirmation
                        // e.g. m.At(TimeProvider.Current.UtcNow.AddMinutes(10), eScheduledCheck);
                        return Task.FromResult(Completed);
                    })
                    .When(eScheduledCheck, (m, s, e, c) =>
                    {
                        SendReminderEmail(m, s, e, c);
                        return Task.FromResult(s);
                    });
            // event bubbling ...

            Verified
                    .When(eVerifyEmail, (m, s, e, c) => { Trace.WriteLine("Already verified " + c); return Task.FromResult(s); })
                    .When(eVerifySMS, (m, s, e, c) => { Trace.WriteLine("Already verified " + c); return Task.FromResult(s); });
        }

        public VerifyingStatemachine()
            : base()
        {
        }

        public VerifyingStatemachine(State initial)
            : base(initial)
        {
        }

        public async Task VerifiesEmail()
        {
            await this.EventHappens(eVerifyEmail, null);
        }

        public async Task VerifiesSMS()
        {
            await this.EventHappens(eVerifySMS, null);
        }

        internal async Task Starts()
        {
            await this.EventHappens(eStart, null);
        }
    }

    public class LoginOutStatemachine : StateMachineAsync<LoginOutStatemachine, Event, User>
    {
        public static Task ReportEnter(StateMachineAsync<LoginOutStatemachine, Event, User> m, Event e, State state, User user)
        {
            Trace.WriteLine(user + " entered state " + state + " via " + e);
            return Task.CompletedTask;
        }

        public static Task ReportLeave(StateMachineAsync<LoginOutStatemachine, Event, User> m, State state, Event e, User user)
        {
            Trace.WriteLine(user + " left state " + state + " via " + e);
            return Task.CompletedTask;
        }

        public static readonly State Initial = AddState("Initial", ReportEnter, ReportLeave);
        public static readonly State LoggedIn = AddState("Logged In", ReportEnter, ReportLeave);
        public static readonly State LoggedOut = AddState("Logged Out", ReportEnter, ReportLeave);
        public static readonly State Deleted = AddState("Deleted", ReportEnter, ReportLeave);

        private static readonly Event eLogsIn = new Event("Logs In");
        private static readonly Event eLogsOut = new Event("Logs Out");
        private static readonly Event eDeletesAccount = new Event("Account Deleted");

        static LoginOutStatemachine()
        {
            Initial
                    .When(eLogsIn, (m, s, e, c) => { Trace.WriteLine("Logging in " + c); return Task.FromResult(LoggedIn); })
                    .When(eDeletesAccount, (m, s, e, c) => { Trace.WriteLine("Deleting account " + c); return Task.FromResult(Deleted); });
            LoggedIn
                    .When(eLogsOut, (m, s, e, c) => { Trace.WriteLine("Logging out " + c); return Task.FromResult(LoggedOut); })
                    .When(eDeletesAccount, (m, s, e, c) => { Trace.WriteLine("Account deleted " + c); return Task.FromResult(Deleted); });
            LoggedOut
                    .When(eLogsIn, (m, s, e, c) => { Trace.WriteLine("Logging in " + c); return Task.FromResult(LoggedIn); })
                    .When(eDeletesAccount, (m, s, e, c) => { Trace.WriteLine("Account deleted " + c); return Task.FromResult(Deleted); });
        }

        public LoginOutStatemachine()
            : base()
        {
        }

        public LoginOutStatemachine(State initial)
            : base(initial)
        {
        }

        // Expose the events as public methods

        public async Task LogsIn()
        {
            await this.EventHappens(eLogsIn, null);
        }

        public async Task LogsOut()
        {
            await this.EventHappens(eLogsOut, null);
        }

        public async Task DeletesAccount()
        {
            await this.EventHappens(eDeletesAccount, null);
        }
    }

    public class StateMachineTests
    {
        [TestMethod]
        public async Task TestStateMachine()
        {
            User u = new User("User");
            await u.Created();
            Assert.AreEqual(LoginOutStatemachine.Initial, u.LState);
            Assert.AreEqual(VerifyingStatemachine.UnVerified, u.VState);

            await u.LogsIn();
            Assert.AreEqual(LoginOutStatemachine.LoggedIn, u.LState);
            Assert.AreEqual(VerifyingStatemachine.UnVerified, u.VState);

            await u.LogsOut();
            Assert.AreEqual(LoginOutStatemachine.LoggedOut, u.LState);
            Assert.AreEqual(VerifyingStatemachine.UnVerified, u.VState);

            await u.VerifiesEmail();
            Assert.AreEqual(LoginOutStatemachine.LoggedOut, u.LState);
            Assert.AreEqual(VerifyingStatemachine.VerifiedEmail, u.VState);
        }

        [TestMethod]
        public async Task TestHierarchy()
        {
            User u = new User("User");
            await u.Created();
            await u.VerifiesEmail();
            Assert.AreEqual(VerifyingStatemachine.VerifiedEmail, u.VState);
            Assert.AreEqual(VerifyingStatemachine.Verified, u.VState.ParentState);
            await u.VerifiesEmail();
            Assert.AreEqual(VerifyingStatemachine.VerifiedEmail, u.VState);
            Assert.AreEqual(VerifyingStatemachine.Verified, u.VState.ParentState);

            await u.VerifiesSMS();
            Assert.AreEqual(VerifyingStatemachine.Completed, u.VState);
            Assert.AreEqual(VerifyingStatemachine.Verified, u.VState.ParentState);
        }

        [TestMethod]
        public async Task TestHierarchyIsMethod()
        {
            User u = new User("User");
            await u.Created();                    // Needed to set some initial states (as opposed to deserialized with all state present)
            await u.VerifiesEmail();
            Assert.AreEqual(VerifyingStatemachine.VerifiedEmail, u.VState);
            Assert.AreEqual(VerifyingStatemachine.Verified, u.VState.ParentState);

            Assert.IsTrue(VerifyingStatemachine.VerifiedEmail.Is(VerifyingStatemachine.Verified));
            Assert.IsTrue(VerifyingStatemachine.VerifiedSMS.Is(VerifyingStatemachine.Verified));
            Assert.IsTrue(VerifyingStatemachine.Completed.Is(VerifyingStatemachine.Verified));

            Assert.IsFalse(VerifyingStatemachine.Completed.Is(VerifyingStatemachine.VerifiedSMS));
            Assert.IsFalse(VerifyingStatemachine.Completed.Is(VerifyingStatemachine.VerifiedEmail));
            Assert.IsFalse(VerifyingStatemachine.VerifiedEmail.Is(VerifyingStatemachine.VerifiedSMS));
            Assert.IsFalse(VerifyingStatemachine.Verified.Is(VerifyingStatemachine.UnVerified));
        }

        [TestMethod]
        public async Task SerializationTest()
        {
            var d = new ManualTimeProvider();
            TimeProvider.Current = d; // better to inject it

            User u = new User("User");

            async Task After(int minutes)
            {
                d.Add(minutes: minutes);
                await u.Tick(d.Now);
            }

            d.Add(minutes: 5);

            u.Created();
            await u.VerifiesEmail();

            await After(minutes: 10);

            await After(minutes: 10);

            await After(minutes: 5);

            await After(minutes: 10);

            await u.LogsIn();

            await After(minutes: 10);

            var nextTimedEventTime = u.VerifyingStateMachine.NextTimedEventAt;

            var xs = new XmlSerializer(typeof(User));

            string serialized = "";

            using (var ms = new MemoryStream())
            {
                xs.Serialize(ms, u);

                ms.Position = 0;

                var reader = new StreamReader(ms);
                serialized = reader.ReadToEnd();

                Trace.WriteLine(serialized);
            }

            // Now read it back in

            User u2 = (User)xs.Deserialize(new StringReader(serialized));

            Trace.WriteLine("Checking that the retrieved state is the same");
            Assert.AreEqual(VerifyingStatemachine.VerifiedEmail, u2.VState);
            Assert.AreEqual(VerifyingStatemachine.Verified, u2.VState.ParentState);
            Assert.AreEqual(nextTimedEventTime, u2.VerifyingStateMachine.NextTimedEventAt);

            // Check that the auto repeating event is still working
            int tc = u2.MessageCounterForTesting;

            await After(minutes: 5);
            tc += 5;
            Assert.AreEqual(tc, u2.MessageCounterForTesting, "No more emails should have been sent yet");

            await After(minutes: 5);
            tc += 5;
            Assert.AreEqual(tc, u2.MessageCounterForTesting, "No more emails should have been sent yet");

            await After(minutes: 20);
            tc += 20;
            Assert.AreEqual(tc, u2.MessageCounterForTesting, "Another email should have been sent");

            Trace.WriteLine("Checking that the retrieved state is the same");
            Assert.AreEqual(VerifyingStatemachine.VerifiedEmail, u2.VState);
            Assert.AreEqual(VerifyingStatemachine.Verified, u2.VState.ParentState);

            Trace.WriteLine("Checking that no more emails get sent");
            d.Add(minutes: 5);
            await u2.VerifiesSMS();
            await u2.Tick(d.Now);
            Assert.AreEqual(tc, u2.MessageCounterForTesting, "No more emails should have been sent");

            Trace.WriteLine("Checking that no more emails get sent");

            await After(minutes: 5);

            Assert.AreEqual(tc, u2.MessageCounterForTesting, "No more emails should have been sent");

            Assert.AreEqual(VerifyingStatemachine.Completed, u2.VState);
            Trace.WriteLine("Complete");
        }

        // TODO: Test Bump and other methods
    }
}