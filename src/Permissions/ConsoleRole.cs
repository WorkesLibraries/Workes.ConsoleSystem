using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ConsoleSystem.Permissions;

/// <summary>
/// A named immutable collection of permission grants that can be added to a permission set.
/// </summary>
public sealed class ConsoleRole
{
    /// <summary>
    /// Initializes a role with the specified permission grants.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">The permissions granted by this role.</param>
    public ConsoleRole(string name, params string[] permissions)
    {
        Name = PermissionPath.ValidateName(name, nameof(name));
        if (permissions is null)
        {
            throw new ArgumentNullException(nameof(permissions));
        }

        var copy = new List<string>(permissions.Length);
        foreach (string permission in permissions)
        {
            copy.Add(PermissionPath.ValidateRule(permission, nameof(permissions)));
        }

        Permissions = copy.AsReadOnly();
    }

    /// <summary>
    /// Gets the role name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the permission grants in this role.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; }
}
