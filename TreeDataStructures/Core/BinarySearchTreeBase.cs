using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode> {
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default;

    public int Count { get; protected set; }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys
    {
        get
        {
            List<TKey> keys = new(Count);
            foreach (var entry in InOrder()) {
                keys.Add(entry.Key);
            }

            return keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            List<TValue> values = new(Count);
            foreach (var entry in InOrder()) {
                values.Add(entry.Value);
            }

            return values;
        }
    }

    public virtual void Add(TKey key, TValue value) {
        if (Root == null) {
            Root = CreateNode(key, value);
            Count = 1;
            OnNodeAdded(Root);
            return;
        }

        TNode? parent = null;
        TNode? current = Root;

        while (current != null) {
            parent = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) {
                current.Value = value;
                return;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;
        if (Comparer.Compare(key, parent!.Key) < 0) {
            parent.Left = newNode;
        }
        else {
            parent.Right = newNode;
        }

        Count++;
        OnNodeAdded(newNode);
    }

    public virtual bool Remove(TKey key) {
        TNode? node = FindNode(key);
        if (node == null) {
            return false;
        }

        RemoveNode(node);
        Count--;
        return true;
    }

    protected virtual void RemoveNode(TNode node) {
        if (node.Left == null) {
            TNode? parent = node.Parent;
            TNode? child = node.Right;
            Transplant(node, node.Right);
            OnNodeRemoved(parent, child);
            return;
        }

        if (node.Right == null) {
            TNode? parent = node.Parent;
            TNode? child = node.Left;
            Transplant(node, node.Left);
            OnNodeRemoved(parent, child);
            return;
        }

        TNode successor = Minimum(node.Right);

        if (successor.Parent != node) {
            Transplant(successor, successor.Right);
            successor.Right = node.Right;
            successor.Right!.Parent = successor;
        }

        Transplant(node, successor);
        successor.Left = node.Left;
        successor.Left!.Parent = successor;

        OnNodeRemoved(successor.Parent, successor);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
        TNode? node = FindNode(key);
        if (node != null) {
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    public TValue this[TKey key] {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    protected virtual void OnNodeAdded(TNode newNode) { }

    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }

    protected abstract TNode CreateNode(TKey key, TValue value);

    protected TNode? FindNode(TKey key) {
        TNode? current = Root;
        while (current != null) {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) {
                return current;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        return null;
    }

    private static TNode Minimum(TNode node) {
        TNode current = node;
        while (current.Left != null) {
            current = current.Left;
        }

        return current;
    }

    protected void RotateLeft(TNode x) {
        TNode? y = x.Right;
        if (y == null) {
            return;
        }

        x.Right = y.Left;
        if (y.Left != null) {
            y.Left.Parent = x;
        }

        y.Parent = x.Parent;

        if (x.Parent == null) {
            Root = y;
        }
        else if (x.IsLeftChild) {
            x.Parent.Left = y;
        }
        else {
            x.Parent.Right = y;
        }

        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y) {
        TNode? x = y.Left;
        if (x == null) {
            return;
        }

        y.Left = x.Right;
        if (x.Right != null) {
            x.Right.Parent = y;
        }

        x.Parent = y.Parent;

        if (y.Parent == null) {
            Root = x;
        }
        else if (y.IsLeftChild) {
            y.Parent.Left = x;
        }
        else {
            y.Parent.Right = x;
        }

        x.Right = y;
        y.Parent = x;
    }

    protected void RotateBigLeft(TNode x) {
        TNode? pivot = x.Right;
        if (pivot == null) {
            return;
        }

        RotateLeft(x);
        RotateLeft(pivot);
    }

    protected void RotateBigRight(TNode y) {
        TNode? pivot = y.Left;
        if (pivot == null) {
            return;
        }

        RotateRight(y);
        RotateRight(pivot);
    }

    protected void RotateDoubleLeft(TNode x) {
        if (x.Right == null) {
            return;
        }

        RotateRight(x.Right);
        RotateLeft(x);
    }

    protected void RotateDoubleRight(TNode y) {
        if (y.Left == null) {
            return;
        }

        RotateLeft(y.Left);
        RotateRight(y);
    }

    protected void Transplant(TNode u, TNode? v) {
        if (u.Parent == null) {
            Root = v;
        }
        else if (u.IsLeftChild) {
            u.Parent.Left = v;
        }
        else {
            u.Parent.Right = v;
        }

        if (v != null) {
            v.Parent = u.Parent;
        }
    }

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private List<TreeEntry<TKey, TValue>>? _entries;
        private int _index;

        public TreeIterator(TNode? root, TraversalStrategy strategy) {
            _root = root;
            _strategy = strategy;
            _entries = null;
            _index = -1;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => new TreeIterator(_root, _strategy);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_entries == null || _index < 0 || _index >= _entries.Count) {
                    throw new InvalidOperationException();
                }

                return _entries[_index];
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            if (_entries == null) {
                _entries = BuildEntries(_root, _strategy);
                _index = -1;
            }

            if (_index + 1 >= _entries.Count) {
                return false;
            }

            _index++;
            return true;
        }

        public void Reset() {
            _entries = null;
            _index = -1;
        }

        public void Dispose() { }

        private static List<TreeEntry<TKey, TValue>> BuildEntries(TNode? root, TraversalStrategy strategy) {
            if (root == null) {
                return [];
            }

            TraversalStrategy baseStrategy = strategy switch
            {
                TraversalStrategy.InOrderReverse => TraversalStrategy.InOrder,
                TraversalStrategy.PreOrderReverse => TraversalStrategy.PreOrder,
                TraversalStrategy.PostOrderReverse => TraversalStrategy.PostOrder,
                _ => strategy
            };

            bool reverse = strategy is TraversalStrategy.InOrderReverse
                or TraversalStrategy.PreOrderReverse
                or TraversalStrategy.PostOrderReverse;

            List<TNode> nodes = BuildDirectNodeOrder(root, baseStrategy);
            if (reverse) {
                nodes.Reverse();
            }

            Dictionary<TNode, int> heights = BuildHeightMap(root);
            List<TreeEntry<TKey, TValue>> entries = new(nodes.Count);
            foreach (TNode node in nodes) {
                entries.Add(new TreeEntry<TKey, TValue>(node.Key, node.Value, heights[node]));
            }

            return entries;
        }

        private static List<TNode> BuildDirectNodeOrder(TNode root, TraversalStrategy strategy) {
            return strategy switch
            {
                TraversalStrategy.InOrder => BuildInOrder(root),
                TraversalStrategy.PreOrder => BuildPreOrder(root),
                TraversalStrategy.PostOrder => BuildPostOrder(root),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unexpected strategy")
            };
        }

        private static List<TNode> BuildInOrder(TNode root) {
            List<TNode> nodes = [];
            Stack<TNode> stack = [];
            TNode? current = root;

            while (current != null || stack.Count > 0) {
                while (current != null) {
                    stack.Push(current);
                    current = current.Left;
                }

                TNode node = stack.Pop();
                nodes.Add(node);
                current = node.Right;
            }

            return nodes;
        }

        private static List<TNode> BuildPreOrder(TNode root) {
            List<TNode> nodes = [];
            Stack<TNode> stack = [];
            stack.Push(root);

            while (stack.Count > 0) {
                TNode node = stack.Pop();
                nodes.Add(node);

                if (node.Right != null) {
                    stack.Push(node.Right);
                }

                if (node.Left != null) {
                    stack.Push(node.Left);
                }
            }

            return nodes;
        }

        private static List<TNode> BuildPostOrder(TNode root) {
            List<TNode> nodes = [];
            Stack<(TNode Node, bool Visited)> stack = [];
            stack.Push((root, false));

            while (stack.Count > 0) {
                var (node, visited) = stack.Pop();
                if (!visited) {
                    stack.Push((node, true));
                    if (node.Right != null) {
                        stack.Push((node.Right, false));
                    }

                    if (node.Left != null) {
                        stack.Push((node.Left, false));
                    }
                }
                else {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        private static Dictionary<TNode, int> BuildHeightMap(TNode root) {
            Dictionary<TNode, int> heights = [];
            Stack<(TNode Node, bool Visited)> stack = [];
            stack.Push((root, false));

            while (stack.Count > 0) {
                var (node, visited) = stack.Pop();
                if (!visited) {
                    stack.Push((node, true));

                    if (node.Left != null) {
                        stack.Push((node.Left, false));
                    }

                    if (node.Right != null) {
                        stack.Push((node.Right, false));
                    }
                }
                else {
                    int leftHeight = node.Left != null ? heights[node.Left] : 0;
                    int rightHeight = node.Right != null ? heights[node.Right] : 0;
                    heights[node] = Math.Max(leftHeight, rightHeight) + 1;
                }
            }

            return heights;
        }
    }

    private enum TraversalStrategy {
        InOrder,
        PreOrder,
        PostOrder,
        InOrderReverse,
        PreOrderReverse,
        PostOrderReverse
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        List<KeyValuePair<TKey, TValue>> items = new(Count);
        foreach (var entry in InOrder()) {
            items.Add(new KeyValuePair<TKey, TValue>(entry.Key, entry.Value));
        }

        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear() {
        Root = null;
        Count = 0;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        if (!TryGetValue(item.Key, out TValue? currentValue)) {
            return false;
        }

        return EqualityComparer<TValue>.Default.Equals(currentValue, item.Value);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        if (arrayIndex < 0) {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < Count) {
            throw new ArgumentException("Destination array does not have enough space.");
        }

        int index = arrayIndex;
        foreach (var entry in InOrder()) {
            array[index++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}


