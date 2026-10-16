using System;
using System.Collections.Generic;
using System.Text;
using StudentRegistration.Api.Models;
using StudentRegistration.Api.Services;
using StudentRegistration.Api.Controllers;
using Moq;
using Microsoft.AspNetCore.Mvc;

namespace StudentRegistration.Api.Tests
{
    public class StudentControllerTests
    {

        [Fact]
        public async Task GetStudents_ReturnsOkWithStudents()
        {
            //Arrange
            var students = new List<Student>
            {
                new Student {Id = 1, FirstName = "John", LastName ="Doe", StudentNumber = "ST01"},
                new Student {Id = 2, FirstName = "Jane", LastName ="Morgans", StudentNumber = "ST02"},
            };
            var service = new Mock<IStudentService>();
            service.Setup(service => service.GetStudentsAsync()).ReturnsAsync(students);
            var controller = new StudentController(service.Object);

            //Act
            var result = await controller.GetStudents();

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(students, okResult.Value);
            service.Verify(x => x.GetStudentsAsync(), Times.Once);

        }

        [Fact]
        public async Task SearchStudents_NoResultsReturnsNotFound()
        {
            //Arrange
            var searchTerm = "ST9000";
            var service = new Mock<IStudentService>();
            service.Setup(service => service.FindAsync(searchTerm)).ReturnsAsync(new List<Student>());
            var controller = new StudentController(service.Object);

            //Act
            var result = await controller.Find(searchTerm);

            //Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No records matching {searchTerm}", notFoundResult.Value);
            service.Verify(x => x.FindAsync(searchTerm), Times.Once);

        }

        [Fact]
        public async Task AddStudent_ReturnsOkWithStudent()
        {
            //Arrange
            var student = new Student { Id = 5, FirstName = "John", LastName = "Doe" };
            var service = new Mock<IStudentService>();
            service.Setup(service => service.AddStudentAsync(student)).ReturnsAsync(student);
            var controller = new StudentController(service.Object);

            //Act
            var result = await controller.AddStudent(student);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(student, okResult.Value);
            service.Verify(x => x.AddStudentAsync(student), Times.Once);
        }


        [Fact]
        public async Task GetStudentById_ReturnsOkWithExistingStudent()
        {
            var student = new Student { Id = 5, FirstName = "John", LastName = "Doe" };
            var service = new Mock<IStudentService>();
            service.Setup(service => service.GetStudentByIdAsync(5)).ReturnsAsync(student);
            var controller = new StudentController(service.Object);


            //Act
            var result = await controller.GetStudentById(5);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(student, okResult.Value);
            service.Verify(x => x.GetStudentByIdAsync(5), Times.Once);
        }
        [Fact]
        public async Task DeleteStudent_ExistingStudentReturnsAccepted()
        {
            var service = new Mock<IStudentService>();
            service.Setup(service => service.DeleteStudentAsync(5)).ReturnsAsync(true);
            var controller = new StudentController(service.Object);

            //Act
            var result = await controller.DeleteStudent(5);

            //Assert
            Assert.IsType<AcceptedResult>(result);
         }

        [Fact]
        public async Task DeleteStudent_NonExistingStudentReturnsNotFound()
        {
            var service = new Mock<IStudentService>();
            service.Setup(service => service.DeleteStudentAsync(5)).ReturnsAsync(false);
            var controller = new StudentController(service.Object);

            //Act
            var result = await controller.DeleteStudent(5);

            //Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }


    }
}
