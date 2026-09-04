using StudentRegistration.Api.Models;

namespace StudentRegistration.Api.Services
{
    public interface IStudentService
    {
        Task<List<Student>> GetStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<List<Student>> FindAsync(string search);
        Task<Student> AddStudentAsync(Student student);
        Task<Student?> UpdateStudentAsync(int id, Student updatedStudent);
        Task<Student?> DeleteStudentAsync(int id);
    }
}
