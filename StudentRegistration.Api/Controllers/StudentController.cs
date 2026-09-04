using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Api.Models;
using StudentRegistration.Api.Services;

namespace StudentRegistration.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Student>>> GetStudents()
        {
            var students = await _studentService.GetStudentsAsync();
            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudentById(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student is null)
                return NotFound($"Student with id {id} was not found");

            return Ok(student);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<Student>>> Find(string search)
        {
            var results = await _studentService.FindAsync(search);
            if (results.Any())
                return Ok(results);
            return NotFound($"No records matching {search}");
        }

        [HttpPost]
        public async Task<ActionResult<Student>> AddStudent(Student student)
        {
            var createdStudent = await _studentService.AddStudentAsync(student);
            return Ok(createdStudent);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Student>> UpdateStudent(int id, Student updatedStudent)
        {
            var student = await _studentService.UpdateStudentAsync(id, updatedStudent);
            if (student is null)
                return NotFound($"Student with id {id} was not found");

            return Ok(student);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<Student>> DeleteStudent(int id)
        {
            var student = await _studentService.DeleteStudentAsync(id);
            if (student is null)
                return NotFound($"Student with id {id} was not found");

            return Ok(student);
        }

    }
}
