using FluentNHibernate.Mapping;

namespace OrmBenchmark
{
    public class BlogMapping: ClassMap<Blog>
    {
        public BlogMapping()
        {
            Table("blogs");
            Id(d => d.Id).Column("Id");
            Map(d => d.Name).Column("name");
            Map(d => d.Url).Column("url");
            Map(d => d.Rating).Column("rating");
        }
    }
}
