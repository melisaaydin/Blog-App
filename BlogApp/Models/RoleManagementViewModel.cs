using System.Collections.Generic;

namespace BlogApp.Models
{
    public class RoleManagementViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public List<SelectableRole> Roles { get; set; } = new();
    }

    public class SelectableRole
    {
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}