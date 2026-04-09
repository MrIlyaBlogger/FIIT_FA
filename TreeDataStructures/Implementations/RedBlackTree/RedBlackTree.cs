using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey> {
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode) {
        FixInsert(newNode);
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child) {
        FixDelete(child, child?.Parent ?? parent);

        if (Root != null) {
            Root.Color = RbColor.Black;
        }
    }

    private static RbColor ColorOf(RbNode<TKey, TValue>? node) => node?.Color ?? RbColor.Black;

    private static void SetColor(RbNode<TKey, TValue>? node, RbColor color) {
        if (node != null) {
            node.Color = color;
        }
    }

    private void FixInsert(RbNode<TKey, TValue> z) {
        while (z.Parent != null && z.Parent.Color == RbColor.Red) {
            RbNode<TKey, TValue> parent = z.Parent;
            RbNode<TKey, TValue>? grand = parent.Parent;
            if (grand == null) {
                break;
            }

            if (parent == grand.Left) {
                RbNode<TKey, TValue>? uncle = grand.Right;
                if (ColorOf(uncle) == RbColor.Red) {
                    parent.Color = RbColor.Black;
                    if (uncle != null) {
                        uncle.Color = RbColor.Black;
                    }

                    grand.Color = RbColor.Red;
                    z = grand;
                }
                else {
                    if (z == parent.Right) {
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
            else {
                RbNode<TKey, TValue>? uncle = grand.Left;
                if (ColorOf(uncle) == RbColor.Red) {
                    parent.Color = RbColor.Black;
                    if (uncle != null) {
                        uncle.Color = RbColor.Black;
                    }

                    grand.Color = RbColor.Red;
                    z = grand;
                }
                else {
                    if (z == parent.Left) {
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

        if (Root != null) {
            Root.Color = RbColor.Black;
        }
    }

    private void FixDelete(RbNode<TKey, TValue>? x, RbNode<TKey, TValue>? parent) {
        while (x != Root && ColorOf(x) == RbColor.Black) {
            if (parent == null) {
                break;
            }

            if (x == parent.Left) {
                RbNode<TKey, TValue>? w = parent.Right;

                if (ColorOf(w) == RbColor.Red) {
                    SetColor(w, RbColor.Black);
                    SetColor(parent, RbColor.Red);
                    RotateLeft(parent);
                    w = parent.Right;
                }

                if (ColorOf(w?.Left) == RbColor.Black && ColorOf(w?.Right) == RbColor.Black) {
                    SetColor(w, RbColor.Red);
                    x = parent;
                    parent = x.Parent;
                }
                else {
                    if (ColorOf(w?.Right) == RbColor.Black) {
                        SetColor(w?.Left, RbColor.Black);
                        SetColor(w, RbColor.Red);
                        if (w != null) {
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
            else {
                RbNode<TKey, TValue>? w = parent.Left;

                if (ColorOf(w) == RbColor.Red) {
                    SetColor(w, RbColor.Black);
                    SetColor(parent, RbColor.Red);
                    RotateRight(parent);
                    w = parent.Left;
                }

                if (ColorOf(w?.Right) == RbColor.Black && ColorOf(w?.Left) == RbColor.Black) {
                    SetColor(w, RbColor.Red);
                    x = parent;
                    parent = x.Parent;
                }
                else {
                    if (ColorOf(w?.Left) == RbColor.Black) {
                        SetColor(w?.Right, RbColor.Black);
                        SetColor(w, RbColor.Red);
                        if (w != null) {
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

