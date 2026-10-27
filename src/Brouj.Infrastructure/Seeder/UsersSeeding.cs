using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Domain.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brouj.Infrastructure.Seeder
{
    public static class UsersSeeding
    {
        public static async Task SeedingAsync(IApplicationDbContext context, IPasswordHasher hasher, IHttpContextAccessor _httpContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(hasher);
            ArgumentNullException.ThrowIfNull(_httpContext);

            if (!context.Users.Any())
            {
                User user = new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Mazen",
                    Email = "mazenkhtab11@gmail.com",
                    HashPassword = hasher.Hash("Mak.12"),
                    Phone = "01023839637",
                    Role = UserRole.SuperAdmin,
                };

                context.Users.Add(user);
            }

            await context.SaveChangesAsync(default);
        }
    }
}
