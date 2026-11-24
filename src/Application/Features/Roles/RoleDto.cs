using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Roles
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserCount { get; set; }
    }
}
