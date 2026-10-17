using System;
using System.Collections.Generic;

namespace Workes.ConsoleSystem.Permissions;

/// <summary>
/// Provides mutable direct permission grants, explicit denials, and reusable role grants for one console caller.
/// </summary>
public sealed class ConsolePermissionSet : IConsolePermissionProvider
{
    private readonly HashSet<string> _grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _denials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConsoleRole> _roles = new Dictionary<string, ConsoleRole>(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new object();

    /// <summary>
    /// Grants a permission directly to this caller.
    /// </summary>
    /// <param name="permission">An exact permission or terminal dot-wildcard rule.</param>
    /// <returns>This permission set.</returns>
    public ConsolePermissionSet Allow(string permission)
    {
        lock (_gate)
        {
            _grants.Add(PermissionPath.ValidateRule(permission, nameof(permission)));
        }

        return this;
    }

    /// <summary>Removes a direct permission grant.</summary>
    public ConsolePermissionSet RemoveGrant(string permission)
    {
        lock (_gate)
        {
            _grants.Remove(PermissionPath.ValidateRule(permission, nameof(permission)));
        }

        return this;
    }

    /// <summary>
    /// Denies a permission directly to this caller. A matching denial overrides direct and role grants.
    /// </summary>
    /// <param name="permission">An exact permission or terminal dot-wildcard rule.</param>
    /// <returns>This permission set.</returns>
    public ConsolePermissionSet Deny(string permission)
    {
        lock (_gate)
        {
            _denials.Add(PermissionPath.ValidateRule(permission, nameof(permission)));
        }

        return this;
    }

    /// <summary>Removes an explicit permission denial.</summary>
    public ConsolePermissionSet RemoveDenial(string permission)
    {
        lock (_gate)
        {
            _denials.Remove(PermissionPath.ValidateRule(permission, nameof(permission)));
        }

        return this;
    }

    /// <summary>
    /// Adds the grants from a reusable role to this caller's permission set.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <returns>This permission set.</returns>
    public ConsolePermissionSet AddRole(ConsoleRole role)
    {
        if (role is null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        lock (_gate)
        {
            if (!_roles.TryAdd(role.Name, role))
            {
                throw new ArgumentException($"A role named '{role.Name}' has already been added.", nameof(role));
            }
        }

        return this;
    }

    /// <summary>Removes a role from this caller's permission set.</summary>
    public bool RemoveRole(string roleName)
    {
        string name = PermissionPath.ValidateName(roleName, nameof(roleName));
        lock (_gate)
        {
            return _roles.Remove(name);
        }
    }

    /// <inheritdoc />
    public bool HasPermission(string permission)
    {
        string requested = PermissionPath.ValidateName(permission, nameof(permission));
        lock (_gate)
        {
            if (PermissionPath.HasMatchingRule(_denials, requested))
            {
                return false;
            }

            if (PermissionPath.HasMatchingRule(_grants, requested))
            {
                return true;
            }

            foreach (ConsoleRole role in _roles.Values)
            {
                if (PermissionPath.HasMatchingRule(role.Permissions, requested))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
