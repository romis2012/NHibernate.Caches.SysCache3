using System.Configuration;

namespace NHibernate.Caches.SysCache3
{
    [ConfigurationCollection(typeof (FileCacheDependencyElement), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
    public class FileCacheDependencyCollection : ConfigurationElementCollection
    {
        public FileCacheDependencyElement this[int index]
        {
            get { return BaseGet(index) as FileCacheDependencyElement; }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                base.BaseAdd(index, value);
            }
        }

        public new FileCacheDependencyElement this[string name]
        {
            get { return BaseGet(name) as FileCacheDependencyElement; }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new FileCacheDependencyElement();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((FileCacheDependencyElement)element).Name;
        }
    }
}