using System.Collections.Generic;

namespace Benchmarks;

public sealed class DepartmentNode
{
    private readonly List<DepartmentNode> _children = [];

    // Все потомки по Id для быстрого поиска
    private readonly HashSet<int> _descendantIds = [];
    public int Id { get; }

    public string Name { get; }

    public DepartmentNode? Parent { get; private set; }

    public DepartmentNode Root { get; set; }
    public IReadOnlyList<DepartmentNode> Children => _children;
    public IReadOnlySet<int> DescendantIds => _descendantIds;

    public DepartmentNode(int id, string name)
    {
        Id = id;
        Name = name;
        Root = this;
    }

    internal void AttachChild(DepartmentNode child)
    {
        child.Parent = this;
        child.Root = Root;
        _children.Add(child);

        // обновляем множества потомков вверх по цепочке
        var current = this;
        while (current is not null)
        {
            current._descendantIds.Add(child.Id);
            foreach (var id in child._descendantIds)
            {
                current._descendantIds.Add(id);
            }

            current = current.Parent;
        }
    }
}
