using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace MewUI.WindowAutomationTest;

/// <summary>
/// A punctuation key pressed on the real keyboard path arrives named, so a shortcut such as Ctrl+. can be told from any
/// other key without reading the platform's own key code.
/// </summary>
[TestClass]
public sealed class PunctuationKeyTests
{
    [TestMethod]
    public async Task ControlPeriodArrivesAsThePeriodKey()
    {
        Assert.IsTrue(RealAppSession.IsAvailable, "the application loop did not start on this platform");

        var pressed = new List<(Key Key, ModifierKeys Modifiers)>();
        await RealAppSession.RunAsync(async () =>
        {
            var window = new Window
            {
                Title = "PunctuationKey",
                StartupLocation = WindowStartupLocation.Manual,
                WindowSize = WindowSize.Fixed(320, 200),
                Content = new TextBlock { Text = "keys" },
            };
            window.PreviewKeyDown += e => pressed.Add((e.Key, e.Modifiers));

            try
            {
                window.Show();
                window.MoveTo(160, 160);
                await Task.Delay(400);
                await RealInput.Current.ActivateAsync(window);
                await RealInput.Current.ControlPeriodAsync(window);
                await Task.Delay(200);
            }
            finally
            {
                window.Close();
            }
        });

        CollectionAssert.Contains(pressed, (Key.Period, ModifierKeys.Control),
            $"the period key was not named; pressed: {string.Join(", ", pressed)}");
    }
}
