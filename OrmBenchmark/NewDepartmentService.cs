namespace Benchmarks;

public sealed class NewDepartmentService(IEnumerable<Department> departments)
{
    private IReadOnlyList<DepartmentNode> Roots { get; } = DepartmentForestBuilder.Build(departments);

  public DepartmentNode? FindByNameAtLevel(string name, int level) =>
    Roots.Select(root => TraverseLevel(root, level)
            .FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        .OfType<DepartmentNode>()
        .FirstOrDefault();

  public DepartmentNode? FindByNameAtLevelRecursive(string name, int level) =>
      Roots.Select(root => TraverseLevelRecursive(root, level)
              .FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
          .OfType<DepartmentNode>()
          .FirstOrDefault();

  public DepartmentNode? FindByNameAtLevelFiltered(string name, int level) =>
        Roots.Select(root =>
                TraverseLevelFiltered(root, level, node => node.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .OfType<DepartmentNode>()
            .FirstOrDefault();

    public DepartmentNode? FindRootByDepartmentId(int id) =>
        Roots
            .Select(root => FindByIdInSubtree(root, id))
            .OfType<DepartmentNode>()
            .Select(node => node.Root)
            .FirstOrDefault();

    public (IReadOnlyList<DepartmentNode> Ancestors, DepartmentNode Node, IReadOnlyList<DepartmentNode> Descendants)
        GetHierarchy(int id)
    {
        foreach (var root in Roots)
        {
            var node = FindByIdInSubtree(root, id);
            if (node is null)
            {
                continue;
            }

            var ancestors = new List<DepartmentNode>();
            var current = node.Parent;
            while (current is not null)
            {
                ancestors.Add(current);
                current = current.Parent;
            }

            var descendants = CollectDescendants(node).ToList();
            return (ancestors, node, descendants);
        }

        throw new KeyNotFoundException($"Department {id} not found");
    }

  private static IEnumerable<DepartmentNode> TraverseLevel(DepartmentNode root, int targetLevel)
  {
    var queue = new Queue<(DepartmentNode Node, int Depth)>();
    queue.Enqueue((root, 0));

    while (queue.Count > 0)
    {
      var (node, depth) = queue.Dequeue();
      if (depth == targetLevel)
      {
        yield return node;
      }

      if (depth > targetLevel)
      {
        continue;
      }

      foreach (var child in node.Children)
      {
        queue.Enqueue((child, depth + 1));
      }
    }
  }

  private static IEnumerable<DepartmentNode> TraverseLevelRecursive(DepartmentNode root, int targetLevel)
  {
    if (targetLevel == 0)
    {
      yield return root;
      yield break;
    }

    foreach (var child in root.Children)
    {
      if (targetLevel == 1)
      {
        yield return child;
      }
      else
      {
        foreach (var node in TraverseLevelRecursive(child, targetLevel - 1))
        {
          yield return node;
        }
      }
    }
  }

  private static IEnumerable<DepartmentNode> TraverseLevelFiltered(
        DepartmentNode root,
        int targetLevel,
        Func<DepartmentNode, bool> predicate)
    {
        if (targetLevel == 0)
        {
            if (predicate(root))
            {
                yield return root;
            }

            yield break;
        }

        foreach (var child in root.Children)
        {
            if (targetLevel == 1)
            {
                if (predicate(child))
                {
                    yield return child;
                }
            }
            else
            {
                foreach (var node in TraverseLevelFiltered(child, targetLevel - 1, predicate))
                {
                    yield return node;
                }
            }
        }
    }

    private static DepartmentNode? FindByIdInSubtree(DepartmentNode root, int id)
    {
        if (root.Id == id)
        {
            return root;
        }

        if (!root.DescendantIds.Contains(id))
        {
            return null;
        }

        var stack = new Stack<DepartmentNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node.Id == id)
            {
                return node;
            }

            foreach (var child in node.Children)
            {
                if (child.Id == id)
                {
                    return child;
                }

                if (child.DescendantIds.Contains(id))
                {
                    stack.Push(child);
                }
            }
        }

        return null;
    }

    private static IEnumerable<DepartmentNode> CollectDescendants(DepartmentNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var d in CollectDescendants(child))
            {
                yield return d;
            }
        }
    }
}
