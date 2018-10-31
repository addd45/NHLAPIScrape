using NHLAPIScrape;
using System;
using System.Threading.Tasks;

namespace NHLAPISCrape
{
    public interface INHLScraper
    {
        Task<Tuple<GameInfo, GameStatuses>> RefreshData();
    }
}