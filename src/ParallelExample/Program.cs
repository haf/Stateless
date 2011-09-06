using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stateless;

namespace ParallelExample
{
    public static class C
    {
        static SequentialActionQueue q = new SequentialActionQueue();

        public static void WriteLine(ConsoleColor color, string format, params object[] items)
        {
            q.Enqueue(() =>
                {
                    var oldColor = Console.BackgroundColor;
                    Console.BackgroundColor = color;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(format, items);
                    Console.BackgroundColor = oldColor;
                }); 
        }
    }

    public class MyAsyncStateMachine
    {
        static string on = "On", off = "Off";
        static char space = ' ';
        static Random rnd = new Random();

        ConsoleColor color;

        StateMachine<string, char> stateMachine;

        SequentialActionQueue queue = new SequentialActionQueue();
        
        public MyAsyncStateMachine()
        {
            this.color = (ConsoleColor)rnd.Next(15);
            this.stateMachine = CreateMachine();
            this.queue.PropertyChanged += (o, e) => C.WriteLine(this.color, "{2} {0} = {1}", e.PropertyName, this.queue.IsActive, this.stateMachine.Id);
        }

        public void Fire(char trigger, int i)
        {
            queue.Enqueue(() =>
            {
                C.WriteLine(this.color, "actually starting {0}", i);
                stateMachine.Fire(trigger);
            });
        }

        private StateMachine<string, char> CreateMachine()
        {
            var machine = new StateMachine<string, char>(off);

            Action log = () =>
            {
                C.WriteLine(this.color, "switched pre {1}: {0}", machine.Id, machine.State);
                //Thread.Sleep(1000);
                C.WriteLine(this.color, "switched post {1}: {0}", machine.Id, machine.State);
            };

            machine
                .Configure(off)
                .Permit(space, on)
                .OnEntry(log);
            machine
                .Configure(on)
                .Permit(space, off)
                .OnEntry(log);
            return machine;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var results = CreateMachines(3);
            
            Parallel.ForEach(results, m =>
                {
                    Parallel.For(0, 10, i =>
                        {
                            C.WriteLine(ConsoleColor.Black, "firing {0}", i);
                            m.Fire(' ', i);
                        });
                });


            C.WriteLine(ConsoleColor.Black, "wait 'till done");
            Console.ReadKey();


            Parallel.ForEach(results, m =>
            {
                Parallel.For(0, 10, i =>
                {
                    C.WriteLine(ConsoleColor.Black, "firing {0}", i);
                    m.Fire(' ', i);
                });
            });

            Console.ReadKey();
        }

        private static IEnumerable<MyAsyncStateMachine> CreateMachines(int num)
        {
            for (int i = 0; i < num; i++)
            {
                yield return new MyAsyncStateMachine();
            }
        }
    }
}
