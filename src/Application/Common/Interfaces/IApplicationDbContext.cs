using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Department> Departments { get; }
        DbSet<DocumentStation> Stations { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
