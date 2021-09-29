using System;

namespace ROBot
{
    public interface IApplication : IDisposable
    {
        void Run();
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