using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using Dapper.Contrib.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Datastore
{
    public interface IBasicRepository<TModel> where TModel : ModelBase, new()
    {
        IEnumerable<TModel> All();
        int Count();
        TModel Get(int id);
        IEnumerable<TModel> Get(IEnumerable<int> ids);
        TModel SingleOrDefault();
        TModel Insert(TModel model);
        TModel Update(TModel model);
        TModel Upsert(TModel model);
        void Delete(int id);
        void Delete(TModel model);
        void InsertMany(IList<TModel> model);
        void UpdateMany(IList<TModel> model);
        void DeleteMany(List<TModel> model);
        void Purge(bool vacuum = false);
        bool HasItems();
        void DeleteMany(IEnumerable<int> ids);
        void SetFields(TModel model, params Expression<Func<TModel, object>>[] properties);
        TModel Single();
        PagingSpec<TModel> GetPaged(PagingSpec<TModel> pagingSpec);
    }

    public class BasicRepository<TModel> : IBasicRepository<TModel> where TModel : ModelBase, new()
    {
        private readonly IDatabase _database;
        private readonly IEventAggregator _eventAggregator;

        protected readonly string _table;
        protected string _template;

        protected IDbConnection DataMapper => _database.GetDataMapper();

        public BasicRepository(IDatabase database, IEventAggregator eventAggregator)
        {
            _database = database;
            _eventAggregator = eventAggregator;

            _table = TableMapping.TableNameMapping(typeof(TModel));
            _template = $"SELECT /**select**/ FROM {_table} /**leftjoin**/ /**innerjoin**/ /**where**/ /**--parameters**/";
        }

        protected virtual SqlBuilder Builder()
        {
            return new SqlBuilder().Select("*");
        }

        protected virtual TModel SelectOne(SqlBuilder.Template sql)
        {
            Console.WriteLine($"Sql: {sql.RawSql}, Params: {sql.Parameters}");
            return DataMapper.QueryFirstOrDefault<TModel>(sql.RawSql, sql.Parameters);
        }

        protected virtual IEnumerable<TModel> SelectMany(SqlBuilder.Template sql)
        {
            Console.WriteLine($"Sql: {sql.RawSql}, Params: {sql.Parameters}");
            return DataMapper.Query<TModel>(sql.RawSql, sql.Parameters);
        }

        public virtual IEnumerable<TModel> All()
        {
            var sql = Builder().AddTemplate(_template);
            return SelectMany(sql);
        }

        public int Count()
        {
            return DataMapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {_table}");
        }

        public TModel Get(int id)
        {
            var sql = Builder()
                .Where($"Id = {id}")
                .AddTemplate(_template);
            var model = SelectOne(sql);

            if (model == null)
            {
                throw new ModelNotFoundException(typeof(TModel), id);
            }

            return model;
        }

        public IEnumerable<TModel> Get(IEnumerable<int> ids)
        {
            var idList = ids.ToList();
            var sql = Builder()
                .Where($"Id in @Ids")
                .AddTemplate(_template, new {Ids = idList});

            var result = SelectMany(sql);

            var count = result.Count();
            if (count != idList.Count)
            {
                throw new ApplicationException($"Expected query to return {idList.Count} rows but returned {count}");
            }

            return result;
        }

        public TModel SingleOrDefault()
        {
            return All().SingleOrDefault();
        }

        public TModel Single()
        {
            return All().Single();
        }

        public TModel Insert(TModel model)
        {
            if (model.Id != 0)
            {
                throw new InvalidOperationException("Can't insert model with existing ID " + model.Id);
            }

            DataMapper.Insert<TModel>(model);

            ModelCreated(model);

            return model;
        }

        public void InsertMany(IList<TModel> models)
        {
            if (models.Any(x => x.Id == 0))
            {
                throw new InvalidOperationException("Can't insert model with existing ID != 0");
            }

            DataMapper.Insert<IList<TModel>>(models);
        }

        public TModel Update(TModel model)
        {
            if (model.Id == 0)
            {
                throw new InvalidOperationException("Can't update model with ID 0");
            }

            DataMapper.Update<TModel>(model);

            ModelUpdated(model);

            return model;
        }

        public void UpdateMany(IList<TModel> models)
        {
            if (models.Any(x => x.Id == 0))
            {
                throw new InvalidOperationException("Can't update model with ID 0");
            }

            DataMapper.Update<IList<TModel>>(models);
        }


        public void Delete(TModel model)
        {
            Delete(model);
        }

        public void Delete(int id)
        {
            Get(id);
            Delete(id);
        }

        public void DeleteMany(IEnumerable<int> ids)
        {
            Get(ids);
            DeleteMany(ids);
        }

        public void DeleteMany(List<TModel> models)
        {
            DataMapper.Delete<List<TModel>>(models);
        }

        public TModel Upsert(TModel model)
        {
            if (model.Id == 0)
            {
                Insert(model);
                return model;
            }
            Update(model);
            return model;
        }

        public void Purge(bool vacuum = false)
        {
            DataMapper.DeleteAll<TModel>();
            if (vacuum)
            {
                Vacuum();
            }
        }

        protected void Vacuum()
        {
            _database.Vacuum();
        }

        public bool HasItems()
        {
            return Count() > 0;
        }

        public void SetFields(TModel model, params Expression<Func<TModel, object>>[] properties)
        {
            if (model.Id == 0)
            {
                throw new InvalidOperationException("Attempted to updated model without ID");
            }

            // DataMapper.Update<TModel>()
            //     .Where(c => c.Id == model.Id)
            //     .ColumnsIncluding(properties)
            //     .Entity(model)
            //     .Execute();

            ModelUpdated(model);
        }

        public virtual PagingSpec<TModel> GetPaged(PagingSpec<TModel> pagingSpec)
        {
            // pagingSpec.Records = GetPagedQuery(Query, pagingSpec).ToList();
            // pagingSpec.TotalRecords = GetPagedQuery(Query, pagingSpec).GetRowCount();

            return pagingSpec;
        }

        // protected virtual SortBuilder<TModel> GetPagedQuery(QueryBuilder<TModel> query, PagingSpec<TModel> pagingSpec)
        // {
        //     var filterExpressions = pagingSpec.FilterExpressions;
        //     var sortQuery = query.Where(filterExpressions.FirstOrDefault());

        //     if (filterExpressions.Count > 1)
        //     {
        //         // Start at the second item for the AndWhere clauses
        //         for (var i = 1; i < filterExpressions.Count; i++)
        //         {
        //             sortQuery.AndWhere(filterExpressions[i]);
        //         }
        //     }

        //     return sortQuery.OrderBy(pagingSpec.OrderByClause(), pagingSpec.ToSortDirection())
        //                     .Skip(pagingSpec.PagingOffset())
        //                     .Take(pagingSpec.PageSize);
        // }

        protected void ModelCreated(TModel model)
        {
            PublishModelEvent(model, ModelAction.Created);
        }

        protected void ModelUpdated(TModel model)
        {
            PublishModelEvent(model, ModelAction.Updated);
        }

        protected void ModelDeleted(TModel model)
        {
            PublishModelEvent(model, ModelAction.Deleted);
        }

        private void PublishModelEvent(TModel model, ModelAction action)
        {
            if (PublishModelEvents)
            {
                _eventAggregator.PublishEvent(new ModelEvent<TModel>(model, action));
            }
        }

        protected virtual bool PublishModelEvents => false;
    }
}
