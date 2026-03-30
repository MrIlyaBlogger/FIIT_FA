using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        RebalanceFrom(newNode);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        RebalanceFrom(child ?? parent);
    }

    private static int GetHeight(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    private static int GetBalance(AvlNode<TKey, TValue> node) => GetHeight(node.Left) - GetHeight(node.Right);

    private static void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = Math.Max(GetHeight(node.Left), GetHeight(node.Right)) + 1;
    }

    private void RotateLeftAndUpdate(AvlNode<TKey, TValue> node)
    {
        RotateLeft(node);
        UpdateHeight(node);
        if (node.Parent != null)
        {
            UpdateHeight(node.Parent);
        }
    }

    private void RotateRightAndUpdate(AvlNode<TKey, TValue> node)
    {
        RotateRight(node);
        UpdateHeight(node);
        if (node.Parent != null)
        {
            UpdateHeight(node.Parent);
        }
    }

    private void RebalanceFrom(AvlNode<TKey, TValue>? node)
    {
        while (node != null)
        {
            UpdateHeight(node);
            int balance = GetBalance(node);

            if (balance > 1)
            {
                if (node.Left != null && GetBalance(node.Left) < 0)
                {
                    RotateLeftAndUpdate(node.Left);
                }

                RotateRightAndUpdate(node);
            }
            else if (balance < -1)
            {
                if (node.Right != null && GetBalance(node.Right) > 0)
                {
                    RotateRightAndUpdate(node.Right);
                }

                RotateLeftAndUpdate(node);
            }

            node = node.Parent;
        }
    }
}
