using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Input;
using MewUI.Test.Infrastructure;

namespace MewUI.Test.Commands;

/// <summary>
/// A sequence of several strokes runs on its last stroke; until then the window holds the strokes pressed so far and
/// every key goes to the sequence instead of the focused element.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class KeySequenceDispatchTests
{
    private static readonly KeyGesture _controlK = new(Key.K, ModifierKeys.Control);
    private static readonly KeyGesture _controlI = new(Key.I, ModifierKeys.Control);

    [TestMethod]
    public void TheLastStrokeRunsTheCommand()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out _);
        var command = new Command("edit.quickInfo");
        int executed = 0;
        window.Commands.Register(command, () => executed++);
        window.InputMap.Map(command, new KeySequence(_controlK, _controlI));

        Assert.IsTrue(Send(window, Key.K, ModifierKeys.Control), "the first stroke was not taken");
        Assert.AreEqual(0, executed);
        Assert.AreEqual(new KeySequence(_controlK), window.PendingKeySequence);

        Assert.IsTrue(Send(window, Key.I, ModifierKeys.Control));
        Assert.AreEqual(1, executed);
        Assert.IsNull(window.PendingKeySequence);
    }

    [TestMethod]
    public void AKeyThatDoesNotContinueTheSequenceIsSwallowed()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out var seenByEditor);
        int sequenceExecuted = 0;
        int singleExecuted = 0;
        window.InputMap.Map(new KeySequence(_controlK, _controlI), () => sequenceExecuted++);
        window.InputMap.Map(new KeyGesture(Key.X), () => singleExecuted++);

        Send(window, Key.K, ModifierKeys.Control);
        seenByEditor.Clear();

        Assert.IsTrue(Send(window, Key.X), "the key would go on to type into the focused element");
        Assert.AreEqual(0, seenByEditor.Count, "the focused element saw the key");
        Assert.AreEqual(0, singleExecuted, "the key ran its own mapping");
        Assert.AreEqual(0, sequenceExecuted);
        Assert.IsNull(window.PendingKeySequence);

        Send(window, Key.X);
        Assert.AreEqual(1, singleExecuted, "the key after the cancelled sequence is an ordinary key again");
        Assert.AreEqual(1, seenByEditor.Count);
    }

    [TestMethod]
    public void EscapeCancelsTheSequence()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out var seenByEditor);
        int executed = 0;
        window.InputMap.Map(new KeySequence(_controlK, _controlI), () => executed++);

        Send(window, Key.K, ModifierKeys.Control);
        seenByEditor.Clear();

        Assert.IsTrue(Send(window, Key.Escape));
        Assert.IsNull(window.PendingKeySequence);
        Assert.AreEqual(0, seenByEditor.Count);

        Send(window, Key.I, ModifierKeys.Control);
        Assert.AreEqual(0, executed);
    }

    [TestMethod]
    public void TheSequenceIsDroppedWhenTheNextStrokeComesTooLate()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out var seenByEditor);
        long now = 1000;
        window.PendingKeySequenceClock = () => now;
        int executed = 0;
        window.InputMap.Map(new KeySequence(_controlK, _controlI), () => executed++);

        Send(window, Key.K, ModifierKeys.Control);
        now += 4999;
        Assert.IsTrue(Send(window, Key.I, ModifierKeys.Control));
        Assert.AreEqual(1, executed, "a stroke inside the limit did not complete the sequence");

        Send(window, Key.K, ModifierKeys.Control);
        now += 5000;
        seenByEditor.Clear();
        Assert.IsFalse(Send(window, Key.I, ModifierKeys.Control), "a late stroke was still taken by the sequence");
        Assert.AreEqual(1, executed);
        Assert.AreEqual(1, seenByEditor.Count, "the late stroke is an ordinary key");
        Assert.IsNull(window.PendingKeySequence);
    }

    [TestMethod]
    public void DeactivatingTheWindowCancelsTheSequence()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out _);
        window.InputMap.Map(new KeySequence(_controlK, _controlI), static () => { });
        window.SetIsActive(true);

        Send(window, Key.K, ModifierKeys.Control);
        Assert.IsNotNull(window.PendingKeySequence);

        window.SetIsActive(false);
        Assert.IsNull(window.PendingKeySequence);
    }

    [TestMethod]
    public void AModifierPressedBeforeTheNextStrokeKeepsTheSequence()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out _);
        int executed = 0;
        window.InputMap.Map(new KeySequence(_controlK, _controlI), () => executed++);

        Send(window, Key.K, ModifierKeys.Control);
        Send(window, Key.None, ModifierKeys.Control);
        Assert.IsNotNull(window.PendingKeySequence);

        Send(window, Key.I, ModifierKeys.Control);
        Assert.AreEqual(1, executed);
    }

    [TestMethod]
    public void EveryStrokeBeforeTheLastExtendsThePendingSequence()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out _);
        int executed = 0;
        var sequence = KeySequence.Parse("Ctrl+K, Ctrl+I, G");
        window.InputMap.Map(sequence, () => executed++);

        Send(window, Key.K, ModifierKeys.Control);
        Send(window, Key.I, ModifierKeys.Control);
        Assert.AreEqual(new KeySequence(_controlK, _controlI), window.PendingKeySequence);
        Assert.AreEqual(0, executed);

        Send(window, Key.G);
        Assert.AreEqual(1, executed);
        Assert.IsNull(window.PendingKeySequence);
    }

    [TestMethod]
    public void InOneMapASingleGestureRunsBeforeASequenceStartingWithIt()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out _);
        int singleExecuted = 0;
        int sequenceExecuted = 0;
        window.InputMap.Map(_controlK, () => singleExecuted++);
        window.InputMap.Map(new KeySequence(_controlK, _controlI), () => sequenceExecuted++);

        Send(window, Key.K, ModifierKeys.Control);
        Assert.AreEqual(1, singleExecuted);
        Assert.IsNull(window.PendingKeySequence);

        Send(window, Key.I, ModifierKeys.Control);
        Assert.AreEqual(0, sequenceExecuted);
    }

    [TestMethod]
    public void TheNearestMapDecidesWhetherAStrokeStartsASequence()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out var editor, out _);
        int windowSingleExecuted = 0;
        int editorSequenceExecuted = 0;
        window.InputMap.Map(_controlK, () => windowSingleExecuted++);
        editor.InputMap.Map(new KeySequence(_controlK, _controlI), () => editorSequenceExecuted++);

        Send(window, Key.K, ModifierKeys.Control);
        Assert.AreEqual(0, windowSingleExecuted, "the farther single gesture ran although the nearer map starts a sequence");
        Assert.IsNotNull(window.PendingKeySequence);

        Send(window, Key.I, ModifierKeys.Control);
        Assert.AreEqual(1, editorSequenceExecuted);
    }

    [TestMethod]
    public void ASequenceIsTheShortcutShownForItsCommand()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out var editor, out _);
        var command = new Command("edit.quickInfo");
        var sequence = new KeySequence(_controlK, _controlI);
        window.InputMap.Map(command, sequence);

        Assert.AreEqual("Ctrl+K, Ctrl+I", InputMapResolver.GetEffectiveGestureText(window, command, editor));
        Assert.IsTrue(window.InputMap.TryGetPrimarySequence(command, out var primary));
        Assert.AreEqual(sequence, primary);
        Assert.IsFalse(window.InputMap.TryGetPrimaryGesture(command, out _), "a sequence is not a single gesture");

        // A nearer map that gives the first stroke another meaning leaves the sequence unreachable from there.
        editor.InputMap.Map(_controlK, static () => { });
        Assert.IsNull(InputMapResolver.GetEffectiveGestureText(window, command, editor));
    }

    [TestMethod]
    public void UnmappingTheSequenceFreesItsFirstStroke()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("GDI backend is Windows-only."); return; }

        var window = CreateWindow(out _, out var seenByEditor);
        var sequence = new KeySequence(_controlK, _controlI);
        window.InputMap.Map(sequence, static () => { });
        Assert.IsTrue(window.InputMap.Unmap(sequence));

        Assert.IsFalse(Send(window, Key.K, ModifierKeys.Control));
        Assert.IsNull(window.PendingKeySequence);
        Assert.AreEqual(1, seenByEditor.Count);
    }

    private static Window CreateWindow(out Button editor, out List<Key> seenByEditor)
    {
        var window = HeadlessWindow.Create();
        editor = new Button();
        window.Content = editor;
        window.PerformLayout();
        window.FocusManager.SetFocus(editor);

        var seen = new List<Key>();
        editor.KeyDown += args => seen.Add(args.Key);
        seenByEditor = seen;
        return window;
    }

    private static bool Send(Window window, Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        var args = new KeyEventArgs(key, platformKey: 0, modifiers);
        WindowInputRouter.KeyDown(window, args);
        return args.Handled;
    }
}
