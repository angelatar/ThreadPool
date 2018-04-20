using System;
using System.Threading.Tasks;
using ThreadPoolLib;
using System.Threading;

namespace ThreadPool
{
    class Program
    {
        static void Main(string[] args)
        {


            MyThreadPool pool = new MyThreadPool();


            for (int i = 0; i < 8; i++)
            {
                var temp = i;
                Thread.Sleep(300);
                pool.Execute(new MyTask(() =>
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Running thread ID =  "+Thread.CurrentThread.Name+"  "+temp);
                }));
            }

            pool.Stop();
            Console.WriteLine();


        }
    }
}
