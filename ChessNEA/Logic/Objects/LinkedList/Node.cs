namespace ChessNEA.Logic.Objects.LinkedList;

public class Node<T>(T data)
{
    public T Data { get; set; } = data;
    public Node<T>? NextNode { get; set; }
    public Node<T>? PreviousNode { get; set; }
}