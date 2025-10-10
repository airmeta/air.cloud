using Air.Cloud.Core;
using Air.Cloud.DataBase.Repositories;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DataBase.BackgroundServices
{
    public  class DatabaseStatusCheckBackgroundService
    {




        public async Task CheckDatabaseStatus(IServiceProvider serviceProvider)
        {
            AppRealization.Output.Print("数据库状态检查","开始进行数据库状态检查...");
            AppRealization.Output.Print("数据库状态检查", "正在获取数据库连接...");

            var repository = serviceProvider.GetService<IRepository>();

            await repository.Sql().SqlNonQueryAsync("SELECT SYS_DATE FROM DUAL");







        }




    }
}
