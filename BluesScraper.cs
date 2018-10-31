using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using NHLAPIScrape;

namespace NHLAPISCrape.BluesScraper
{
    public class BluesScraper: INHLScraper, IDisposable
    {
        string _url;
        HttpClient _httpClient;
        JToken _gameData, _liveData;

        public BluesScraper(int gameCode)
        {
            _url = string.Format("https://statsapi.web.nhl.com/api/v1/game/{0}/feed/live", gameCode);
            _httpClient = new HttpClient();
        }

        public async Task<Tuple<GameInfo, GameStatuses>> RefreshData()
        {
            try
            {
                //TODO: better error handlin
                string rawJson = await _httpClient.GetStringAsync(_url);
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    throw new Exception();
                }

                var jsonObj = JObject.Parse(rawJson);
                _gameData = jsonObj["gameData"];
                _liveData = jsonObj["liveData"];

                var gameInfo = ExtractData();
                string gameStatus = GetJsonValue(_gameData["status"]["statusCode"]);

                bool fail = !Enum.TryParse(gameStatus, out GameStatuses status);

                if (fail)
                {
                    //Failsafe
                    status = GameStatuses.Final;
                }
                //End of Period
                bool gameLive = (status == GameStatuses.InAction || status == GameStatuses.CriticalAction);
                if (gameInfo.TimeRemaining < 1 && gameLive)
                {
                    status = GameStatuses.Intermission;
                }

                return Tuple.Create(gameInfo, status);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async static Task<Tuple<string, string>> GetNextGameTimeAndCode(string url)
        {
            using (var client = new HttpClient())
            {
                //TODO: better error handlin
                string rawJson = await client.GetStringAsync(url);
                var jsonObj = JObject.Parse(rawJson);

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    throw new Exception();
                }
                var usefulNode = jsonObj["teams"][0]["nextGameSchedule"]["dates"][0]["games"][0];
                string utcTime = GetJsonValue(usefulNode["gameDate"]);
                string gameCode = GetJsonValue(usefulNode["gamePk"]);

                return Tuple.Create(utcTime, gameCode);
            }
        }

        public static TimeSpan GetDelayTime(GameStatuses status)
        {
            switch (status)
            {
                case GameStatuses.CriticalAction:
                    return TimeSpan.FromSeconds(1);
                case GameStatuses.Intermission:
                case GameStatuses.NotStarted:
                    return TimeSpan.FromSeconds(90);
                case GameStatuses.Preview:
                    return TimeSpan.FromSeconds(30);
                case GameStatuses.InAction:
                    return TimeSpan.FromSeconds(5);
                case GameStatuses.Final:
                default:
                    return TimeSpan.Zero;
            }

        }

        private GameInfo ExtractData()
        {
            GameInfo gameInfo = new GameInfo();

            //Live Data
            var lineScore = _liveData["linescore"];
            gameInfo.WereHome = (GetJsonValue(_gameData["teams"]["home"]["id"]) == "19");
            gameInfo.HomeScore = int.Parse(GetJsonValue(lineScore["teams"]["home"]["goals"]));
            gameInfo.AwayScore = int.Parse(GetJsonValue(lineScore["teams"]["away"]["goals"]));
            gameInfo.HomeSOG = int.Parse(GetJsonValue(lineScore["teams"]["home"]["shotsOnGoal"]));
            gameInfo.AwaySOG = int.Parse(GetJsonValue(lineScore["teams"]["away"]["shotsOnGoal"]));
            gameInfo.Period = int.Parse(GetJsonValue(lineScore["currentPeriod"]));
            TimeSpan.TryParse(GetJsonValue(lineScore["currentPeriodTimeRemaining"]), out TimeSpan temp);
            gameInfo.TimeRemaining = temp.TotalSeconds;
            gameInfo.TimeRemaining = (gameInfo.TimeRemaining == 0) ? -1 : gameInfo.TimeRemaining;

            return gameInfo;
        }

        private static string GetJsonValue(JToken token)
        {
            try
            {
                return token.Value<string>().Trim();
            }
            //not sure if needed but why not
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

    }
}
