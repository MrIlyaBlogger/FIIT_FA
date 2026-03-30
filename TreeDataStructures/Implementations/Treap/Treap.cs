using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
        {
            return (null, null);
        }

        int cmp = Comparer.Compare(root.Key, key);
        if (cmp <= 0)
        {
            var (left, right) = Split(root.Right, key);
            root.Right = left;
            if (left != null)
            {
                left.Parent = root;
            }

            root.Parent = null;
            if (right != null)
            {
                right.Parent = null;
            }

            return (root, right);
        }
        else
        {
            var (left, right) = Split(root.Left, key);
            root.Left = right;
            if (right != null)
            {
                right.Parent = root;
            }

            root.Parent = null;
            if (left != null)
            {
                left.Parent = null;
            }

            return (left, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null)
        {
            if (right != null)
            {
                right.Parent = null;
            }

            return right;
        }

        if (right == null)
        {
            left.Parent = null;
            return left;
        }

        if (left.Priority >= right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null)
            {
                left.Right.Parent = left;
            }

            left.Parent = null;
            return left;
        }

        right.Left = Merge(left, right.Left);
        if (right.Left != null)
        {
            right.Left.Parent = right;
        }

        right.Parent = null;
        return right;
    }
    

    public override void Add(TKey key, TValue value)
    {
        TreapNode<TKey, TValue>? existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }

        TreapNode<TKey, TValue> newNode = CreateNode(key, value);
        var (left, right) = Split(Root, key);
        Root = Merge(Merge(left, newNode), right);
        if (Root != null)
        {
            Root.Parent = null;
        }

        Count++;
        OnNodeAdded(newNode);
    }

    public override bool Remove(TKey key)
    {
        TreapNode<TKey, TValue>? node = FindNode(key);
        if (node == null)
        {
            return false;
        }

        TreapNode<TKey, TValue>? replacement = Merge(node.Left, node.Right);
        TreapNode<TKey, TValue>? parent = node.Parent;

        if (parent == null)
        {
            Root = replacement;
        }
        else if (node.IsLeftChild)
        {
            parent.Left = replacement;
        }
        else
        {
            parent.Right = replacement;
        }

        if (replacement != null)
        {
            replacement.Parent = parent;
        }

        Count--;
        OnNodeRemoved(parent, replacement);
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
        // Treap balancing is done directly in Add/Merge.
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
        // Treap balancing is done directly in Remove/Merge.
    }
    
}
