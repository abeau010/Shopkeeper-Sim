using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;

// One line in a ListMenu
public class ListMenuRow
{
    public string text;
    public bool canSelect = true;
    public Func<string> confirm; // Returns null when done (the menu closes), otherwise a message to show
}

// List above a shelf or crate. Press E to open it, then pick a row with the arrow keys and E
public class ListMenu : MonoBehaviour
{
    public static ListMenu Current { get; private set; }
    // Stays true on the frame the menu closes so the same E press can't reopen it (used to lock player movement/interaction)
    public static bool IsOpen => Current != null || Time.frameCount == closedFrame;

    private const string SelectedColor = "#FFD54A";
    private const string UnavailableColor = "#A0A0A0";
    private const string MessageColor = "#FFB3A0";

    private static int closedFrame = -1;

    private string title;
    private Func<PlayerHands, List<ListMenuRow>> rowBuilder;
    private int maxVisibleRows;
    private bool showRowsWhenClosed;

    private List<ListMenuRow> rows = new List<ListMenuRow>();
    private PlayerHands playerHands;
    private TextMeshPro label;
    private int selected = -1;
    private int firstVisible;
    private int openedFrame;
    private string message = "";

    // maxVisibleRows above 0 makes long lists scroll
    public void Init(string menuTitle, Func<PlayerHands, List<ListMenuRow>> buildRows, int visibleRows = 0, bool rowsWhenClosed = true)
    {
        title = menuTitle;
        rowBuilder = buildRows;
        maxVisibleRows = visibleRows;
        showRowsWhenClosed = rowsWhenClosed;
        label = CreateLabel();
        Refresh();
    }

    public void SetLabelVisible(bool visible) 
    {
        if (label != null) 
        {
            label.gameObject.SetActive(visible);
        }
    }

    public void Open(PlayerHands hands)
    {
        if (Current != null) return;
        playerHands = hands;
        rows = rowBuilder(playerHands);
        selected = rows.FindIndex(r => r.canSelect);
        if (selected < 0) // Nothing to pick, so don't freeze the player
        {
            playerHands = null;
            Refresh();
            return;
        }
        Current = this;
        openedFrame = Time.frameCount;
        message = "";
        firstVisible = 0;
        KeepSelectedVisible();
        SetLabelVisible(true);
        Draw();
    }

    public void Close()
    {
        Current = null;
        closedFrame = Time.frameCount;
        playerHands = null;
        selected = -1;
        message = "";
        Refresh();
    }

    // Rebuilds the rows after the contents change
    public void Refresh()
    {
        rows = rowBuilder(playerHands);
        Draw();
    }

    private void Update()
    {
        if (Current != this) return;

        var kb = Keyboard.current;
        if (kb == null || Time.frameCount == openedFrame) return; // Skip the frame E was pressed to open
        if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }

        if (kb.upArrowKey.wasPressedThisFrame) MoveSelection(-1);
        if (kb.downArrowKey.wasPressedThisFrame) MoveSelection(1);
        if (kb.eKey.wasPressedThisFrame) Confirm();
    }

    private void OnDisable()
    {
        if (Current == this) Current = null;
    }

    // Moves to the next row that can be picked, wrapping around the list
    private void MoveSelection(int direction)
    {
        for (int i = 1; i < rows.Count; i++)
        {
            int next = (selected + direction * i + rows.Count) % rows.Count;
            if (rows[next].canSelect)
            {
                selected = next;
                message = "";
                KeepSelectedVisible();
                Draw();
                return;
            }
        }
    }

    private void KeepSelectedVisible()
    {
        if (maxVisibleRows <= 0) return;
        if (selected < firstVisible) firstVisible = selected;
        if (selected >= firstVisible + maxVisibleRows) firstVisible = selected - maxVisibleRows + 1;
    }

    // The list stays open if the row reports a problem
    private void Confirm()
    {
        message = rows[selected].confirm() ?? "";
        if (message == "") Close();
        else Draw();
    }

    private void Draw()
    {
        bool isOpen = Current == this;
        bool scrolling = isOpen && maxVisibleRows > 0 && rows.Count > maxVisibleRows;
        var text = new StringBuilder($"<b>{title}</b>");
        if (scrolling) text.Append($" ({selected + 1}/{rows.Count})");

        if (isOpen || showRowsWhenClosed)
        {
            int first = scrolling ? firstVisible : 0;
            int end = scrolling ? firstVisible + maxVisibleRows : rows.Count;
            for (int i = first; i < end; i++)
            {
                ListMenuRow row = rows[i];
                string line = row.text;
                if (i == selected) line = $"<color={SelectedColor}>><pos=1em>{line}</color>";
                else if (!row.canSelect) line = $"<pos=1em><color={UnavailableColor}>{line}</color>";
                else line = $"<pos=1em>{line}";
                text.Append('\n').Append(line);
            }
        }
        if (message != "") text.Append($"\n<color={MessageColor}>{message}</color>");
        label.text = text.ToString();
    }

    // List text above the object, grows upwards as rows are added
    private TextMeshPro CreateLabel()
    {
        var go = new GameObject("List");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(-0.5f, 0.6f, 0f);
        var list = go.AddComponent<TextMeshPro>();
        list.fontSize = 3.2f;
        list.alignment = TextAlignmentOptions.BottomLeft;
        list.rectTransform.pivot = new Vector2(0f, 0f);
        list.rectTransform.sizeDelta = new Vector2(3.5f, 2.5f);
        list.sortingOrder = 90;
        return list;
    }
}
