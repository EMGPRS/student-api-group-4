using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Web.Models;
using System.Diagnostics;

namespace StudentRegistration.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("StudentApi");
        }
        public async Task<IActionResult> Index(int? Id)
        {
            var students = await _httpClient.GetFromJsonAsync<List<Student>>("api/student");
            StudentViewModel viewModel = new StudentViewModel();
            viewModel.Students = students;
            if (Id.HasValue)
            {
                viewModel.Student = students.FirstOrDefault(x => x.Id == Id.Value);
            }
            else
            {
                viewModel.Student = new Student();
            }
            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Save(Student student)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            if(student.Id == 0)
            {
                await _httpClient.PostAsJsonAsync("api/student", student);
            }
            else
            {
                await _httpClient.PutAsJsonAsync($"api/student{student.Id}", student);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _httpClient.DeleteAsync($"api/student{id}");
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
