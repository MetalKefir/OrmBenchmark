using ViennaNET.Orm.Seedwork;

namespace OrmBenchmark
{
    public class Blog : IEntityKey<int>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Url { get; set; }
        public virtual int Rating { get; set; }
    }
}
