using Microsoft.EntityFrameworkCore;
using StudentRegistration.Api.Data;
using StudentRegistration.Api.Models;

namespace StudentRegistration.Api.Services
{
    public class StudentService : IStudentService
    {
        private readonly StudentDBContext _context;

        public StudentService(StudentDBContext context)
        {
            _context = context;
        }

        public Task<List<Student>> GetStudentsAsync()
        {
            return _context.Students.OrderBy(x => x.Id).ToListAsync();
        }

        public Task<Student?> GetStudentByIdAsync(int id)
        {
            return _context.Students
                 .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<List<Student>> FindAsync(string search)
        {

            return _context.Students.Where(x => x.StudentNumber.Contains(search) ||
                    x.FirstName.Contains(search) ||
                    x.LastName.Contains(search) ||
                    x.Gender.Contains(search)).ToListAsync();
        }

        public async Task<Student> AddStudentAsync(Student student)
        {
            student.Id = 0;
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<Student?> UpdateStudentAsync(int id, Student updatedStudent)
        {

            var existingStudent = _context.Students.FirstOrDefault(x => x.Id == id);
            if (existingStudent is null)
                return null;

            existingStudent.StudentNumber = updatedStudent.StudentNumber;
            existingStudent.FirstName = updatedStudent.FirstName;
            existingStudent.LastName = updatedStudent.LastName;
            existingStudent.Gender = updatedStudent.Gender;

            await _context.SaveChangesAsync();
            return existingStudent;

        }

        public async Task<bool> DeleteStudentAsync(int id)
        {

            var existingStudent = _context.Students.FirstOrDefault(x => x.Id == id);
            if (existingStudent is null)
                return false;

            _context.Students.Remove(existingStudent);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
