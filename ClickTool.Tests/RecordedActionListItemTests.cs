using ClickTool.Models;
using Xunit;

namespace ClickTool.Tests;

public sealed class RecordedActionListItemTests
{
    [Fact]
    public void IsSelected_RaisesPropertyChanged_WhenValueChanges()
    {
        var item = new RecordedActionListItem
        {
            Index = 0,
            StepNumber = 1,
            Action = new MouseAction
            {
                Action = MouseActionType.Move,
                X = 10,
                Y = 20,
                DelayMs = 0
            }
        };

        var raisedProperties = new List<string?>();
        item.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        item.IsSelected = true;

        Assert.True(item.IsSelected);
        Assert.Contains(nameof(RecordedActionListItem.IsSelected), raisedProperties);
    }
}
