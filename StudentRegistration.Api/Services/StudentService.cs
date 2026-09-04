using StudentRegistration.Api.Models;

namespace StudentRegistration.Api.Services
{
    public class StudentService : IStudentService
    {
        private readonly List<Student> _students =
        [
            new Student { Id = 1, FirstName = "John", LastName = "Doe", StudentNumber = "ST01", Gender = "Male" },
            new Student { Id = 2, FirstName = "Thabo", LastName = "Mokoena", StudentNumber = "ST02", Gender = "Male" },
            new Student { Id = 3, FirstName = "Naledi", LastName = "Dhlamini", StudentNumber = "ST03", Gender = "Female" },
            new Student { Id = 4, FirstName = "Tracy", LastName = "Ndlovu", StudentNumber = "ST04", Gender = "Female" },
            new Student { Id = 5, FirstName = "Thato", LastName = "Moyo", StudentNumber = "ST05", Gender = "Male" }
        ];
        private readonly object _lock = new();

        public Task<List<Student>> GetStudentsAsync()
        {
            lock (_lock)
            return Task.FromResult(_students.ToList());
        }

        public Task<Student?> GetStudentByIdAsync(int id)
        {
            lock (_lock)
            return Task.FromResult(_students.FirstOrDefault(x => x.Id == id));
        }

        public Task<List<Student>> FindAsync(string search)
        {
            lock (_lock)
            return Task.FromResult(_students.Where(x => x.StudentNumber.Contains(search) ||
                    x.FirstName.Contains(search) ||
                    x.LastName.Contains(search) ||
                    x.Gender.Contains(search)).ToList());
        }

        public Task<Student> AddStudentAsync(Student student)
        {
            lock (_lock)
            {
                student.Id = _students.Count == 0 ? 1 : _students.Max(x => x.Id) + 1;
                _students.Add(student);
                return Task.FromResult(student);
            }
        }

        public Task<Student?> UpdateStudentAsync(int id, Student updatedStudent)
        {
            lock (_lock)
            {
                var existingStudent = _students.FirstOrDefault(x => x.Id == id);
                if (existingStudent is null)
                    return Task.FromResult<Student?>(null);

                existingStudent.StudentNumber = updatedStudent.StudentNumber;
                existingStudent.FirstName = updatedStudent.FirstName;
                existingStudent.LastName = updatedStudent.LastName;
                existingStudent.Gender = updatedStudent.Gender;

                return Task.FromResult<Student?>(existingStudent);
            }
        }

        public Task<Student?> DeleteStudentAsync(int id)
        {
            lock (_lock)
            {
                var existingStudent = _students.FirstOrDefault(x => x.Id == id);
                if (existingStudent is null)
                    return Task.FromResult<Student?>(null);

                _students.Remove(existingStudent);
                return Task.FromResult<Student?>(existingStudent);
            }
        }
    }
}
