using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Models;

namespace StudentRegistration.Api.Data
{
    public class StudentDBContext(DbContextOptions<StudentDBContext> options) : DbContext(options)
    {
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.StudentNumber).IsUnique();

                //prpoerty validation
                entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();

                //data seeding
                entity.HasData(
                        new Student { Id = 1, FirstName = "John", LastName = "Doe", StudentNumber = "ST01", Gender = "Male" },
                        new Student { Id = 2, FirstName = "Thabo", LastName = "Mokoena", StudentNumber = "ST02", Gender = "Male" },
                        new Student { Id = 3, FirstName = "Naledi", LastName = "Dhlamini", StudentNumber = "ST03", Gender = "Female" },
                        new Student { Id = 4, FirstName = "Tracy", LastName = "Ndlovu", StudentNumber = "ST04", Gender = "Female" },
                        new Student { Id = 5, FirstName = "Thato", LastName = "Moyo", StudentNumber = "ST05", Gender = "Male" }

                    );
            });
        }


    }
}
