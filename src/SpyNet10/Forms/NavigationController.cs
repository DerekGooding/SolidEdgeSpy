// Based off http://www.codeproject.com/Articles/13943/Navigational-history-go-back-forward-for-WinForms.

using System.Diagnostics;

namespace SpyNet10.Forms;

/// <summary>Provides navigational history (go back/forward) in conjunction with 2 ToolStripSplitButtons</summary>
public sealed class NavigationController<T> : IDisposable where T : class
{
    public event EventHandler<NavigationControllerEventArgs<T>> GotoItem;

    public NavigationController(ToolStripButton back, ToolStripButton forward, uint limitList)
    {
        _linkedList = new LinkedList<T>();

        _buttonBack = back;
        _buttonForward = forward;
        AssignButton(back);
        AssignButton(forward);

        _limit = (int)limitList;
        _enabled = true;
    }

    #region Fields

    private T _currentItem;
    private readonly LinkedList<T> _linkedList;
    private LinkedListNode<T> _currentLinkedListNode;

    private readonly ToolStripButton _buttonBack;
    private readonly ToolStripButton _buttonForward;

    private bool _allowDuplicates;
    private bool _enabled;
    private bool _inProc;
    private readonly int _limit;

    #endregion Fields

    #region Public Interface

    public bool AllowDuplicates
    {
        get => _allowDuplicates; set => _allowDuplicates = value;
    }

    public bool Enabled
    {
        get => _enabled; set => _enabled = value;
    }

    public T CurrentItem
    {
        get => _currentItem;
        set
        {
            if (_enabled)
            {
                _currentItem = value;

                if (!_inProc && _currentItem != null)
                {
                    AddCurrentItem(value);
                }
            }
        }
    }

    /// <summary></summary>
    public void Clear()
    {
        _currentItem = default(T);
        _currentLinkedListNode = null;
        _linkedList.Clear();
        EnableButtons();
    }

    /// <summary></summary>
    public void Remove(T item)
    {
        if (item != null)
        {
            if (_currentLinkedListNode != null && item.Equals(_currentLinkedListNode.Value))
            {
                _currentLinkedListNode = null;
            }

            if (_linkedList.Contains(item))
            {
                _linkedList.Remove(item);
            }
        }

        EnableButtons();
    }

    public void Remove(T[] items)
    {
        if (items != null)
        {
            foreach (var item in items)
            {
                Remove(item);
            }
        }
    }

    #endregion Public Interface

    #region Private Interface

    private void AddCurrentItem(T item)
    {
        if (!_allowDuplicates && _linkedList.Contains(item))
        {
            _linkedList.Remove(item);
        }

        _linkedList.AddFirst(item);

        _currentLinkedListNode = _linkedList.First;
        LimitList();
        EnableButtons();
    }

    private void toolStripButton_ButtonClick(object sender, EventArgs e)
    {
        var node = sender.Equals(_buttonBack) ? _currentLinkedListNode.Next : _currentLinkedListNode.Previous;

        OnGotoItem(node);

        _currentLinkedListNode = node;
        LimitList();
        EnableButtons();
    }

    private void cms_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
    {
        var node = (LinkedListNode<T>)e.ClickedItem.Tag;

        OnGotoItem(node);

        _currentLinkedListNode = node;
        LimitList();
        EnableButtons();
    }

    // limiting Back history is sufficient (by design Forward is less or equal to Back)
    private void LimitList()
    {
        if (_limit != 0 && _linkedList.Count > _limit)
        {
            var node = _currentLinkedListNode.Next;
            var count = 0;

            while (node != null)
            {
                if (++count > _limit)
                {
                    _linkedList.RemoveLast();
                }
                node = node.Next;
            }
        }
    }

    private void AssignButton(ToolStripButton toolStripButton)
    {
        if (toolStripButton != null)
        {
            toolStripButton.Click += toolStripButton_ButtonClick;
            toolStripButton.Enabled = false;
        }
    }

    private void EnableButtons()
    {
        if (_buttonBack != null)
            _buttonBack.Enabled = (_currentLinkedListNode != null && _currentLinkedListNode.Next != null);
        if (_buttonForward != null)
            _buttonForward.Enabled = (_currentLinkedListNode != null && _currentLinkedListNode.Previous != null);
    }

    private void OnGotoItem(LinkedListNode<T> node)
    {
        Debug.Assert(node != null && node.Value != null);

        var item = node.Value;

        // block client setting CurrentItem
        _inProc = true;

        if (GotoItem != null)
        {
            GotoItem(this, new NavigationControllerEventArgs<T>(item));
        }

        _inProc = false;
    }

    #endregion Private Interface

    #region IDisposable Members

    /// <summary></summary>
    public void Dispose()
    {
    }

    #endregion IDisposable Members
}

/// <summary>Provides data for the History{T}.GotoItem event</summary>
public class NavigationControllerEventArgs<T> : EventArgs
{
    public NavigationControllerEventArgs(T item) => _Item = item;

    private readonly T _Item;

    public T Item => _Item;
}