using System.Collections.Generic;

namespace Benchmarks;

public static class DepartmentForestBuilder
{
    public static IReadOnlyList<DepartmentNode> Build(IEnumerable<Department> departments)
    {
        var byId = new Dictionary<int, DepartmentNode>();

        // создаём узлы
        foreach (var dept in departments)
        {
            if (!byId.ContainsKey(dept.Id))
            {
                byId[dept.Id] = new DepartmentNode(dept.Id, dept.Name);
            }
        }

        var roots = new List<DepartmentNode>();

        // связываем
        foreach (var dept in departments)
        {
            var node = byId[dept.Id];
            if (dept.ParentId is null)
            {
                node.Root = node;
                roots.Add(node);
            }
            else
            {
                var parent = byId[dept.ParentId.Value];
                parent.AttachChild(node);
            }
        }

        return roots;
    }
}
