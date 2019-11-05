using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download.Pending
{
    public interface IPendingReleaseRepository : IBasicRepository<PendingRelease>
    {
        void DeleteByMovieId(int movieId);
        List<PendingRelease> AllByMovieId(int movieId);
        List<PendingRelease> WithoutFallback();
    }

    public class PendingReleaseRepository : BasicRepository<PendingRelease>, IPendingReleaseRepository
    {
        public PendingReleaseRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteByMovieId(int movieId)
        {
            Delete(movieId);
        }

        public List<PendingRelease> AllByMovieId(int movieId)
        {
            return DataMapper.Query<PendingRelease>($"SELECT * FROM {_table} WHERE MovieId = {movieId}").ToList();
        }

        public List<PendingRelease> WithoutFallback()
        {
            return new List<PendingRelease>();
            // return Query.Where(p => p.Reason != PendingReleaseReason.Fallback);
        }
    }
}
