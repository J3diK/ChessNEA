namespace ChessNEA.Logic.Objects.LinkedList;

public class LinkedList<T>(Node<T> currentNode)
{
    private void GoToTail()
    {
        while (currentNode.NextNode is not null)
        {
            currentNode = currentNode.NextNode;
        }
    }

    public void GoToHead()
    {
        while (currentNode.PreviousNode is not null)
        {
            currentNode = currentNode.PreviousNode;
        }
    }

    public void AddNode(Node<T> newNode)
    {
        GoToTail();
        currentNode.NextNode = newNode;
    }
}