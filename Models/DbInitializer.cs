using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Helpers;

namespace StudentPartTime.Models;

public static class DbInitializer
{
    public static async Task InitializeAsync(StudentPartTimeJobDbContext context)
    {
        // Automatically apply database migrations if using SQL Server
        if (context.Database.IsSqlServer())
        {
            await context.Database.MigrateAsync();
        }

        // Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin", Description = "Administrator" },
                new Role { RoleId = 2, RoleName = "Employer", Description = "Recruiter / Employer" },
                new Role { RoleId = 3, RoleName = "Student", Description = "Job Seeker / Student" }
            });
            await context.SaveChangesAsync();
        }

        // Seed Provinces
        if (!await context.Provinces.AnyAsync())
        {
            context.Provinces.AddRange(new List<Province>
            {
                new Province { ProvinceName = "Hà Nội", CreatedAt = DateTime.Now },
                new Province { ProvinceName = "Hồ Chí Minh", CreatedAt = DateTime.Now },
                new Province { ProvinceName = "Đà Nẵng", CreatedAt = DateTime.Now },
                new Province { ProvinceName = "Cần Thơ", CreatedAt = DateTime.Now },
                new Province { ProvinceName = "Hải Phòng", CreatedAt = DateTime.Now }
            });
            await context.SaveChangesAsync();
        }

        // Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(new List<Category>
            {
                new Category { CategoryName = "Phục vụ / Pha chế", Description = "Waiter, Bartender, Barista", IsActive = true, CreatedAt = DateTime.Now },
                new Category { CategoryName = "Bán hàng / Thu ngân", Description = "Salesperson, Cashier", IsActive = true, CreatedAt = DateTime.Now },
                new Category { CategoryName = "Giao hàng / Shipper", Description = "Delivery, Shipper", IsActive = true, CreatedAt = DateTime.Now },
                new Category { CategoryName = "Gia sư / Dạy kèm", Description = "Tutor, Teacher", IsActive = true, CreatedAt = DateTime.Now },
                new Category { CategoryName = "Công nghệ / IT", Description = "Software, Website, IT Support", IsActive = true, CreatedAt = DateTime.Now },
                new Category { CategoryName = "Khác", Description = "Other part-time jobs", IsActive = true, CreatedAt = DateTime.Now }
            });
            await context.SaveChangesAsync();
        }

        // Seed Job Types
        if (!await context.JobTypes.AnyAsync())
        {
            context.JobTypes.AddRange(new List<JobType>
            {
                new JobType { TypeName = "Bán thời gian (Part-time)", Description = "Part-time job working shift-based", IsActive = true, CreatedAt = DateTime.Now },
                new JobType { TypeName = "Thực tập (Internship)", Description = "Internship for students", IsActive = true, CreatedAt = DateTime.Now },
                new JobType { TypeName = "Làm việc tự do (Freelance)", Description = "Project-based or freelance", IsActive = true, CreatedAt = DateTime.Now }
            });
            await context.SaveChangesAsync();
        }

        // Seed default Admin User if not exist
        if (!await context.Users.AnyAsync(u => u.Email == "admin@studentjob.com"))
        {
            var adminUser = new User
            {
                Email = "admin@studentjob.com",
                FullName = "System Administrator",
                PasswordHash = SecurityHelper.HashPassword("Admin@123"),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var adminRole = await context.Roles.FindAsync(1);
            if (adminRole != null)
            {
                adminUser.Roles.Add(adminRole);
            }

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
