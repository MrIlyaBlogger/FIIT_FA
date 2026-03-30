using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    public override void Add(TKey key, TValue value)
    {
        if (Root == null)
        {
            Root = CreateNode(key, value);
            Root.Color = RbColor.Black;
            Count = 1;
            return;
        }

        RbNode<TKey, TValue>? parent = null;
        RbNode<TKey, TValue>? current = Root;

        while (current != null)
        {
            parent = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                current.Value = value;
                return;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        RbNode<TKey, TValue> node = CreateNode(key, value);
        node.Parent = parent;
        if (Comparer.Compare(key, parent!.Key) < 0)
        {
            parent.Left = node;
        }
        else
        {
            parent.Right = node;
        }

        Count++;
        OnNodeAdded(node);
    }

    public override bool Remove(TKey key)
    {
        RbNode<TKey, TValue>? z = FindNode(key);
        if (z == null)
        {
            return false;
        }

        RbNode<TKey, TValue> y = z;
        RbColor yOriginalColor = y.Color;
        RbNode<TKey, TValue>? x;
        RbNode<TKey, TValue>? xParent;

        if (z.Left == null)
        {
            x = z.Right;
            xParent = z.Parent;
            Transplant(z, z.Right);
        }
        else if (z.Right == null)
        {
            x = z.Left;
            xParent = z.Parent;
            Transplant(z, z.Left);
        }
        else
        {
            y = Minimum(z.Right);
            yOriginalColor = y.Color;
            x = y.Right;

            if (y.Parent == z)
            {
                xParent = y;
                if (x != null)
                {
                    x.Parent = y;
                }
            }
            else
            {
                xParent = y.Parent;
                Transplant(y, y.Right);
                y.Right = z.Right;
                y.Right!.Parent = y;
            }

            Transplant(z, y);
            y.Left = z.Left;
            y.Left!.Parent = y;
            y.Color = z.Color;
        }

        Count--;

        if (yOriginalColor == RbColor.Black)
        {
            FixDelete(x, xParent);
        }

        if (Root != null)
        {
            Root.Color = RbColor.Black;
        }

        return true;
    }

    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixInsert(newNode);
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        // Delete balancing is implemented in Remove because fix-up needs removed-node color context.
    }

    private static RbColor ColorOf(RbNode<TKey, TValue>? node) => node?.Color ?? RbColor.Black;

    private static void SetColor(RbNode<TKey, TValue>? node, RbColor color)
    {
        if (node != null)
        {
            node.Color = color;
        }
    }

    private static RbNode<TKey, TValue> Minimum(RbNode<TKey, TValue> node)
    {
        RbNode<TKey, TValue> current = node;
        while (current.Left != null)
        {
            current = current.Left;
        }

        return current;
    }

    private void FixInsert(RbNode<TKey, TValue> z)
    {
        while (z.Parent != null && z.Parent.Color == RbColor.Red)
        {
            RbNode<TKey, TValue> parent = z.Parent;
            RbNode<TKey, TValue>? grand = parent.Parent;
            if (grand == null)
            {
                break;
            }

            if (parent == grand.Left)
            {
                RbNode<TKey, TValue>? uncle = grand.Right;
                if (ColorOf(uncle) == RbColor.Red)
                {
                    parent.Color = RbColor.Black;
                    if (uncle != null)
                    {
                        uncle.Color = RbColor.Black;
                    }

                    grand.Color = RbColor.Red;
                    z = grand;
                }
                else
                {
                    if (z == parent.Right)
                    {
                        z = parent;
                        RotateLeft(z);
                        parent = z.Parent!;
                        grand = parent.Parent!;
                    }

                    parent.Color = RbColor.Black;
                    grand.Color = RbColor.Red;
                    RotateRight(grand);
                }
            }
            else
            {
                RbNode<TKey, TValue>? uncle = grand.Left;
                if (ColorOf(uncle) == RbColor.Red)
                {
                    parent.Color = RbColor.Black;
                    if (uncle != null)
                    {
                        uncle.Color = RbColor.Black;
                    }

                    grand.Color = RbColor.Red;
                    z = grand;
                }
                else
                {
                    if (z == parent.Left)
                    {
                        z = parent;
                        RotateRight(z);
                        parent = z.Parent!;
                        grand = parent.Parent!;
                    }

                    parent.Color = RbColor.Black;
                    grand.Color = RbColor.Red;
                    RotateLeft(grand);
                }
            }
        }

        if (Root != null)
        {
            Root.Color = RbColor.Black;
        }
    }

    private void FixDelete(RbNode<TKey, TValue>? x, RbNode<TKey, TValue>? parent)
    {
        while (x != Root && ColorOf(x) == RbColor.Black)
        {
            if (parent == null)
            {
                break;
            }

            if (x == parent.Left)
            {
                RbNode<TKey, TValue>? w = parent.Right;

                if (ColorOf(w) == RbColor.Red)
                {
                    SetColor(w, RbColor.Black);
                    SetColor(parent, RbColor.Red);
                    RotateLeft(parent);
                    w = parent.Right;
                }

                if (ColorOf(w?.Left) == RbColor.Black && ColorOf(w?.Right) == RbColor.Black)
                {
                    SetColor(w, RbColor.Red);
                    x = parent;
                    parent = x.Parent;
                }
                else
                {
                    if (ColorOf(w?.Right) == RbColor.Black)
                    {
                        SetColor(w?.Left, RbColor.Black);
                        SetColor(w, RbColor.Red);
                        if (w != null)
                        {
                            RotateRight(w);
                        }

                        w = parent.Right;
                    }

                    SetColor(w, ColorOf(parent));
                    SetColor(parent, RbColor.Black);
                    SetColor(w?.Right, RbColor.Black);
                    RotateLeft(parent);
                    x = Root;
                    parent = null;
                }
            }
            else
            {
                RbNode<TKey, TValue>? w = parent.Left;

                if (ColorOf(w) == RbColor.Red)
                {
                    SetColor(w, RbColor.Black);
                    SetColor(parent, RbColor.Red);
                    RotateRight(parent);
                    w = parent.Left;
                }

                if (ColorOf(w?.Right) == RbColor.Black && ColorOf(w?.Left) == RbColor.Black)
                {
                    SetColor(w, RbColor.Red);
                    x = parent;
                    parent = x.Parent;
                }
                else
                {
                    if (ColorOf(w?.Left) == RbColor.Black)
                    {
                        SetColor(w?.Right, RbColor.Black);
                        SetColor(w, RbColor.Red);
                        if (w != null)
                        {
                            RotateLeft(w);
                        }

                        w = parent.Left;
                    }

                    SetColor(w, ColorOf(parent));
                    SetColor(parent, RbColor.Black);
                    SetColor(w?.Left, RbColor.Black);
                    RotateRight(parent);
                    x = Root;
                    parent = null;
                }
            }
        }

        SetColor(x, RbColor.Black);
    }
}
