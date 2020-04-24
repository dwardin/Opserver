using Opserver.Data.SQL;

namespace Opserver.Poller.Services
{
    public class PollSql
    {
        private readonly SQLModule _sqlModule;
        public PollSql(SQLModule sqlModule)
        {
            _sqlModule = sqlModule;
        }

        public void Poll()
        {

            // _sqlModule.Poller.PollAsync();
        }

    }
}
