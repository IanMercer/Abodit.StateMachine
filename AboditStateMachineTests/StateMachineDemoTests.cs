using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Abodit.StateMachine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AboditStateMachineTests
{
    [TestClass]
    public class StateMachineDemoTests
    {
        [TestMethod]
        public async Task StateMachineDemoBehaves()
        {
            DemoStatemachine demoStateMachine = new DemoStatemachine(DemoStatemachine.UnVerified);
            DemoContext dc = new DemoContext { UserName = "fred" };

            demoStateMachine.StateChanges += DemoStateMachine_StateChanges;

            var time = new ManualTimeProvider(DateTime.UtcNow);

            await demoStateMachine.Tick(time.Now, dc);
            Trace.WriteLine("Next time to tick = " + demoStateMachine.NextTimedEventAt);

            time.AddTime(days: 1);
            await demoStateMachine.Tick(time.Now, dc);
            Trace.WriteLine("Next time to tick = " + demoStateMachine.NextTimedEventAt);

            time.AddTime(days: 1);
            await demoStateMachine.Tick(time.Now, dc);
            Trace.WriteLine("Next time to tick = " + demoStateMachine.NextTimedEventAt);

            time.AddTime(days: 1);
            // Finally the user verifies their email
            await demoStateMachine.VerifiesEmail(dc);

            time.AddTime(days: 1);
            await demoStateMachine.Tick(time.Now, dc);

            time.AddTime(days: 1);
            await demoStateMachine.Tick(time.Now, dc);
        }

        [TestMethod]
        public async Task TestMethod1()
        {
            DemoStatemachine demoStateMachine = new DemoStatemachine(DemoStatemachine.UnVerified);
            DemoContext dc = new DemoContext { UserName = "fred" };

            demoStateMachine.StateChanges += DemoStateMachine_StateChanges;

            // Normally you would be looking at the NextTimeEventAt field, and when it's after UtcNow you would call Tick()
            // For this test, simulate several days going by ...

            await demoStateMachine.Tick(DateTime.UtcNow.AddDays(1), dc);
            Trace.WriteLine("Next time to tick = " + demoStateMachine.NextTimedEventAt);

            await demoStateMachine.Tick(DateTime.UtcNow.AddDays(2), dc);
            Trace.WriteLine("Next time to tick = " + demoStateMachine.NextTimedEventAt);

            await demoStateMachine.Tick(DateTime.UtcNow.AddDays(3), dc);
            Trace.WriteLine("Next time to tick = " + demoStateMachine.NextTimedEventAt);

            await demoStateMachine.Tick(DateTime.UtcNow.AddDays(4), dc);

            // Finally the user verifies their email
            await demoStateMachine.VerifiesEmail(dc);

            await demoStateMachine.Tick(DateTime.UtcNow.AddDays(5), dc);

            await demoStateMachine.Tick(DateTime.UtcNow.AddDays(6), dc);
        }

        private void DemoStateMachine_StateChanges(StateTransitionArgs<DemoContext, DemoStatemachine.State> transitionArgs)
        {
            Trace.WriteLine("New state = " + transitionArgs.NewState);
        }

        /// <summary>
        /// The ManualTimeProvider is good for testing - you can manually advance time as you wish to simulate real time
        /// </summary>
        public class ManualTimeProvider
        {
            private DateTime now;

            public DateTime Now
            {
                get { return now; }
                set { this.now = value; }
            }

            public ManualTimeProvider() : this(DateTime.UtcNow)
            {
            }

            public ManualTimeProvider(DateTime initialTime)
            {
                this.now = initialTime;
            }

            public void AddTime(int days = 0, int hours = 0, int minutes = 0, int seconds = 0)
            {
                this.now = this.now.AddDays(days).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
                Trace.WriteLine("Time advanced to " + this.now.ToString("hh:mm"));
            }
        }
    }
}