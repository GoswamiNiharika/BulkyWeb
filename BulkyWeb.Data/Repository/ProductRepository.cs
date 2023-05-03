using BulkyWeb.DataAccess.Data;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyWeb.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(Product product)
        {
            var productExist = _context.Products.FirstOrDefault(u => u.Id == product.Id);
            if (productExist != null) 
            {
                productExist.Title = product.Title;
                productExist.Description = product.Description;
                productExist.CategoryId = product.CategoryId;
                productExist.ListPrice = product.ListPrice;
                productExist.Price = product.Price;
                productExist.Price50 = product.Price50;
                productExist.Price100 = product.Price100;
                productExist.Author = product.Author;
                productExist.ISBN = product.ISBN;
                if(product.ImageUrl!= null) 
                {
                    productExist.ImageUrl = product.ImageUrl;
                }
            }
        }
    }
}
