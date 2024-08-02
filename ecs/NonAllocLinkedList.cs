using System.Collections;

namespace ecs;

/// <summary>
/// Only in use by ObjPool at the moment, so a  bit underdeveloped
/// </summary>
public class NonAllocLinkedList<T>
{
  private const int                              DEFAULT_POOL_SIZE = 5;

  private Dictionary<T, NonAllocLinkedListNode<T>> _nodeLookup = new Dictionary<T, NonAllocLinkedListNode<T>>();
  
  private NonAllocLinkedListNode<T>?               _tail;
  private Stack<NonAllocLinkedListNode<T>>         _pool = new Stack<NonAllocLinkedListNode<T>>();
  
  public int Count { get; private set; }

  public NonAllocLinkedList()
  {
    for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
    {
      _pool.Push(new NonAllocLinkedListNode<T>());
    }
  }

  private NonAllocLinkedListNode<T> TakeNode()
  {
    if (_pool.Count == 0)
    {
      for (int i = 0; i < Count * 2; i++)
      {
        _pool.Push(new NonAllocLinkedListNode<T>());
      }
    }

    return _pool.Pop();
  }

  public T this[int idx]
  {
    get
    {
      if (idx < 0 || idx >= Count)
      {
        throw new Exception();
      }

      NonAllocLinkedListNode<T>? node = _tail;
      for (int i = 0; i < Count - idx - 1; i++)
      {
        node = node.Prev;
      }

      return node.Object;
    }
  }

  public void Add(T item)
  {
    if (_tail == null)
    {
      _tail = TakeNode();
      _nodeLookup.Add(item, _tail);
      _tail.Object = item;
      _tail.Next = null;
      _tail.Prev = null;
    }
    else
    {
      NonAllocLinkedListNode<T> newNode = TakeNode();
      _nodeLookup.Add(item, newNode);
      newNode.Object = item;
      
      _tail.Next = newNode;
      newNode.Prev = _tail;
      _tail = newNode;
    }

    Count++;
  }

  public bool Remove(T item)
  {
    if (!_nodeLookup.Remove(item, out NonAllocLinkedListNode<T> node))
    {
      return false;
    }

    bool isTail = _tail == node;

    if (node.Prev != null)
    {
      node.Prev.Next = node.Next;
    }

    if (node.Next != null)
    {
      node.Next.Prev = node.Prev;
    }

    if (isTail)
    {
      _tail = node.Prev;
    }

    node.Reset();
    _pool.Push(node);
    Count--;
    return true;
  }
}

public class NonAllocLinkedListNode<T>
{
  public T?                         Object { get; set; }
  public NonAllocLinkedListNode<T>? Next   { get; set; }
  public NonAllocLinkedListNode<T>? Prev   { get; set; }

  public void Reset()
  {
    Object = default;
    Next = null;
    Prev = null;
  }
}