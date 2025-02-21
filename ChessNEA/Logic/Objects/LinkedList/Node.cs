namespace ChessNEA.Logic.Objects.LinkedList;

public class Node<T>
{
    public T? Data { get; init; }
    public Node<T>? NextNode { get; set; }
    public Node<T>? PreviousNode { get; set; }
}