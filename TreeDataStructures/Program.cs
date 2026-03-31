using TreeDataStructures.Implementations.AVL;
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Implementations.RedBlackTree;
using TreeDataStructures.Implementations.Splay;
using TreeDataStructures.Implementations.Treap;
using TreeDataStructures.Interfaces;

ITree<int, string> tree = CreateTree("bst");

PrintWelcome();

while (true)
{
    Console.Write("> ");
    string? line = Console.ReadLine();
    if (line == null)
    {
        break;
    }

    string trimmed = line.Trim();
    if (trimmed.Length == 0)
    {
        continue;
    }

    string[] parts = trimmed.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
    string command = parts[0].ToLowerInvariant();

    try
    {
        switch (command)
        {
            case "help":
                PrintHelp();
                break;

            case "type":
                if (parts.Length < 2)
                {
                    Console.WriteLine("Usage: type <bst|avl|rb|splay|treap>");
                    break;
                }

                tree = CreateTree(parts[1]);
                Console.WriteLine($"Switched to {tree.GetType().Name}. Tree is empty.");
                break;

            case "add":
                if (parts.Length < 3 || !TryParseKey(parts[1], out int addKey))
                {
                    Console.WriteLine("Usage: add <int-key> <value>");
                    break;
                }

                tree[addKey] = parts[2];
                Console.WriteLine($"OK. Count={tree.Count}");
                break;

            case "remove":
                if (parts.Length < 2 || !TryParseKey(parts[1], out int removeKey))
                {
                    Console.WriteLine("Usage: remove <int-key>");
                    break;
                }

                Console.WriteLine(tree.Remove(removeKey) ? "Removed." : "Key not found.");
                Console.WriteLine($"Count={tree.Count}");
                break;

            case "get":
                if (parts.Length < 2 || !TryParseKey(parts[1], out int getKey))
                {
                    Console.WriteLine("Usage: get <int-key>");
                    break;
                }

                if (tree.TryGetValue(getKey, out string? value))
                {
                    Console.WriteLine(value);
                }
                else
                {
                    Console.WriteLine("Key not found.");
                }

                break;

            case "contains":
                if (parts.Length < 2 || !TryParseKey(parts[1], out int containsKey))
                {
                    Console.WriteLine("Usage: contains <int-key>");
                    break;
                }

                Console.WriteLine(tree.ContainsKey(containsKey) ? "true" : "false");
                break;

            case "print":
                PrintTraversals(tree);
                break;

            case "count":
                Console.WriteLine(tree.Count);
                break;

            case "clear":
                tree.Clear();
                Console.WriteLine("Cleared.");
                break;

            case "exit":
            case "quit":
                return;

            default:
                Console.WriteLine("Unknown command. Use 'help'.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static bool TryParseKey(string text, out int key) => int.TryParse(text, out key);

static ITree<int, string> CreateTree(string kind)
{
    return kind.Trim().ToLowerInvariant() switch
    {
        "bst" => new BinarySearchTree<int, string>(),
        "avl" => new AvlTree<int, string>(),
        "rb" => new RedBlackTree<int, string>(),
        "splay" => new SplayTree<int, string>(),
        "treap" => new Treap<int, string>(),
        _ => throw new ArgumentException("Unknown tree type. Use bst|avl|rb|splay|treap.")
    };
}

static void PrintTraversals(ITree<int, string> tree)
{
    Console.WriteLine($"Type: {tree.GetType().Name}");
    Console.WriteLine($"Count: {tree.Count}");
    Console.WriteLine($"InOrder:         {string.Join(", ", tree.InOrder().Select(e => $"{e.Key}:{e.Value}[h={e.Depth}]"))}");
    Console.WriteLine($"PreOrder:        {string.Join(", ", tree.PreOrder().Select(e => $"{e.Key}:{e.Value}[h={e.Depth}]"))}");
    Console.WriteLine($"PostOrder:       {string.Join(", ", tree.PostOrder().Select(e => $"{e.Key}:{e.Value}[h={e.Depth}]"))}");
    Console.WriteLine($"InOrderReverse:  {string.Join(", ", tree.InOrderReverse().Select(e => $"{e.Key}:{e.Value}[h={e.Depth}]"))}");
    Console.WriteLine($"PreOrderReverse: {string.Join(", ", tree.PreOrderReverse().Select(e => $"{e.Key}:{e.Value}[h={e.Depth}]"))}");
    Console.WriteLine($"PostOrderReverse:{string.Join(", ", tree.PostOrderReverse().Select(e => $"{e.Key}:{e.Value}[h={e.Depth}]"))}");
}

static void PrintWelcome()
{
    Console.WriteLine("Tree playground started. Default type: bst");
    PrintHelp();
}

static void PrintHelp()
{
    Console.WriteLine("Commands:");
    Console.WriteLine("  help");
    Console.WriteLine("  type <bst|avl|rb|splay|treap>");
    Console.WriteLine("  add <int-key> <value>");
    Console.WriteLine("  remove <int-key>");
    Console.WriteLine("  get <int-key>");
    Console.WriteLine("  contains <int-key>");
    Console.WriteLine("  print");
    Console.WriteLine("  count");
    Console.WriteLine("  clear");
    Console.WriteLine("  exit");
}
