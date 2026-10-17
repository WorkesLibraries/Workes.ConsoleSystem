using Workes.ConsoleSystem.Permissions;

namespace Workes.ConsoleSystem.Tests.Permissions;

public sealed class ConsolePermissionSetTests
{
    [Test]
    public void AllowAndDeny_MatchPermissionNamesCaseInsensitively_DenialWins()
    {
        var permissions = new ConsolePermissionSet()
            .Allow("admin.*")
            .Deny("ADMIN.BAN");

        Assert.That(permissions.HasPermission("admin.kick"), Is.True);
        Assert.That(permissions.HasPermission("Admin.Ban"), Is.False);
        Assert.That(permissions.HasPermission("admin"), Is.False);
    }

    [Test]
    public void AddRole_ComposesPermissionGrants()
    {
        var role = new ConsoleRole("moderator", "chat.mute", "chat.kick");
        var permissions = new ConsolePermissionSet().AddRole(role);

        Assert.That(permissions.HasPermission("chat.mute"), Is.True);
        Assert.That(permissions.HasPermission("chat.kick"), Is.True);
        Assert.That(permissions.HasPermission("chat.ban"), Is.False);
    }

    [Test]
    public void DirectDeny_OverridesRoleGrant()
    {
        var permissions = new ConsolePermissionSet()
            .AddRole(new ConsoleRole("admin", "admin.*"))
            .Deny("admin.delete");

        Assert.That(permissions.HasPermission("admin.read"), Is.True);
        Assert.That(permissions.HasPermission("admin.delete"), Is.False);
    }

    [Test]
    public void RemoveOperations_ChangeRuntimePermissionEvaluation()
    {
        var permissions = new ConsolePermissionSet()
            .Allow("chat.mute")
            .Allow("chat.kick")
            .Deny("chat.kick")
            .AddRole(new ConsoleRole("moderator", "chat.ban"));

        Assert.That(permissions.RemoveGrant("chat.mute"), Is.SameAs(permissions));
        Assert.That(permissions.RemoveDenial("chat.kick"), Is.SameAs(permissions));
        Assert.That(permissions.RemoveRole("MODERATOR"), Is.True);
        Assert.That(permissions.RemoveRole("missing"), Is.False);
        Assert.That(permissions.HasPermission("chat.mute"), Is.False);
        Assert.That(permissions.HasPermission("chat.kick"), Is.True);
        Assert.That(permissions.HasPermission("chat.ban"), Is.False);
    }

    [Test]
    public void AddRole_RejectsDuplicateNames()
    {
        var permissions = new ConsolePermissionSet().AddRole(new ConsoleRole("moderator", "chat.mute"));

        Assert.Throws<ArgumentException>(() => permissions.AddRole(new ConsoleRole("MODERATOR", "chat.kick")));
    }

    [TestCase("")]
    [TestCase("admin..kick")]
    [TestCase(".admin")]
    [TestCase("admin.")]
    [TestCase("admin.*.kick")]
    [TestCase("admin kick")]
    public void Allow_RejectsMalformedPermissionRules(string permission)
    {
        Assert.Throws<ArgumentException>(() => new ConsolePermissionSet().Allow(permission));
    }

    [Test]
    public void HasPermission_RejectsWildcardQueries()
    {
        Assert.Throws<ArgumentException>(() => new ConsolePermissionSet().HasPermission("admin.*"));
    }
}
