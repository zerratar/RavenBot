using System;
using System.Threading.Tasks;

namespace ROBot.Core
{
    public interface IApplication : IDisposable
    {
        Task RunAsync();
        void Shutdown();
    }

    //public interface IProcessStatistics
    //{
    //    IProcessStatisticsReport Report(string key, int value);
    //}

    //public interface IProcessStatisticsReport
    //{
    //}
}