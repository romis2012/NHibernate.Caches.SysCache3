using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Caching;
using NHibernate.Cache;
using Environment = NHibernate.Cfg.Environment;
using System.Threading.Tasks;
using System.Threading;

namespace NHibernate.Caches.SysCache3
{
	public class SysCacheRegion : ICache
	{
		private const string CacheKeyPrefix = "NHibernate-Cache:";
		private static readonly TimeSpan defaultRelativeExpiration = TimeSpan.FromSeconds(300);
		private static readonly IInternalLogger log = LoggerProvider.LoggerFor((typeof(SysCacheRegion)));
		private readonly List<ICacheDependencyEnlister> _dependencyEnlisters = new List<ICacheDependencyEnlister>();
		private readonly string _name;

		/// <summary>The name of the cache key for the region</summary>
		private readonly string _rootCacheKey;

		/// <summary>The cache for the web application</summary>
		//private readonly System.Web.Caching.Cache _webCache;

		private readonly ObjectCache _cache = MemoryCache.Default;

		/// <summary>Indicates if the root cache item has been stored or not</summary>
		private bool _isRootItemCached;

		/// <summary>The priority of the cache item</summary>
		private CacheItemPriority _priority;

		/// <summary>relative expiration for the cache items</summary>
		private TimeSpan? _relativeExpiration;

		/// <summary>time of day expiration for the cache items</summary>
		private TimeSpan? _timeOfDayExpiration;

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class with
		/// the default region name and configuration properties
		/// </summary>
		public SysCacheRegion() : this(null, null, null) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class with the default configuration
		/// properties
		/// </summary>
		/// <param name="name">The name of the region</param>
		/// <param name="additionalProperties">additional NHibernate configuration properties</param>
		public SysCacheRegion(string name, IDictionary<string, string> additionalProperties)
			: this(name, null, additionalProperties) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="SysCacheRegion"/> class.
		/// </summary>
		/// <param name="name">The name of the region</param>
		/// <param name="settings">The configuration settings for the cache region</param>
		/// <param name="additionalProperties">additional NHibernate configuration properties</param>
		public SysCacheRegion(string name, CacheRegionElement settings, IDictionary<string, string> additionalProperties)
		{
			//validate the params
			if (String.IsNullOrEmpty(name))
			{
				log.Info("No region name specified for cache region. Using default name of 'nhibernate'");
				name = "nhibernate";
			}

			//_webCache = HttpRuntime.Cache;
			_name = name;

			//configure the cache region based on the configured settings and any relevant nhibernate settings
			Configure(settings, additionalProperties);

			//creaet the cache key that will be used for the root cache item which all other
			//cache items are dependent on
			_rootCacheKey = GenerateRootCacheKey();
		}

		#region ICache Members

		/// <summary>
		/// Clear the Cache
		/// </summary>
		/// <exception cref="T:NHibernate.Cache.CacheException"></exception>
		public void Clear()
		{
			//remove the root cache item, this will cause all of the individual items to be removed from the cache
			//_webCache.Remove(_rootCacheKey);
			_cache.Remove(_rootCacheKey);
			_isRootItemCached = false;

			log.Debug("All items cleared from the cache.");
		}

		public Task ClearAsync(CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Clear();
			}, cancellationToken);
		}


		/// <summary>
		/// Clean up.
		/// </summary>
		/// <exception cref="T:NHibernate.Cache.CacheException"></exception>
		public void Destroy()
		{
			Clear();
		}

		public object Get(object key)
		{
			if (key == null || _isRootItemCached == false)
			{
				return null;
			}

			string cacheKey = GetCacheKey(key);

			object cachedObject = _cache.Get(cacheKey);
			if (cachedObject == null)
			{
				return null;
			}

			var entry = (DictionaryEntry) cachedObject;
			if (key.Equals(entry.Key))
			{
				return entry.Value;
			}

			return null;
		}

		public Task<object> GetAsync(object key, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				return Get(key);
			}, cancellationToken);
		}


		/// <summary>
		/// If this is a clustered cache, lock the item
		/// </summary>
		/// <param name="key">The Key of the Item in the Cache to lock.</param>
		/// <exception cref="T:NHibernate.Cache.CacheException"></exception>
		public void Lock(object key)
		{
			//nothing to do here
		}

		public Task LockAsync(object key, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Lock(key);
			}, cancellationToken);
		}


		/// <summary>
		/// Generate a timestamp
		/// </summary>
		/// <returns>a timestamp</returns>
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		public void Put(object key, object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			if (_isRootItemCached == false)
			{
				CacheRootItem();
			}

			string cacheKey = GetCacheKey(key);

			if (_cache[cacheKey] != null)
			{
				//
				//_cache.Remove(cacheKey);
				//
			}

			var expiration = GetCacheItemExpiration();

			CacheItemPolicy policy = new CacheItemPolicy();
			policy.ChangeMonitors.Add(_cache.CreateCacheEntryChangeMonitor(new[] { _rootCacheKey }));
			policy.AbsoluteExpiration = new DateTimeOffset(expiration);
			policy.SlidingExpiration = ObjectCache.NoSlidingExpiration;
			policy.Priority = _priority;

			//_cache.Add(cacheKey, new DictionaryEntry(key, value), policy);
			_cache.Set(cacheKey, new DictionaryEntry(key, value), policy);
		}

		public Task PutAsync(object key, object value, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Put(key, value);
			}, cancellationToken);
		}


		/// <summary>
		/// Gets the name of the cache region
		/// </summary>
		public string RegionName
		{
			get { return _name; }
		}

		public void Remove(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			string cacheKey = GetCacheKey(key);
			_cache.Remove(cacheKey);
		}

		public Task RemoveAsync(object key, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Remove(key);
			}, cancellationToken);
		}


		/// <summary>
		/// Get a reasonable "lock timeout"
		/// </summary>
		/// <value></value>
		public int Timeout
		{
			get 
			{ 
				return Timestamper.OneMs * 60000; // 60 seconds
			}
		}

		public void Unlock(object key)
		{
			//nothing to do since we arent locking
		}

		public Task UnlockAsync(object key, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				Unlock(key);
			}, cancellationToken);
		}

		#endregion

		private void Configure(CacheRegionElement settings, IDictionary<string, string> additionalProperties)
		{
			log.Debug("Configuring cache region");

			//these are some default conenction values that can be later used by the data dependencies
			//if no custome settings are specified
			string connectionName = null;
			string connectionString = null;

			if (additionalProperties != null)
			{
				//pick up connection settings that might be used later for data dependencis if any are specified
				if (additionalProperties.ContainsKey(Environment.ConnectionStringName))
				{
					connectionName = additionalProperties[Environment.ConnectionStringName];
				}

				if (additionalProperties.ContainsKey(Environment.ConnectionString))
				{
					connectionString = additionalProperties[Environment.ConnectionString];
				}
			}

			if (settings != null)
			{
				//_priority = settings.Priority;
				_timeOfDayExpiration = settings.TimeOfDayExpiration;
				_relativeExpiration = settings.RelativeExpiration;

				if (log.IsDebugEnabled)
				{
					log.DebugFormat("using priority: {0}", settings.Priority.ToString("g"));

					if (_relativeExpiration.HasValue)
					{
						log.DebugFormat("using relative expiration :{0}", _relativeExpiration);
					}

					if (_timeOfDayExpiration.HasValue)
					{
						log.DebugFormat("using time of day expiration : {0}", _timeOfDayExpiration);
					}
				}

				CreateDependencyEnlisters(settings.Dependencies, connectionName, connectionString);
			}
			else
			{
				_priority = CacheItemPriority.Default;
			}

			if (_relativeExpiration.HasValue == false && _timeOfDayExpiration.HasValue == false)
			{
				_relativeExpiration = defaultRelativeExpiration;
			}
		}

		private void CreateDependencyEnlisters(CacheDependenciesElement dependencyConfig, string defaultConnectionName,
											   string defaultConnectionString)
		{
			//dont do anything if there is no config
			if (dependencyConfig == null)
			{
				log.Debug("no data dependencies specified");
				return;
			}

			if (dependencyConfig.FileDependencies.Count > 0)
			{
				var paths = new List<string>();
				foreach (FileCacheDependencyElement fileConfig in dependencyConfig.FileDependencies)
				{
					var path = fileConfig.Path;
					if (!Path.IsPathRooted(path))
					{
						path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
					}
					if (File.Exists(path))
					{
						paths.Add(path);
					}
				}
				_dependencyEnlisters.Add(new FileCacheDependencyEnlister(paths));
			}

			if (dependencyConfig.CommandDependencies.Count > 0)
			{
				foreach (CommandCacheDependencyElement commandConfig in dependencyConfig.CommandDependencies)
				{
					IConnectionStringProvider connectionStringProvider;
					string connectionName = null;

					if (commandConfig.ConnectionStringProviderType != null)
					{
						connectionStringProvider = Activator.CreateInstance(commandConfig.ConnectionStringProviderType) as IConnectionStringProvider;
						connectionName = commandConfig.ConnectionName;
					}
					else
					{
						if (String.IsNullOrEmpty(defaultConnectionName) && String.IsNullOrEmpty(commandConfig.ConnectionName))
						{
							log.DebugFormat("no connection string provider specified using nhibernate configured connection string");

							connectionStringProvider = new StaticConnectionStringProvider(defaultConnectionString);
						}
						else
						{
							connectionStringProvider = new ConfigConnectionStringProvider();

							if (String.IsNullOrEmpty(commandConfig.ConnectionName) == false)
							{
								connectionName = commandConfig.ConnectionName;
							}
							else
							{
								connectionName = defaultConnectionName;
							}
						}
					}

					var commandEnlister = new SqlCommandCacheDependencyEnlister(commandConfig.Command, commandConfig.IsStoredProcedure,
																				commandConfig.CommandTimeout, connectionName, 
																				connectionStringProvider);
					_dependencyEnlisters.Add(commandEnlister);
				}
			}
		}

		private string GetCacheKey(object identifier)
		{
			return String.Concat(CacheKeyPrefix, _name, ":", identifier.ToString(), "@", identifier.GetHashCode());
		}

		private string GenerateRootCacheKey()
		{
			return GetCacheKey(Guid.NewGuid());
		}

		private void CacheRootItem()
		{
			var policy = new CacheItemPolicy();
			policy.AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration;
			policy.SlidingExpiration = ObjectCache.NoSlidingExpiration;
			policy.Priority = CacheItemPriority.Default;
			policy.RemovedCallback = RootCacheItemRemovedCallback; 

			foreach (ICacheDependencyEnlister enlister in _dependencyEnlisters)
			{
				policy.ChangeMonitors.Add(enlister.Enlist());
			}

			_cache.Add(_rootCacheKey, _rootCacheKey, policy);
			
			_isRootItemCached = true;
		}

		private void RootCacheItemRemovedCallback(CacheEntryRemovedArguments arguments)
		{
			_isRootItemCached = false;
		}

		private DateTime GetCacheItemExpiration()
		{
			DateTime expiration = ObjectCache.InfiniteAbsoluteExpiration.DateTime;

			//use the relative expiration if one is specified, otherwise use the 
			//time of day expiration if that is specified
			if (_relativeExpiration.HasValue)
			{
				expiration = DateTime.Now.Add(_relativeExpiration.Value);
			}
			else if (_timeOfDayExpiration.HasValue)
			{
				//calculate the expiration by starting at 12 am of today
				DateTime timeExpiration = DateTime.Today;

				//add a day to the expiration time if the time of day has already passed,
				//this will cause the item to expire tommorrow
				if (DateTime.Now.TimeOfDay > _timeOfDayExpiration.Value)
				{
					timeExpiration = timeExpiration.AddDays(1);
				}

				//adding the specified time of day expiration to the adjusted base date
				//will provide us with the time of day expiration specified
				timeExpiration = timeExpiration.Add(_timeOfDayExpiration.Value);

				expiration = timeExpiration;
			}

			return expiration;
		}
	}
}