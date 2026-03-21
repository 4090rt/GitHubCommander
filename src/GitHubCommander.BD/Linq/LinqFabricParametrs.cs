using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD.Linq
{
    public interface ISortParametrs<T>
    {
        Task<List<T>> MethodLinq(DateTime date, int symbols);
    }

    public class LinqLogDates : ISortParametrs<ModelData>
    {
        private readonly ILogger _logger;
        private readonly DelegateForBd _delegateForBd;
        private readonly LINQ _lINQ;

        public LinqLogDates(ILogger logger, DelegateForBd delegateForBd, LINQ lINQ)
        {
            _logger = logger;
            _delegateForBd = delegateForBd;
            _lINQ = lINQ;
        }

        public async Task<List<ModelData>> MethodLinq(DateTime date, int symbols)
        {
            try
            {
                var result = await _lINQ.LogDates(date, symbols).ConfigureAwait(false);

                if (result != null || result.Count > 0)
                {
                    return result;
                }
                else
                {
                    _logger.LogError("Список пуст!");
                    return new List<ModelData>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение: " + ex.Message + ex.StackTrace);
                return new List<ModelData>();
            }
        }
    }

    public class SimpleFactoryParametrs
    {
        private readonly ILogger _logger;
        private readonly DelegateForBd _delegateForBd;
        private readonly LINQ _lINQ;

        public SimpleFactoryParametrs(ILogger logger, DelegateForBd delegateForBd, LINQ lINQ)
        {
            _logger = logger;
            _delegateForBd = delegateForBd;
            _lINQ = lINQ;
        }
        public ISortParametrs<T> sortirovka(String type, DateTime date, int symbols)
        {
            if (type.Equals("LogDates"))
            {
                return new LinqLogDates(_logger, _delegateForBd,3 _lINQ);
            }
            return null;
        }
    }
}
