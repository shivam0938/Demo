using Demo.Data;
using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace Demo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        protected readonly ApplicationDbContext _context;
        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string sortOrder,  int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
            ViewData["CategorySortParm"] = sortOrder == "Category" ? "cat_desc" : "Category";
            ViewData["StockSortParm"] = sortOrder == "Stock" ? "Stock_desc" : "Stock";
            ViewData["DescriptionSortParm"] = sortOrder == "Description" ? "Description_desc" : "Description";

            var nameClaim = User.FindFirst(ClaimTypes.Name);
            string name = nameClaim?.Value;
            Guid customerId = Guid.NewGuid();
            if(!string.IsNullOrEmpty(name))
            {
                customerId = new Guid(name);
            }

            var products = from p in _context.Products.Include(p => p.Images).Where(p=> p.CustomerId == customerId)
                           select p;

           
            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "Price" => products.OrderBy(p => (double)p.Price),
                "price_desc" => products.OrderByDescending(p => (double)p.Price),
                "Category" => products.OrderBy(p => p.Category),
                "cat_desc" => products.OrderByDescending(p => p.Category),
                "Description" => products.OrderBy(p => p.Description),
                "Description_desc" => products.OrderByDescending(p => p.Description),
                "Stock" => products.OrderBy(p => (double)p.StockQuantity),
                "Stock_desc" => products.OrderByDescending(p => (double)p.StockQuantity),
                _ => products.OrderBy(p => p.Name),
            };

            int pageSize = 3;
            return View(await PaginatedList<Product>.CreateAsync(products.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public ActionResult Create()
        {
            return View(new Product());
        }
          
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,StockQuantity,Category")] Product product, List<IFormFile> images)
        {
            if (images == null || !images.Any(f => f != null && f.Length > 0))
            {
                ModelState.AddModelError("Images", "Please upload at least one image.");
            }
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            long maxFileSize = 10 * 1024 * 1024; // 10 MB max per file

            foreach (var image in images)
            {
                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("Images", $"File {image.FileName} has unsupported extension(not valid images).");
                }

                if (image.Length > maxFileSize)
                {
                    ModelState.AddModelError("Images", $"File {image.FileName} exceeds max allowed size of 10MB.");
                }

            }

            if (ModelState.IsValid)
            {
                var nameClaim = User.FindFirst(ClaimTypes.Name);
                string name = nameClaim?.Value;
                product.CustomerId = new Guid(name);
                _context.Add(product);
                await _context.SaveChangesAsync();

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        string uniqueFileName = GenerateUniqueFileName(image.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ImageUrl = uniqueFileName,
                            ProductId = product.ProductId
                        };

                        _context.Add(productImage);
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,StockQuantity,Category")] Product product, List<IFormFile> images)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            long maxFileSize = 10 * 1024 * 1024; // 10 MB max per file
            if(images != null)
            {
                foreach (var image in images)
                {
                    var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("Images", $"File {image.FileName} has unsupported extension.");
                    }

                    if (image.Length > maxFileSize)
                    {
                        ModelState.AddModelError("Images", $"File {image.FileName} exceeds max allowed size of 10MB.");
                    }

                }
            }
            

            // Fetch existing product with images
            var existingProduct = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);


            if (existingProduct == null)
            {
                return NotFound();
            }

            if ((existingProduct.Images == null || !existingProduct.Images.Any()) && (images == null || !images.Any(f => f.Length > 0)))
            {
                ModelState.AddModelError("Images", "Please upload at least one image.");
            }

            if (ModelState.IsValid)
            {
                // Update fields
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.Category = product.Category;

                // Handle uploaded images
                if (images != null && images.Any(f => f.Length > 0))
                {
                    foreach (var image in images)
                    {
                        string uniqueFileName = GenerateUniqueFileName(image.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        var newImage = new ProductImage
                        {
                            ImageUrl = uniqueFileName,
                            ProductId = existingProduct.ProductId
                        };

                        _context.ProductImages.Add(newImage);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(existingProduct);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
         .Include(p => p.Images)
         .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                // Delete image files from disk
                foreach (var image in product.Images)
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.ImageUrl);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Remove image records
                _context.ProductImages.RemoveRange(product.Images);

                // Remove the product
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            string extension = Path.GetExtension(originalFileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"); // Format: 20250615_133143_123
            string uniqueFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_{timestamp}{extension}";
            return uniqueFileName;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>  DeleteImage(int? id)
        {
            if(id == null)
                return Json(new { success = false });

            var image = await _context.ProductImages.FirstOrDefaultAsync(x => x.ProductImageId == id);
            if (image != null)
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.ImageUrl);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
                _context.ProductImages.Remove(image);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }
    }

}
