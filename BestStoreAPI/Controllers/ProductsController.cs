﻿using BestStoreAPI.Models;
using BestStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace BestStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        private readonly List<string> listCategories = new List<string>()
        {
            "Phones", "Computers", "Accessories", "Printers", "Cameras", "Other"
        };

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }

        [HttpGet("categories")]
        public IActionResult GetCategories() 
        {
            return Ok(listCategories);
        }

        [HttpGet]
        public IActionResult GetProducts(string? search, string? category,
            int? minPrice, int? maxPrice, string? sort, string? order,
            int? page)
        {
            IQueryable<Product> query = context.Products;

            // search functionality
            if (search != null)
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            //search by category
            if (category != null)
            {
                query = query.Where(p => p.Category == category);
            }

            //search by price min
            if (minPrice != null)
            {
                query = query.Where(p => p.Price >= minPrice);
            }

            //search by price max
            if (maxPrice != null)
            {
                query = query.Where(p => p.Price <= maxPrice);
            }


            //  sort functionality

            if (sort == null)
                sort = "id"; // jesli jest  puste to sortoanie bedzie zawsze po id

            if (order == null || order != "asc")
                order = "desc";

            if (sort.ToLower() == "name")  // po nazwie
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Name);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Name);
                }
            }

            else if (sort.ToLower() == "brand")  // po firmie
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Brand);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Brand);
                }
            }

            else if (sort.ToLower() == "category")  // po kategorii
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Category);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Category);
                }
            }

            else if (sort.ToLower() == "price")  // po cenie
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Price);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }

            else if (sort.ToLower() == "createdat")  // po dacie
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
            }

            else // inaczej
            { 
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Id);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }
            }

            //pagination functionality

            if (page == null || page < 1)
                page = 1;

            int pageSize = 5;
            int totalPages = 0;

            decimal productNumber = query.Count();
            totalPages = (int)Math.Ceiling(productNumber / pageSize);

            query = query.Skip((int)(page - 1) * pageSize).Take(pageSize);

            var products = query.ToList();

            var response = new
            {
                Products = products,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = context.Products.Find(id);

            if(product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult CreateProduct([FromForm ]ProductDto productDto)
        {
            //checing if the category is valid, czyli czy jest na liście
            if (!listCategories.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please select a valid category");
                return BadRequest(ModelState);
            }

            if(productDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The ImageFile is required");
                return BadRequest(ModelState);
            }

            // save the image on the server
            string imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

            string imagesFolderPath = env.WebRootPath + "/images/products/";

            Console.WriteLine(imagesFolderPath);
            Console.WriteLine(imagesFolderPath);
            Console.WriteLine(imagesFolderPath);
            Console.WriteLine(imagesFolderPath);
            Console.WriteLine(imagesFolderPath);
            Console.WriteLine(imagesFolderPath);

            using (var stream = System.IO.File.Create(imagesFolderPath + imageFileName))
            {
                productDto.ImageFile.CopyTo(stream); 
            }

            // save product in the database
            Product product = new Product()
            {
                Name = productDto.Name,
                Brand = productDto.Brand,
                Category = productDto.Category,
                Price = productDto.Price,
                Description = productDto.Description ?? "",
                ImageFileName = imageFileName,
                CreatedAt = DateTime.Now,   
            };

            context.Products.Add(product);
            context.SaveChanges();

            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, [FromForm] ProductDto productDto)
        {
            //checing if the category is valid, czyli czy jest na liście
            if (!listCategories.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please select a valid category");
                return BadRequest(ModelState);
            }

            var product = context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            string imageFileNameStary = product.ImageFileName;
            if(productDto.ImageFile != null) // jeśli mamy nowy obraz w productDto to znaczy że jest nowe zdjęce do updata
            {
                // save the new image on the server and delete the old one from server
                imageFileNameStary = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                imageFileNameStary += Path.GetExtension(productDto.ImageFile.FileName);

                string imagesFolderPath = env.WebRootPath + "/images/products/";
                using (var stream = System.IO.File.Create(imagesFolderPath + imageFileNameStary))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                // delete the old file
                System.IO.File.Delete(imagesFolderPath + product.ImageFileName);
            }

            // update product in the database, wczesniej tylko pliki i obrazy były edytowane

            product.Name = productDto.Name;
            product.Brand = productDto.Brand;
            product.Category = productDto.Category;
            product.Price = productDto.Price;
            product.Description = productDto.Description ?? "";
            product.ImageFileName = imageFileNameStary;

            context.SaveChanges();

            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = context.Products.Find(id);

            if (product == null) 
            { 
                return NotFound(); 
            }

            // delete product image z plików servera
            string imagesFolderPath = env.WebRootPath + "/images/products/";
            System.IO.File.Delete(imagesFolderPath + product.ImageFileName);

            //delete product from database
            context.Products.Remove(product);
            context.SaveChanges();

            return Ok();
        }
    }
}
