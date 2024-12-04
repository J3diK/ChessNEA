namespace ChessNEA.Logic.Objects.LinkedList;

public class LinkedList<T>
{
    public Node<T>? Head { get; private set; }
    public Node<T>? Tail { get; private set; }
    private int _count = -1;

    public void AddNode(T data)
    {
        Node<T> node = new()
        {
            Data = data
        };

        if (Head is null)
        {
            Head = node;
            Tail = Head;
        }
        else
        {
            Tail!.NextNode = node;
            Tail!.NextNode.PreviousNode = Tail;
            Tail = Tail.NextNode;

        }

        _count++;
    }

    public static LinkedList<T>? operator+(LinkedList<T> list1, LinkedList<T> list2)
    {
        if (list1.Head is null)
        {
            return list2.Head is null ? null : list2;
        }
        if (list2.Head is null)
        {
            return list1.Head is null ? null : list1;
        }

        list1.AddNode(list2.Head);

        return list1;
    }

    private void AddNode(Node<T> node)
    {
        if (Head is null)
        {
            Head = node;
            Tail = Head;
        }
        else
        {
            Tail!.NextNode = node;
            Tail!.NextNode.PreviousNode = Tail;
            Tail = Tail.NextNode;

        }

        _count++;
    }
}