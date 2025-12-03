using Cinemax.Controllers;
using Cinemax.Data;
using Cinemax.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaxTest;

[TestClass]
public class MovieControllerTests
{
    private ApplicationDbContext _context;
    MovieController _controller;

    [TestInitialize]
    public void TestInitialize()
    {
        //This sets up an in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        //populate the in-memory database with test data
        var genres = new List<Genre>
        {
            new Genre { GenreId = 1, Name = "Action" },
            new Genre { GenreId = 2, Name = "Drama" },
            new Genre { GenreId = 3, Name = "Comedy" }
        };
        _context.Genres.AddRange(genres);

        var movies = new List<Movie>
        {
            new Movie
            {
                MovieId = 100,
                MovieTitle = "Test Movie 1",
                Director = "Director 1",
                ReleaseYear = 2020,
                Duration = 120,
                GenreId = 1
            },
            new Movie
            {
                MovieId = 200,
                MovieTitle = "Test Movie 2",
                Director = "Director 2",
                ReleaseYear = 2021,
                Duration = 110,
                GenreId = 2
            },
            new Movie
            {
                MovieId = 300,
                MovieTitle = "Test Movie 3",
                Director = "Director 3",
                ReleaseYear = 2022,
                Duration = 130,
                GenreId = 3
            }
        };
        _context.Movies.AddRange(movies);
        _context.SaveChanges();

        _controller = new MovieController(_context);
    }

    [TestMethod]
    //method to test Index method
    public void IndexReturnsView()
    {
        var result = (ViewResult)_controller.Index();
        Assert.AreEqual("Index", result.ViewName);
    }

    [TestMethod]
    //method to test Create GET method
    public void CreateGetReturnsCreateView()
    {
        var result = (ViewResult)_controller.Create();
        Assert.AreEqual("Create", result.ViewName);
    }

    [TestMethod]
    //method to test Create POST with valid model
    public void CreatePostValidModelRedirectsToIndex()
    {
        var movie = new Movie
        {
            MovieTitle = "New Movie",
            Director = "New Director",
            ReleaseYear = 2023,
            Duration = 100,
            GenreId = 1
        };

        var result = (RedirectToActionResult)_controller.Create(movie);
        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    //method to test Create POST with invalid model
    public void CreatePostInvalidModelReturnsCreateView()
    {
        var movie = new Movie();
        _controller.ModelState.AddModelError("MovieTitle", "Movie title is required");

        var result = (ViewResult)_controller.Create(movie);
        Assert.AreEqual("Create", result.ViewName);
    }

    [TestMethod]
    //method to test Edit GET with invalid id
    public void EditGetInvalidIdReturnsNotFound()
    {
        var result = _controller.Edit(-1);
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    //method to test Edit GET with valid id
    public void EditGetValidIdReturnsEditView()
    {
        var result = (ViewResult)_controller.Edit(100);
        Assert.AreEqual("Edit", result.ViewName);
    }

    [TestMethod]
    //method to test Edit POST with valid model
    public void EditPostValidModelRedirectsToIndex()
    {
        var movie = _context.Movies.Find(100);
        movie.MovieTitle = "Updated Title";

        var result = (RedirectToActionResult)_controller.Edit(100, movie);
        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    //method to test Edit POST with invalid model
    public void EditPostInvalidModelReturnsEditView()
    {
        var movie = _context.Movies.Find(100);
        _controller.ModelState.AddModelError("MovieTitle", "Movie title is required");

        var result = (ViewResult)_controller.Edit(100, movie);
        Assert.AreEqual("Edit", result.ViewName);
    }
}