using System;
using System.Diagnostics;

namespace ChessNEA.Logic.Objects.LinkedList;

public class LinkedList<T>
{
    public Node<T>? Head { get; private set; }
    private Node<T>? Tail { get; set; }
    private int _count;

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
    
    public void RemoveNode(T data)
    {
        Node<T>? node = Head;
        
        if (node is null)
        {
            return;
        }

        if (node.Data != null && node.Data.Equals(data))
        {
            Head = node.NextNode;
            if (Head != null) Head.PreviousNode = null;
            _count--;
            return;
        }
        
        while (node.NextNode is not null)
        {
            try
            {
                node = node.NextNode;
                if (node.Data == null || !node.Data.Equals(data)) continue;
                node.PreviousNode!.NextNode = node.NextNode;
                if (node.NextNode == null) continue;
                node.NextNode.PreviousNode = node.PreviousNode;
                _count--;
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
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

        Node<T>? node = list2.Head;

        while (node is not null)
        {
            Debug.Assert(node.Data != null, "node.Data != null");
            list1.AddNode(node.Data);
            node = node.NextNode;
        }

        return list1;
    }

    public bool Contains(T data)
    {
        Node<T>? node = Head;
        
        if (node is null)
        {
            return false;
        }

        if (node.Data != null && node.Data.Equals(data))
        {
            return true;
        }
        
        while (node.NextNode is not null)
        {
            node = node.NextNode;
            if (node.Data != null && node.Data.Equals(data))
            {
                return true;
            }
        }

        return false;
    }
}