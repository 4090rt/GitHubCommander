using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Data
{
    public class DataModelRepositoryInfo
    {
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Description { get; set; }
            public int StargazersCount { get; set; }
            public DateTime UpdatedAt { get; set; }
            public string DefaultBranch { get; set; }
    }
}
