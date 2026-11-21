using System.Windows;
using System.Windows.Controls;

namespace DxTools.App.Services;

internal static class MenuHeaderFactory
{
    private const double MaxHeaderWidth = 360;

    public static TextBlock Create(string text)
    {
        return new TextBlock
        {
            Text = text,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = MaxHeaderWidth,
            ToolTip = text,
        };
    }
}
