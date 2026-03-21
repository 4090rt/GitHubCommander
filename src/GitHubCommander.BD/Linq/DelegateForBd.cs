using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class DelegateForBd
    {
        public delegate Task<T> NotParametrsDelegate<T>(Func<Task<T>> method);
        public delegate Task<T> ParametrsDelegate1<T>(Func<DateTime, int, Task<T>> method, DateTime date, int symbols);

        private readonly ILogger _logger;

        public DelegateForBd(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<T> DelegateRunNotParametrs<T>(NotParametrsDelegate<T> delegates, Func<Task<T>> method)
        {
            var result = await delegates.Invoke(method);
            return result;
        }

        public async Task<T> DelegateNotParametrsReqliz<T>(Func<Task<T>> method)
        {
            try
            {
                var result = await method().ConfigureAwait(false);

                if (result != null)
                {
                    if (result is System.Collections.IEnumerable enumerable && result is not string)
                    {
                        foreach (var item in enumerable)
                        {
                            _logger.LogWarning(item?.ToString());
                        }
                    }
                    else
                    {
                        _logger.LogWarning(result?.ToString());
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return default;
            }
        }

        public async Task<T> DelegateRunFromParametrs<T>(ParametrsDelegate1<T> delegates, Func<DateTime, int, Task<T>> method, DateTime date, int symbols)
        {
            var result = await delegates.Invoke(method, date, symbols);
            return result;
        }

        public async Task<T> DelegateParametrs1Reqliz<T>(Func<DateTime, int, Task<T>> method, DateTime date, int symbols)
        {
            try
            {
                var result = await method(date, symbols).ConfigureAwait(false);

                if (result != null)
                {
                    if (result is System.Collections.IEnumerable enumerable && result is not string)
                    {
                        foreach (var item in enumerable)
                        {
                            _logger.LogWarning(item?.ToString());
                        }
                    }
                    else
                    {
                        _logger.LogWarning(result?.ToString());
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return default;
            }
        }
    }
}
