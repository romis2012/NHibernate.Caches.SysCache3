using System;
using System.Configuration;

namespace NHibernate.Caches.SysCache3
{
    public class FileCacheDependencyElement : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection properties;

        static FileCacheDependencyElement()
        {
            properties = new ConfigurationPropertyCollection();
            properties.Add(new ConfigurationProperty("name", typeof(string), String.Empty, ConfigurationPropertyOptions.IsKey));
            properties.Add(new ConfigurationProperty("path", typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired));
        }

        public string Name
        {
            get { return (string)base["name"]; }
        }

        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)base["path"]; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return properties; }
        }
    }
}