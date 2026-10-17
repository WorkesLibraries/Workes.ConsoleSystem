using System;
using System.Collections.Generic;
using Workes.ConsoleSystem.Presentation;

namespace Workes.ConsoleSystem.Tests.Presentation;

public sealed class ConsoleTextTests
{
    [Test]
    public void Plain_PreservesLiteralMarkupLikeText()
    {
        ConsoleText text = ConsoleText.Plain("<b>literal</b>", "Information");

        Assert.That(text.PlainText, Is.EqualTo("<b>literal</b>"));
        Assert.That(text.DefaultSemanticId, Is.EqualTo("Information"));
        Assert.That(text.Segments, Has.Count.EqualTo(1));
        Assert.That(text.ResolveSemanticId(text.Segments[0]), Is.EqualTo("Information"));
    }

    [Test]
    public void Build_CreatesOrderedSegmentsWithSemanticIdsDataAndPlainText()
    {
        var player = new { Id = "player-1" };
        ConsoleText text = ConsoleText.Build(defaultSemanticId: "Success")
            .Text("Gave ")
            .Value("Workes", semanticId: "Player", data: player)
            .Text(" 20 ")
            .Value("wood", semanticId: "Item", data: "wood")
            .Text(".")
            .Build();

        Assert.That(text.PlainText, Is.EqualTo("Gave Workes 20 wood."));
        Assert.That(text.DefaultSemanticId, Is.EqualTo("Success"));
        Assert.That(text.Segments, Has.Count.EqualTo(5));
        Assert.That(text.Segments[1].SemanticId, Is.EqualTo("Player"));
        Assert.That(text.Segments[1].Data, Is.SameAs(player));
        Assert.That(text.Segments[3].SemanticId, Is.EqualTo("Item"));
        Assert.That(text.ResolveSemanticId(text.Segments[0]), Is.EqualTo("Success"));
        Assert.That(text.ResolveSemanticId(text.Segments[1]), Is.EqualTo("Player"));
    }

    [Test]
    public void Build_CanCreateEmptyText()
    {
        ConsoleText text = ConsoleText.Build().Build();

        Assert.That(text.PlainText, Is.Empty);
        Assert.That(text.Segments, Is.Empty);
    }

    [Test]
    public void Segments_AreReadOnly()
    {
        ConsoleText text = ConsoleText.Build().Text("first").Build();

        Assert.That(((IList<ConsoleTextSegment>)text.Segments).IsReadOnly, Is.True);
    }

    [Test]
    public void ResolveSemanticId_NullSegmentThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ConsoleText.Plain("text").ResolveSemanticId(null!));
    }

    [Test]
    public void NullTextThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ConsoleText.Plain(null!));
        Assert.Throws<ArgumentNullException>(() => ConsoleText.Build().Text(null!));
        Assert.Throws<ArgumentNullException>(() => ConsoleText.Build().Value(null!));
        Assert.Throws<ArgumentNullException>(() => new ConsoleTextSegment(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void BlankSemanticIdsThrow(string semanticId)
    {
        Assert.Throws<ArgumentException>(() => ConsoleText.Plain("x", semanticId));
        Assert.Throws<ArgumentException>(() => ConsoleText.Build(semanticId));
        Assert.Throws<ArgumentException>(() => new ConsoleTextSegment("x", semanticId));
    }
}
