using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GithubComander.src.GitHubCommander.BD.Linq
{
    public class ModelData
    { 
        public string Id { get; set; }
        public string LogText { get; set; }
        public DateTime date { get; set; }
    }

    public class ModelData2
    {
        public string Id { get; set; }
        public string LogText { get; set; }
        public DateTime Date { get; set; }
        public int count { get; set; }
    }
    public class LINQ
    {
        private readonly ILogger _logger;
        private readonly PollSQLiteConnect _pollSQLiteConnect;

        public LINQ(ILogger logger, PollSQLiteConnect pollSQLiteConnect)
        {
            _logger = logger;
            _pollSQLiteConnect = pollSQLiteConnect;
        }

        public async Task<List<ModelData>> LogDates(DateTime date, int symbols)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "SELECT Id, LogText, Date FROM LogBase WHERE Date >= @StartDate AND Date < @DateEND";

                var los = (await connection.QueryAsync<ModelData>(command, new { StartDate = date.Date, DateEND = date.AddDays(1) })).ToList();

                var sort = from p in los
                           where p.LogText.Length > symbols
                           orderby p.date descending
                           select p;

                return sort.ToList();

            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return new List<ModelData>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<ModelData>();
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }

        public async Task<List<ModelData>> LogForDay()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "SELECT Id, LogText, Date FROM LogBase WHERE Date = @date";

                var los = (await connection.QueryAsync<ModelData>(command, new { date = DateTime.Now.Date })).ToList();

                var sort = from p in los
                           orderby p.Id descending
                           select p;

                return sort.ToList();
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return new List<ModelData>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<ModelData>();
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }

        public async Task<List<ModelData2>> GroupCountinDate()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "SELECT Id, LogText, Date FROM LogBase";

                var us = (await connection.QueryAsync<ModelData2>(command)).ToList();

                var sort = (from p in us
                            group p by p.Date into g
                            orderby g.Count() descending
                            select new ModelData2
                            {
                                Date = g.Key,
                                count = g.Count()
                            }).Take(5).ToList();

                return sort.ToList();
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return new List<ModelData2>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<ModelData2>();
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }

        public async Task<List<ModelData2>> RepeatLogs()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "SELECT Id, LogText, Date FROM LogBase";

                var us = (await connection.QueryAsync<ModelData>(command)).ToList();

                var sort = (from p in us
                            group p by p.LogText into g
                            where g.Count() > 1
                            orderby g.Count() descending
                            select new ModelData2
                            {
                                LogText = g.Key,
                                count = g.Count()
                            }).Take(15).ToList();

                return sort.ToList();
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return new List<ModelData2>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<ModelData2>();
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }
    }
}
