using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        /// <summary>
        /// Start tailing the file at user-specified path.
        /// </summary>
        public class StartTail
        {
            public IActorRef ReporterActor { get; set; }

            public string FilePath { get; }

            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }
        }

        /// <summary>
        /// Stop tailing the file at user-specified path.
        /// </summary>
        public class StopTail
        {
            public string FilePath { get; }

            public StopTail(string filePath)
            {
                FilePath = filePath;
            }
        }

        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;

                // here we are creating our first parent/child relationship!
                // the TailActor instance is created here is a child
                // of this instance of TailCoordinatorActor
                Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // max number of retries
                TimeSpan.FromSeconds(30), // withing time range
                x => // local only decider
                {
                    if (x is ArithmeticException)
                        return Directive.Resume;

                    // error that we cannot recover from, stop the failing actor
                    else if (x is NotSupportedException)
                        return Directive.Stop;

                    // in all other cases, just restart the failing actor
                    else
                        return Directive.Restart;
                });
        }
    }
}
