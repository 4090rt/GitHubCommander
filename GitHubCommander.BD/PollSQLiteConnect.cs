using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class PollSQLiteConnect
    {
        private readonly Stack<SQLiteConnection> _available = new Stack<SQLiteConnection>();
        private readonly List<SQLiteConnection> _inUse = new List<SQLiteConnection>();
        private readonly object _lock = new object();
        private readonly string _dbpath;
        private readonly int _maxCouhnt = 10;
        private readonly ILogger _loggr;

        public PollSQLiteConnect(ILogger loggr)
        {
            BDPathClass dbpath = new BDPathClass();
            _dbpath = dbpath.dbpath();
            _loggr = loggr;
        }

        public SQLiteConnection CreatePoolConnect()
        {
            try
            {
                var connect = new SQLiteConnection($"DATA SOURCE={_dbpath}");
                connect.Open();
                return connect;
            }
            catch (SQLiteException ex)
            {
                _loggr.LogError("Возникло исключение " + ex.Message + ex.StackTrace);
                throw;
            }
        }

        public SQLiteConnection PoolOpen()
        {
            try
            {
                lock (_lock)
                {
                    SQLiteConnection connection;

                    if (_available.Count > 0)
                    {
                        connection = _available.Pop();
                        if (connection == null)
                        {
                            connection = CreatePoolConnect();
                        }
                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            connection = CreatePoolConnect();
                        }
                    }
                    else if (_inUse.Count < _maxCouhnt)
                    {
                        connection = CreatePoolConnect();
                    }
                    else
                    {
                        throw new Exception("Пул занят!");
                    }

                    _inUse.Add(connection);
                    return connection;
                }
            }
            catch (SQLiteException ex)
            {
                _loggr.LogError("Возникло исключение " + ex.Message + ex.StackTrace);
                throw;
            }
        }

        public void PullClose(SQLiteConnection connection)
        {
            if (connection == null) return;
            try
            {
                lock (_lock)
                {
                    if (_inUse.Contains(connection))
                    {
                        _inUse.Remove(connection);

                        if (connection.State == System.Data.ConnectionState.Open)
                        {
                            _available.Push(connection);
                        }
                        else
                        {
                            connection.Dispose();
                        }
                    }
                    else
                    {
                        throw new Exception("Соединение не найдено");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                _loggr.LogError("Возникло исключение " + ex.Message + ex.StackTrace);
                throw;
            }
        }
    }
}
