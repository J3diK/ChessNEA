using System;
using System.Diagnostics;

namespace ChessNEA.Logic.Objects.LinkedList;

public class LinkedList<T>
{
    public Node<T>? Head { get; private set; }
    private Node<T>? Tail { get; set; }
    public int Count { get; private set; }

    /// <summary>
    ///     Allows a node to be added to the end (tail) of the linked list.
    /// </summary>
    /// <param name="data">The node/data to be added</param>
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

        Count++;
    }

    /// <summary>
    ///     Splits the linked list into two separate linked lists at the
    ///     specified index.
    /// </summary>
    /// <param name="index">The index to split at</param>
    /// <returns>The two split left and right lists</returns>
    /// <exception cref="IndexOutOfRangeException">
    ///     If the index is greater than the length of the list.
    /// </exception>
    public (LinkedList<T>, LinkedList<T>) SplitList(int index)
    {
        LinkedList<T> left = new();
        LinkedList<T> right = new();

        Node<T>? node = Head;
        for (int i = 0; i < index; i++)
        {
            if (node is null) throw new IndexOutOfRangeException();
            left.AddNode(node.Data!);
            node = node.NextNode;
        }

        while (node is not null)
        {
            right.AddNode(node.Data!);
            node = node.NextNode;
        }

        return (left, right);
    }

    /// <summary>
    ///     Accesses a node at a specified index.
    /// </summary>
    /// <param name="index">The index to access</param>
    /// <returns>The node at the index</returns>
    /// <exception cref="IndexOutOfRangeException">
    ///     If the index is greater than the length of the list.
    /// </exception>
    public Node<T> GetNode(int index)
    {
        Node<T>? node;
        
        if (index <= Count / 2)
        {
            node = Head;
            for (int i = 0; i < index; i++)
            {
                if (node is null) throw new IndexOutOfRangeException();
                node = node.NextNode;
            }
        }
        else
        {
            node = Tail;
            for (int i = 0; i < Count - index; i++)
            {
                if (node is null) throw new IndexOutOfRangeException();
                node = node.PreviousNode;
            }
        }

        if (node is null) throw new IndexOutOfRangeException();

        return node;
    }

    /// <summary>
    ///     Remove a node from the linked list. Specifically the first node
    ///     that matches the data node parameter.
    /// </summary>
    /// <param name="data">The data/node to remove</param>
    public void RemoveNode(T data)
    {
        Node<T>? node = Head;

        if (node is null) return;

        if (node.Data != null && node.Data.Equals(data))
        {
            Head = node.NextNode;
            if (Head != null) Head.PreviousNode = null;
            Count--;
            return;
        }

        while (node.NextNode is not null)
            try
            {
                node = node.NextNode;
                if (node.Data == null || !node.Data.Equals(data)) continue;
                node.PreviousNode!.NextNode = node.NextNode;
                if (node.NextNode == null) continue;
                node.NextNode.PreviousNode = node.PreviousNode;
                Count--;
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
    }

    /// <summary>
    ///     Adds two lists together.
    /// </summary>
    /// <param name="list1">The left list to add</param>
    /// <param name="list2">The right list to add</param>
    /// <returns>The second list appended to the first</returns>
    public static LinkedList<T>? operator +(LinkedList<T> list1,
        LinkedList<T> list2)
    {
        if (list1.Head is null) return list2.Head is null ? null : list2;
        if (list2.Head is null) return list1.Head is null ? null : list1;

        Node<T>? node = list2.Head;

        while (node is not null)
        {
            Debug.Assert(node.Data != null, "node.Data != null");
            list1.AddNode(node.Data);
            node = node.NextNode;
        }

        return list1;
    }

    /// <summary>
    ///     Check if the linked list contains a specific node/data.
    /// </summary>
    /// <param name="data">The data to check for</param>
    /// <returns>Whether the data is contained</returns>
    public bool Contains(T data)
    {
        Node<T>? node = Head;

        if (node is null) return false;

        if (node.Data != null && node.Data.Equals(data)) return true;

        while (node.NextNode is not null)
        {
            node = node.NextNode;
            if (node.Data != null && node.Data.Equals(data)) return true;
        }

        return false;
    }
}