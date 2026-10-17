namespace Workes.ConsoleSystem.Permissions;

/// <summary>
/// Determines whether the caller associated with a console has a named permission.
/// </summary>
public interface IConsolePermissionProvider
{
    /// <summary>
    /// Determines whether the caller has the specified permission.
    /// </summary>
    /// <param name="permission">The required permission.</param>
    /// <returns><see langword="true"/> when the permission is granted.</returns>
    bool HasPermission(string permission);
}
