using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (child != null)
        {
            Splay(child);
            return;
        }

        if (parent != null)
        {
            Splay(parent);
        }
    }

    public override bool ContainsKey(TKey key)
    {
        var (node, last) = FindNodeWithLast(key);
        if (node != null)
        {
            Splay(node);
            return true;
        }

        if (last != null)
        {
            Splay(last);
        }

        return false;
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var (node, last) = FindNodeWithLast(key);
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }

        if (last != null)
        {
            Splay(last);
        }

        value = default;
        return false;
    }

    private (BstNode<TKey, TValue>? Node, BstNode<TKey, TValue>? Last) FindNodeWithLast(TKey key)
    {
        BstNode<TKey, TValue>? current = Root;
        BstNode<TKey, TValue>? last = null;

        while (current != null)
        {
            last = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                return (current, last);
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        return (null, last);
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            BstNode<TKey, TValue> parent = node.Parent;
            BstNode<TKey, TValue>? grand = parent.Parent;

            if (grand == null)
            {
                if (node.IsLeftChild)
                {
                    RotateRight(parent);
                }
                else
                {
                    RotateLeft(parent);
                }

                continue;
            }

            if (node.IsLeftChild && parent.IsLeftChild)
            {
                RotateRight(grand);
                RotateRight(parent);
            }
            else if (node.IsRightChild && parent.IsRightChild)
            {
                RotateLeft(grand);
                RotateLeft(parent);
            }
            else if (node.IsRightChild && parent.IsLeftChild)
            {
                RotateLeft(parent);
                RotateRight(grand);
            }
            else
            {
                RotateRight(parent);
                RotateLeft(grand);
            }
        }
    }
}
