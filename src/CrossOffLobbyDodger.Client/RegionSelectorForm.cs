using System.Drawing;

namespace CrossOff.LobbyDodger;

public sealed class RegionSelectorForm : Form
{
    private Point _dragStart;
    private Rectangle _selection;
    private bool _dragging;

    public RegionSelectorForm(Rectangle currentSelection)
    {
        Bounds = SystemInformation.VirtualScreen;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        Opacity = 0.42;
        DoubleBuffered = true;

        if (currentSelection.Width > 0 && currentSelection.Height > 0)
        {
            _selection = new Rectangle(
                currentSelection.X - Bounds.X,
                currentSelection.Y - Bounds.Y,
                currentSelection.Width,
                currentSelection.Height);
        }
    }

    public Rectangle SelectedScreenRegion => new(
        _selection.X + Bounds.X,
        _selection.Y + Bounds.Y,
        _selection.Width,
        _selection.Height);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragStart = e.Location;
        _selection = Rectangle.Empty;
        _dragging = true;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
        {
            return;
        }

        _selection = NormalizeRectangle(_dragStart, e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging || e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = false;
        _selection = NormalizeRectangle(_dragStart, e.Location);
        if (_selection.Width >= 20 && _selection.Height >= 20)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var selectionPen = new Pen(Color.DeepSkyBlue, 3);
        using var fillBrush = new SolidBrush(Color.FromArgb(80, Color.DeepSkyBlue));
        using var instructionFont = new Font(Font.FontFamily, 18, FontStyle.Bold);
        using var instructionBrush = new SolidBrush(Color.White);
        using var shadowBrush = new SolidBrush(Color.Black);

        if (!_selection.IsEmpty)
        {
            e.Graphics.FillRectangle(fillBrush, _selection);
            e.Graphics.DrawRectangle(selectionPen, _selection);
        }

        const string instructions = "Drag around the lobby player names • Esc cancels";
        PointF location = new(31, 31);
        e.Graphics.DrawString(instructions, instructionFont, shadowBrush, location);
        e.Graphics.DrawString(instructions, instructionFont, instructionBrush, new PointF(29, 29));
    }

    private static Rectangle NormalizeRectangle(Point first, Point second)
    {
        int left = Math.Min(first.X, second.X);
        int top = Math.Min(first.Y, second.Y);
        int right = Math.Max(first.X, second.X);
        int bottom = Math.Max(first.Y, second.Y);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }
}
