namespace DxTools.Core;

public sealed class DesktopIconPosition
{
    public int Index { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public DesktopIconPosition()
    {
    }

    public DesktopIconPosition(int index, int x, int y)
    {
        Index = index;
        X = x;
        Y = y;
    }
}

public sealed class DesktopIconProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public List<DesktopIconPosition> Icons { get; set; } = [];
}
