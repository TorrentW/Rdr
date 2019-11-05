using System;
using System.Data;
using Dapper.Contrib.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;

namespace NzbDrone.Core.Datastore
{
    public interface IMainDatabase : IDatabase
    {

    }

    public class MainDatabase : IMainDatabase
    {
        private readonly IDatabase _database;

        public MainDatabase(IDatabase database)
        {
            _database = database;

        }

        public IDbConnection GetDataMapper()
        {
            return _database.GetDataMapper();
        }

        public Version Version => _database.Version;

        public int Migration => _database.Migration;

        public void Vacuum()
        {
            _database.Vacuum();
        }
    }
}
