using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using PortScanner.Model;

namespace PortScanner
{
    /**
     * This executor class responsible for scanner creation and tasks for each scanner.
     * It builds executors for each port range then creates parallel scans from given Scanner classes.
     * 
     * 
     */
    class ScannerExecutor
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly int _numberOfTasks;
        private readonly List<Task> _runningTasks;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ScannerExecutor(int numberOfTasks)
        {
            logger.Debug("Starting scanners via # tasks {} ", numberOfTasks);
            this._numberOfTasks = numberOfTasks;
            _runningTasks = new List<Task>(numberOfTasks);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Build and execute scanners for given ip range. Split given ip range to the different Tasks.
        /// Each scanner will be responsible for one chunk of ips.
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="callback"></param>
        /// <param name="quickScan"></param>
        public void BuildExecutor(IEnumerable<IPAddress> addresses, Action<OpenPort> callback,
            bool quickScan)
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            //batch ips to processors
            int ipCount = addresses.Count();
            int chunkSize = 0;
            if (ipCount > _numberOfTasks)
            {
                chunkSize = ipCount / _numberOfTasks;
            }
            else
            {
                logger.Trace("No need to split chunks.");
            }

            logger.Debug("Chunk size for each scanner: {} ", chunkSize);
            IEnumerable<List<IPAddress>> enumerable = SplitChunks<IPAddress>(addresses, chunkSize);

            Parallel.ForEach(enumerable, (ipList =>
            {
                var scanner = new Scanner(ipList);
                var task = Task.Run(() => scanner.ScanAsync(callback, quickScan, cancellationToken),
                    cancellationToken);

                _runningTasks.Add(task);
            }));
        }

        /// <summary>
        /// Split given whole ip range into the chunks so that we can share with unique scanner in parallel
        /// </summary>
        /// <typeparam name="IPAddress"></typeparam>
        /// <param name="source"></param>
        /// <param name="chunkSize"></param>
        /// <returns></returns>
        private static IEnumerable<List<IPAddress>> SplitChunks<IPAddress>(IEnumerable<IPAddress> source, int chunkSize)
        {
            var toReturn = new List<IPAddress>(chunkSize);
            foreach (var item in source)
            {
                toReturn.Add(item);
                if (toReturn.Count != chunkSize) continue;
                yield return toReturn;
                toReturn = new List<IPAddress>(chunkSize);
            }

            if (toReturn.Any())
            {
                yield return toReturn;
            }
        }

        /// <summary>
        /// Shutdown Signal for all the Tasks created.
        /// </summary>
        public void StopAllTasks()
        {
            logger.Info("Cancelling all the tasks...");
            _cancellationTokenSource.Cancel();
        }

        public List<Task> GetRunningTasks()
        {
            _runningTasks.RemoveAll(task => task == null);
            return _runningTasks;
        }
    }
}