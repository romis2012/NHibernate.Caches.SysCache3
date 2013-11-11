using System.Collections.Generic;
using System.Runtime.Caching;

namespace NHibernate.Caches.SysCache3
{
	public class FileCacheDependencyEnlister : ICacheDependencyEnlister
	{
		private readonly IList<string> _paths;

		public FileCacheDependencyEnlister(IList<string> paths)
		{
			_paths = paths;
		}

		public ChangeMonitor Enlist()
		{
			return new HostFileChangeMonitor(_paths);
		}
	}
}