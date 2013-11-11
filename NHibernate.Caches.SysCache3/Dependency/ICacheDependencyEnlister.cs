using System.Runtime.Caching;

namespace NHibernate.Caches.SysCache3
{
	/// <summary>
	/// Enlists a <see cref="CacheDependency"/> for change notifications
	/// </summary>
	public interface ICacheDependencyEnlister
	{
		/// <summary>
		/// Enlists a cache dependency to recieve change notifciations with an underlying resource
		/// </summary>
		/// <returns>The cache dependency linked to the notification subscription</returns>
		//CacheDependency Enlist();
		//todo: return ChangeMonitor here
		ChangeMonitor Enlist();

	}
}