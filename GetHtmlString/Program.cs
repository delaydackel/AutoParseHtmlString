/* 
 * Quick'n'dirty HTML parsing to find prices on a webpage  
 * Thanks https://gist.github.com/HockeyJustin for overengineering the timer stuff
 * */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestConsole
{
    public interface IProcessor
    {
        void Run();
    }

    public interface IWorker
    {
        void DoWork();
    }

    public interface ILogger
    {
        void Log(string logDetails, int level);
    }


    /// <summary>
    /// - A means of performing a process every x milliseconds.
    /// - Just need to change the interval and the worker injected (or the content of the method in the worker)
    /// - Remove the aTimer.Stop() and Start() if you want it to work every x milliseconds regardless of 
    ///   whether it has finished the previous process.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Global exception handler.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.WriteLine(DateTime.Now.ToString("yyyy-mm-dd H:mm:ss") + "... ... PROGRAM START");

            int intervalTimeInMilliseconds = 600000;
            var processor = new Processor(new Worker(new Logger()), new Logger(), intervalTimeInMilliseconds);
            processor.Run();

            // Code here will not he hit, due to Processor class' readline.
            Console.ReadLine();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            new Logger().Log("-Global unhandled exception:" + e.ExceptionObject.ToString(), 0);
            // Log exception / send notification.
        }

        public static double FindMinPrice(List<double> priceDoubles)
        {
            double tmpMin = double.MaxValue;
            foreach (var item in priceDoubles)
            {
                if (tmpMin > item)
                {
                    tmpMin = item;
                }
            }
            return tmpMin;
        }
        public static string GetResponseHtmlStr(string startUrl)
        {
            string htmlStr = "";

            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(startUrl);
                WebRequest webRequest = (WebRequest)httpWebRequest;
                webRequest.Proxy = null;
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                htmlStr = sr.ReadToEnd();
            }
            catch (WebException ex)
            {
                throw ex;
            }
            return htmlStr;
        }
        public static List<string> ParseHtmlString(string text)
        {
            var result = new List<string>();
            Regex regex = new Regex("((?<=gh_price\\\">&euro;\\s*))\\w*\\,(\\w{2}|-*)\\s*((?=<\\/span))");

            foreach (var match in regex.Matches(text))
            {
                if (!result.Contains(match.ToString()))
                {
                    result.Add(match.ToString());
                }
            }
            return result;
        }
        public static List<double> ConvertPricesToDouble(List<string> prices)
        {
            var priceAsDoubles = new List<double>();
            string tmpString;

            foreach (var item in prices)
            {
                if (item.EndsWith(",--"))
                {
                    tmpString = item.Substring(0, item.Length - 3);
                }
                else
                {
                    tmpString = item;//.Replace(",", ".");
                }
                var tmpDouble = Convert.ToDouble(tmpString);
                priceAsDoubles.Add(tmpDouble);
            }
            return priceAsDoubles;
        }
        public static void WriteStringToFile(string path, string text)
        {
            System.IO.File.AppendAllText(@path/*"C:\Temp\Public\TestFolder\WriteText.txt"*/, text);
        }
    }



    /// <summary>
    /// Main processor class. Will run a worker every x seconds and enable quitting via the console command of "exit"
    /// </summary>
    public class Processor : IProcessor
    {
        IWorker _worker;
        ILogger _logger;
        int _intervalInMilliseconds = 1000;
        System.Timers.Timer _timer;
        bool _running;

        public Processor(IWorker worker, ILogger logger, int intervalInMilliseconds)
        {
            _worker = worker;
            _logger = logger;
            _intervalInMilliseconds = intervalInMilliseconds;
        }

        public void Run()
        {
            _timer = new System.Timers.Timer();
            _timer.Elapsed += aTimer_Elapsed;
            _timer.Interval = _intervalInMilliseconds;
            _timer.Enabled = true;
            _timer.Start();

            // Use this if needing to run something at the same time as the timer. E.g a key enter for cancellation.
            _running = true;
            while (_running)
            {
                if (Console.ReadLine().ToLower() == "exit" || Console.ReadLine().ToLower() == "abort")
                {
                    Environment.Exit(0);
                }
            }
        }


        void aTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Need to stop the timer, otherwise it will continue at it's set interval wether this process has finished or not.
            // Remove the stop if you want the event to fire every x seconds as opposed to x seconds after finish.
            _timer.Stop();

            //Main method to run
            RunProcess_WithExceptionHandling();

            // Start the timer back up
            _timer.Start();
        }


        public void RunProcess_WithExceptionHandling()
        {
            try
            {
                _logger.Log("... ... Start Process", 3);

                _worker.DoWork();

                _logger.Log("... ... End Process", 3);
            }
            catch (Exception ex)
            {
                // Log exception and/or send notification.
                _logger.Log(ex.ToString(), 0);
            }
        }
    }


    /// <summary>
    /// This is the class where you do what you want to do at the specified interval
    /// </summary>
    public class Worker : IWorker
    {
        ILogger _logger;

        public Worker(ILogger logger)
        {
            _logger = logger;
        }


        /// <summary>        
        /// *** THIS IS WHERE WE DO THE WORK AT THE SET INTERVAL. IWorker.DoWork is the only method we would need to change. ***
        /// No need for standard exception handling here. Can throw up to the RunProcessHandler.
        /// </summary>
        public void DoWork()
        {
            //var bob = "";
            //bob = bob + bob;

            //for (int i = 0; i < 4; i++)
            //{
            //    System.Threading.Thread.Sleep(1000);
            //    _logger.Log("I'm Working...Sleep " + i, 3);
            //}

            var path = "C:\\Temp\\tmp.txt";
            var url1 = "https://geizhals.de/1582191";
            var url2 = "https://geizhals.de/1393943";
            var url3 = "https://geizhals.de/1582200";

            var urls = new List<string>();
            urls.Add(url1);
            urls.Add(url2);
            urls.Add(url3);
            double totalPrice = 0;
            foreach (var item in urls)
            {
                var result = Program.GetResponseHtmlStr(item);
#if DEBUG
                Console.WriteLine(result);
                //WriteStringToFile(path, result);
#endif
                var prices = Program.ParseHtmlString(result);
                var priceDoubles = Program.ConvertPricesToDouble(prices);
                var curPrice = Program.FindMinPrice(priceDoubles);
                totalPrice += curPrice;
            }
            Program.WriteStringToFile(path, totalPrice.ToString() + " at " + DateTime.Now.ToString() + Environment.NewLine);

        }
    }


    public class Logger : ILogger
    {
        public void Log(string logDetails, int level)
        {
            if (level == 0)
                Console.WriteLine(DateTime.Now.ToString("yyyy-mm-dd H:mm:ss") + ":EXCEPTION -" + logDetails);
            else
                Console.WriteLine(DateTime.Now.ToString("yyyy-mm-dd H:mm:ss") + ": " + logDetails);
        }
    }

}