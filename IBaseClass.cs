using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;




public enum SortType
{
    Ascending, Descending, None
}

public class Counter
{
    public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
    public string ObjectName { get; set; } = "";
    public long Count { get; set; } = 1;
}
public class IBaseClass
{
    [BsonRequired]
    public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
    public string _sid { get; set; } = "";
    public long _cid { get; set; } = 0;
    public bool isCid { get; set; } = false;
    public bool isDeleted { get; set; } = false;
    [BsonDateTimeOptions]
    public DateTime _CreatedDate { get; set; } = DateTime.Now;
    [BsonDateTimeOptions]
    public DateTime LastUpdateDate { get; set; } = DateTime.Now;


    /// <summary>
    /// MongoDB database connection in the format:
    /// mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
    /// See http://www.mongodb.org/display/DOCS/Connections
    /// </summary>
    public IMongoDatabase GetDatabase()
    {
        return new MongoClient("Connection String for MongoDB").GetDatabase("MongoDB Database Name");
    }

    /// <summary>
    /// Get Collection for MongDB
    /// Document Type BsonDocument
    /// </summary>
    public IMongoCollection<BsonDocument> GetCollection()
    {
        IMongoDatabase _database = GetDatabase();
        var collection = _database.GetCollection<BsonDocument>(GetCurrentNamespace());
        if (collection == null)
        {
            _database.CreateCollectionAsync(GetCurrentNamespace()).Wait();
            collection = _database.GetCollection<BsonDocument>(GetCurrentNamespace());
        }
        return collection;
    }
    /// <summary>
    /// Get Collection for MongDB with Namespace
    /// Document Type BsonDocument
    /// </summary>
    public IMongoCollection<BsonDocument> GetCollection(string _namespace)
    {
        IMongoDatabase _database = GetDatabase();
        var collection = _database.GetCollection<BsonDocument>(_namespace);
        if (collection == null)
        {
            _database.CreateCollectionAsync(_namespace).Wait();
            collection = _database.GetCollection<BsonDocument>(_namespace);
        }
        return collection;
    }
    /// <summary>
    /// Get Collection for MongDB with Namespace
    /// Document Type T
    /// </summary>
    public IMongoCollection<T> GetCollection<T>(string _namespace)
    {
        IMongoDatabase _database = GetDatabase();
        var collection = _database.GetCollection<T>(_namespace);
        if (collection == null)
        {
            _database.CreateCollectionAsync(_namespace).Wait();
            collection = _database.GetCollection<T>(_namespace);
        }
        return collection;
    }
    /// <summary>
    /// Get Collection for MongDB
    /// Document Type T
    /// </summary>
    public IMongoCollection<T> GetCollection<T>()
    {
        IMongoDatabase _database = GetDatabase();
        var collection = _database.GetCollection<T>(GetCurrentNamespace());
        if (collection == null)
        {
            _database.CreateCollectionAsync(GetCurrentNamespace()).Wait();
            collection = _database.GetCollection<T>(GetCurrentNamespace());
        }
        return collection;
    }
    public string GetCurrentNamespace()
    {
        return this.GetType().Namespace;
    }
    public string GetCurrentClassName()
    {
        return this.GetType().Name;
    }
    public Dictionary<string, object> GetCurrentProperties()
    {
        Dictionary<string, object> propertyList = new Dictionary<string, object>();
        if (this != null)
        {
            foreach (var prop in this.GetType().GetProperties())
            {
                propertyList[prop.Name] = prop.GetValue(this, null);
            }
        }
        return propertyList;
    }
    public long GetNewSequanceValue()
    {
        string ObjectName = GetCurrentNamespace() + "." + GetCurrentClassName();
        var collection = GetCollection<Counter>("BaseCounters");
        var filter = Builders<Counter>.Filter.Eq("ObjectName", ObjectName);
        var cursor = collection.Find(filter).ToList();
        if (cursor.Count > 0)
            return cursor[0].Count;
        else
        {
            Counter count = new Counter();
            count.ObjectName = ObjectName;
            collection.InsertOneAsync(count).Wait();
            return 1;
        }
    }
    public void IncSequanceValue()
    {
        string ObjectName = GetCurrentNamespace() + "." + GetCurrentClassName();
        var collection = GetCollection<Counter>("mdbs");
        var filter = Builders<Counter>.Filter.Eq("ObjectName", ObjectName);
        var update = Builders<Counter>.Update.Inc("Count", 1);
        collection.UpdateOne(filter, update);
    }
    public bool Insert()
    {
        try
        {
            if (isCid)
                this._cid = GetNewSequanceValue();
            var collection = GetCollection();
            collection.InsertOneAsync(this.ToBsonDocument()).Wait();
            if (isCid)
                IncSequanceValue();
            return true;
        }
        catch { return false; }
    }
    public bool InsertMany<T>(List<T> objects)
    {
        try
        {
            foreach (var t in objects)
            {
                if (isCid)
                    this._cid = GetNewSequanceValue();
                var collection = GetCollection();
                collection.InsertOneAsync(this.ToBsonDocument()).Wait();
                if (isCid)
                    IncSequanceValue();
            }
            return true;
        }
        catch { return false; }
    }
    public UpdateResult Update()
    {
        var collection = GetCollection();
        var filter = Builders<BsonDocument>.Filter.Eq("_id", this._id);
        var updList = new List<UpdateDefinition<BsonDocument>>();
        foreach (var prop in this.GetType().GetProperties())
        {
            var upd = Builders<BsonDocument>.Update.Set(prop.Name, prop.GetValue(this, null));
            updList.Add(upd);
        }
        var finalUpd = Builders<BsonDocument>.Update.Combine(updList);
        return collection.UpdateOne(filter, finalUpd, new UpdateOptions { IsUpsert = true });
    }
    public UpdateResult Remove()
    {
        var collection = GetCollection();
        var filter = Builders<BsonDocument>.Filter.Eq("_id", this._id);
        var upd = Builders<BsonDocument>.Update.Set("isDeleted", true);
        return collection.UpdateOne(filter, upd, new UpdateOptions { IsUpsert = true });
    }
    public bool Fill<T>(object id) where T : class
    {
        try
        {
            ObjectId idd = new ObjectId();
            if (!ObjectId.TryParse(id.ToString(), out idd))
            {
                return false;
            }
            var collection = GetCollection<T>();
            var filter = Builders<T>.Filter.Eq("_id", idd);
            var cursor = collection.Find(filter).ToList();

            object obj;
            if (cursor.Count == 0 || cursor.Count > 1)
            {
                return false;
            }
            else
            {
                obj = cursor[0];
                foreach (var prop in this.GetType().GetProperties())
                {
                    prop.SetValue(this, obj.GetType().GetProperty(prop.Name).GetValue(obj, null));

                }
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
    public T GetOne<T>(object id) where T : class
    {

        var collection = GetCollection<T>();
        ObjectId idd = new ObjectId();
        if (!ObjectId.TryParse(id.ToString(), out idd))
        {
            return default(T);
        }
        var filter = Builders<T>.Filter.Eq("_id", idd);
        var cursor = collection.Find(filter).ToList();
        if (cursor.Count == 0 || cursor.Count > 1)
        {
            return default(T);
        }
        else
        {
            return cursor[0];
        }
    }
    public List<T> GetAll<T>(bool _isDeleted) where T : class
    {
        var collection = GetCollection<T>();
        var filter = Builders<T>.Filter.Eq("_t", GetCurrentClassName()) & Builders<T>.Filter.Eq("isDeleted", _isDeleted);
        var cursor = collection.Find(filter).ToList();
        if (cursor.Count == 0)
        {
            return default(List<T>);
        }
        else
        {
            return cursor;
        }
    }
    public List<T> GetbyAtribute<T>(FieldDefinition<T> t, object val)
    {
        var collection = GetCollection<T>();
        var filter = Builders<T>.Filter.Eq("_t", GetCurrentClassName()) & Builders<T>.Filter.Eq("isDeleted", false) & Builders<T>.Filter.AnyEq(t, val);
        var cursor = collection.Find(filter).ToList();

        if (cursor.Count > 0)
            return BsonSerializer.Deserialize<List<T>>(cursor.ToJson());
        else return null;
    }
    public List<T> GetbyAtribute<T>(string atribute, object atributeValue) where T : class
    {
        var collection = GetCollection<T>();
        var filter = Builders<T>.Filter.Eq("_t", GetCurrentClassName()) & Builders<T>.Filter.Eq("isDeleted", false) & Builders<T>.Filter.Eq(atribute, atributeValue);
        var cursor = collection.Find(filter).ToList();
        if (cursor.Count > 0)
            return cursor;
        else return null;
    }
    public List<T> GetbyAtribute<T>(string atribute, object atributeValue, string ShortingAtribute, SortType type)
    {
        var collection = GetCollection();
        var filter = Builders<BsonDocument>.Filter.Eq("_t", GetCurrentClassName()) & Builders<BsonDocument>.Filter.Eq("isDeleted", false) & Builders<BsonDocument>.Filter.Eq(atribute, atributeValue);
        SortDefinition<BsonDocument> sort = null;
        if (type == SortType.Ascending)
            sort = Builders<BsonDocument>.Sort.Ascending(ShortingAtribute);
        else if (type == SortType.Descending)
            sort = Builders<BsonDocument>.Sort.Descending(ShortingAtribute);

        if (type != SortType.None)
        {
            var cursor = collection.Find(filter).Sort(sort).ToList();
            return BsonSerializer.Deserialize<List<T>>(cursor.ToJson());
        }
        else
        {
            var cursor = collection.Find(filter).ToList();
            return BsonSerializer.Deserialize<List<T>>(cursor.ToJson());
        }
    }
    public List<T> GetDataList<T>(int CurrentPage, int PageSize, string ShortingAtribute, SortType sorttype)
    {
        var collection = GetCollection<T>();
        var filter = Builders<T>.Filter.Eq("_t", GetCurrentClassName()) & Builders<T>.Filter.Eq("isDeleted", false);
        SortDefinition<T> sort = null;
        if (sorttype == SortType.Ascending)
            sort = Builders<T>.Sort.Ascending(ShortingAtribute);
        else if (sorttype == SortType.Descending)
            sort = Builders<T>.Sort.Descending(ShortingAtribute);
        if (sorttype != SortType.None)
        {
            var cursor = collection.Find(filter).Sort(sort).Skip((CurrentPage - 1) * PageSize).Limit(PageSize).ToList();
            return cursor;
        }
        else
        {
            var cursor = collection.Find(filter).Skip((CurrentPage - 1) * PageSize).Limit(PageSize).ToList();
            return cursor;
        }
    }
}
