using Aprillz.MewUI;
using Aprillz.MewUI.Platform;

namespace MewUI.Test.Input;

/// <summary>
/// A key sequence is written the same way on every platform, reads back to the same strokes, and is shown through the
/// platform's own gesture format.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class KeySequenceTests
{
    [TestMethod]
    public void TwoStrokesParseInOrder()
    {
        var sequence = KeySequence.Parse("Ctrl+K, Ctrl+I");

        Assert.AreEqual(2, sequence.Length);
        Assert.IsFalse(sequence.IsSingle);
        Assert.AreEqual(new KeyGesture(Key.K, ModifierKeys.Control), sequence.Strokes[0]);
        Assert.AreEqual(new KeyGesture(Key.I, ModifierKeys.Control), sequence.Strokes[1]);
    }

    [TestMethod]
    [DataRow("Ctrl+K, Ctrl+I")]
    [DataRow("Primary+K, Primary+I")]
    [DataRow("Ctrl+Shift+-")]
    [DataRow("Ctrl+.")]
    [DataRow("Ctrl+,, Ctrl+K")]
    [DataRow("Ctrl+=")]
    [DataRow("Alt+Shift+F10")]
    [DataRow("Ctrl+1")]
    [DataRow("Ctrl+\\")]
    [DataRow("Ctrl+IntlBackslash")]
    [DataRow("Primary+Ctrl+Alt+Shift+Meta+Enter")]
    [DataRow("G, G, G, G")]
    public void ThePortableFormReadsBackUnchanged(string text)
    {
        var sequence = KeySequence.Parse(text);

        Assert.AreEqual(text, sequence.ToString());
        Assert.AreEqual(sequence, KeySequence.Parse(sequence.ToString()));
    }

    [TestMethod]
    [DataRow("Ctrl+.", Key.Period, ModifierKeys.Control)]
    [DataRow("Ctrl+Period", Key.Period, ModifierKeys.Control)]
    [DataRow("Ctrl++", Key.Equal, ModifierKeys.Control)]
    [DataRow("Ctrl+Equal", Key.Equal, ModifierKeys.Control)]
    [DataRow("Ctrl+>", Key.Period, ModifierKeys.Control)]
    [DataRow("ctrl+shift+k", Key.K, ModifierKeys.Control | ModifierKeys.Shift)]
    [DataRow(" Ctrl + K ", Key.K, ModifierKeys.Control)]
    [DataRow("Primary+S", Key.S, ModifierKeys.Primary)]
    [DataRow("D7", Key.D7, ModifierKeys.None)]
    [DataRow("7", Key.D7, ModifierKeys.None)]
    [DataRow("F5", Key.F5, ModifierKeys.None)]
    [DataRow("Add", Key.Add, ModifierKeys.None)]
    public void AKeyIsWrittenAsItsNameOrTheCharacterItTypes(string text, Key key, ModifierKeys modifiers)
        => Assert.AreEqual(new KeyGesture(key, modifiers), KeyGesture.Parse(text));

    [TestMethod]
    [DataRow("")]
    [DataRow("Ctrl")]
    [DataRow("Ctrl+")]
    [DataRow("Ctrl+None")]
    [DataRow("Ctrl+Nope")]
    [DataRow("Hyper+K")]
    [DataRow("K+Ctrl")]
    [DataRow("Ctrl+12")]
    [DataRow("Ctrl+K Ctrl+I")]
    [DataRow("Ctrl+K,")]
    [DataRow("A, B, C, D, E")]
    public void TextThatIsNotASequenceIsRejected(string text)
    {
        Assert.IsFalse(KeySequence.TryParse(text, out _));
        Assert.ThrowsExactly<FormatException>(() => KeySequence.Parse(text));
    }

    [TestMethod]
    public void AGestureIsNotASequenceOfSeveralStrokes()
    {
        Assert.IsFalse(KeyGesture.TryParse("Ctrl+K, Ctrl+I", out _));
        Assert.IsFalse(KeyGesture.TryParse(null, out _));
        Assert.IsTrue(KeyGesture.TryParse("Ctrl+K", out var gesture));
        Assert.AreEqual(new KeyGesture(Key.K, ModifierKeys.Control), gesture);
    }

    [TestMethod]
    public void AStrokeMustNameAKeyAndThereAreOneToFour()
    {
        var stroke = new KeyGesture(Key.G);

        Assert.ThrowsExactly<ArgumentException>(() => new KeySequence(new KeyGesture(Key.None, ModifierKeys.Control)));
        Assert.ThrowsExactly<ArgumentException>(() => new KeySequence(stroke, default));
        Assert.ThrowsExactly<ArgumentException>(() => new KeySequence(Array.Empty<KeyGesture>()));
        Assert.ThrowsExactly<ArgumentException>(() => new KeySequence(stroke, stroke, stroke, stroke, stroke));
        Assert.AreEqual(4, new KeySequence(stroke, stroke, stroke, stroke).Length);
    }

    [TestMethod]
    public void AGestureConvertsToASequenceOfOneStroke()
    {
        KeySequence sequence = new KeyGesture(Key.S, ModifierKeys.Control);

        Assert.IsTrue(sequence.IsSingle);
        Assert.AreEqual(new KeySequence(new KeyGesture(Key.S, ModifierKeys.Control)), sequence);
        Assert.AreNotEqual(new KeySequence(new KeyGesture(Key.S, ModifierKeys.Control), new KeyGesture(Key.S)), sequence);
        Assert.AreEqual(sequence.GetHashCode(), KeySequence.Parse("Ctrl+S").GetHashCode());
    }

    [TestMethod]
    public void ASequenceIsShownStrokeByStrokeInThePlatformFormat()
    {
        var sequence = KeySequence.Parse("Primary+K, Primary+Shift+.");
        var previous = PlatformConventions.Current;
        try
        {
            PlatformConventions.Current = new PlatformConventions();
            Assert.AreEqual("Ctrl+K, Ctrl+Shift+.", sequence.ToDisplayString());

            PlatformConventions.Current = new SymbolConventions();
            Assert.AreEqual("<Meta>K, <Shift|Meta>.", sequence.ToDisplayString());
        }
        finally
        {
            PlatformConventions.Current = previous;
        }
    }

    private sealed class SymbolConventions : PlatformConventions
    {
        public override ModifierKeys PrimaryModifier => ModifierKeys.Meta;

        public override string FormatGesture(KeyGesture gesture)
            => $"<{string.Join('|', Enum.GetValues<ModifierKeys>().Where(flag => flag != ModifierKeys.None && gesture.Modifiers.HasFlag(flag)))}>{FormatKey(gesture.Key)}";
    }
}
