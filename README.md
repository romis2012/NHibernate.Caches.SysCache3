NHibernate.Caches.SysCache3
===========================
NHibernate 2nd level cache provider that uses System.Runtime.Caching.MemoryCache.

* Supports SqlChangeMonitor
* Supports HostFileChangeMonitor
* Does not depend on System.Web.dll

How to use it (see also NHibernate.Caches.SysCache2 from [NHibernate Contrib](http://sourceforge.net/projects/nhcontrib/) project):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  
  <configSections>
    <section name="syscache3" type="NHibernate.Caches.SysCache3.SysCacheSection, NHibernate.Caches.SysCache3" />
  </configSections>
  
  <syscache3>
    <cacheRegion name="DefaultCache" relativeExpiration="3600">
      <dependencies>
        <commands>
          <add name="test1" command="select Id from dbo.Test" />
        </commands>
        <files>
          <add name="test1" path="c:/tmp/test.txt" />
          <add name="test2" path="Test.txt"/>
        </files>
      </dependencies>
    </cacheRegion>
  </syscache3>
  
</configuration>
```
