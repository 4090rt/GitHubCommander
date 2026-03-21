using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD.Linq
{
    public interface ISort
    {
        Task<object> MethodLinq();
    }

    public interface ISort<T> : ISort
    {
        new Task<List<T>> MethodLinq();
    }

    public class LinqRealizForday : ISort<ModelData>
    {
        private readonly ILogger _logger;
        private readonly DelegateForBd _delegateForBd;
        private readonly LINQ _lINQ;

        public LinqRealizForday(ILogger logger, DelegateForBd delegateForBd, LINQ lINQ)
        {
            _logger = logger;
            _delegateForBd = delegateForBd;
            _lINQ = lINQ;
        }

        public async Task<List<ModelData>> MethodLinq()
        {
            try
            {
                var result = await _lINQ.LogForDay().ConfigureAwait(false);

                if (result != null && result.Count > 0)
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

        async Task<object> ISort.MethodLinq()
        {
            return await MethodLinq();
        }
    }

    public class LinqGroupCountinDate : ISort<ModelData2>
    {
        private readonly ILogger _logger;
        private readonly DelegateForBd _delegateForBd;
        private readonly LINQ _lINQ;

        public LinqGroupCountinDate(ILogger logger, DelegateForBd delegateForBd, LINQ lINQ)
        {
            _logger = logger;
            _delegateForBd = delegateForBd;
            _lINQ = lINQ;
        }

        public async Task<List<ModelData2>> MethodLinq()
        {
            try
            {
                var result = await _lINQ.GroupCountinDate().ConfigureAwait(false);

                if (result != null && result.Count > 0)
                {
                    return result;
                }
                else
                {
                    _logger.LogError("Список пуст!");
                    return new List<ModelData2>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение: " + ex.Message + ex.StackTrace);
                return new List<ModelData2>();
            }
        }

        async Task<object> ISort.MethodLinq()
        {
            return await MethodLinq();
        }
    }

    public class LinqRepeatLogs : ISort<ModelData2>
    {
        private readonly ILogger _logger;
        private readonly DelegateForBd _delegateForBd;
        private readonly LINQ _lINQ;

        public LinqRepeatLogs(ILogger logger, DelegateForBd delegateForBd, LINQ lINQ)
        {
            _logger = logger;
            _delegateForBd = delegateForBd;
            _lINQ = lINQ;
        }

        public async Task<List<ModelData2>> MethodLinq()
        {
            try
            {
                var result = await _lINQ.RepeatLogs().ConfigureAwait(false);

                if (result != null && result.Count > 0)
                {
                    return result;
                }
                else
                {
                    _logger.LogError("Список пуст!");
                    return new List<ModelData2>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение: " + ex.Message + ex.StackTrace);
                return new List<ModelData2>();
            }
        }

        async Task<object> ISort.MethodLinq()
        {
            return await MethodLinq();
        }
    }

    public class SimpleFactory
    {
        private readonly ILogger _logger;
        private readonly DelegateForBd _delegateForBd;
        private readonly LINQ _lINQ;

        public SimpleFactory(ILogger logger, DelegateForBd delegateForBd, LINQ lINQ)
        {
            _logger = logger;
            _delegateForBd = delegateForBd;
            _lINQ = lINQ;
        }

        public ISort sortirovka(String type)
        {
            if (type.Equals("ForDay"))
            {
                return new LinqRealizForday(_logger, _delegateForBd, _lINQ);
            }
            else if (type.Equals("CountinDate"))
            {
                return new LinqGroupCountinDate(_logger, _delegateForBd, _lINQ);
            }
            else if (type.Equals("RepeatLogs"))
            {
                return new LinqRepeatLogs(_logger, _delegateForBd, _lINQ);
            }
            return null;
        }
    }
}
