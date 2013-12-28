﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Data;
using Infrastructure.Data.Specification;
using Infrastructure.Tests.Data.Domain;
using Infrastructure.Tests.Data.Specification;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Infrastructure.Tests.Data
{
    [TestClass]
    public class WithoutStorageTest
    {
        private ICustomerRepository customerRepository;
        private GenericRepository genericRepository;

        private IRepository repository
        {
            get { return genericRepository; }
        }

        [TestInitialize]
        public void SetUp()
        {
            var builder = new DbContextBuilder<DbContext>("DefaultDb", new[] {"Infrastructure.Tests"}, true, true);
            DbContext context = ((IEFContextFactory<DbContext>) builder).Create();

            customerRepository = new CustomerRepository(context);
            genericRepository = new GenericRepository(context);
        }

        [TestCleanup]
        public void TearDown()
        {
            genericRepository.Close();
        }

        [TestMethod]
        public void Test()
        {
            DoAction(() => CreateCustomer());
            DoAction(() => CreateProducts());
            DoAction(() => AddOrders());
            DoAction(() => FindOneCustomer());
            DoAction(() => FindManyOrdersForJohnDoe());
            DoAction(() => FindNewlySubscribed());
            DoAction(() => FindOrderWithInclude());
            DoAction(() => FindBySpecification());
            DoAction(() => FindByCompositeSpecification());
            DoAction(() => FindByConcretSpecification());
            DoAction(() => FindByConcretCompositeSpecification());
            DoAction(() => FindCategoryWithInclude());
            DoAction(() => UpdateProduct());
        }

        private void FindBySpecification()
        {
            var specification = new Specification<Product>(p => p.Price < 100);
            IEnumerable<Product> productsOnSale = repository.Find(specification);
            Assert.AreEqual(2, productsOnSale.Count());
        }

        private void FindByCompositeSpecification()
        {
            IEnumerable<Product> products = repository.Find(
                new Specification<Product>(p => p.Price < 100).And(
                    new Specification<Product>(p => p.Name == "Windows XP Professional")));
            Assert.AreEqual(1, products.Count());
        }

        private void FindByConcretSpecification()
        {
            var specification = new ProductOnSaleSpecification();
            IEnumerable<Product> productsOnSale = repository.Find(specification);
            Assert.AreEqual(2, productsOnSale.Count());
        }

        private void FindByConcretCompositeSpecification()
        {
            IEnumerable<Product> products = repository.Find(
                new AndSpecification<Product>(
                    new ProductOnSaleSpecification(),
                    new ProductByNameSpecification("Windows XP Professional")));
            Assert.AreEqual(1, products.Count());
        }

        private void FindOrderWithInclude()
        {
            Customer c = customerRepository.FindByName("John", "Doe");
            List<Order> orders = repository.Find<Order>(x => x.Customer.Id == c.Id).ToList();
            Console.Write("Found {0} Orders with {1} OrderLines", orders.Count(), orders.ToList()[0].OrderLines.Count);
        }

        private void CreateProducts()
        {
            var osCategory = new Category {Name = "Operating System"};
            var msProductCategory = new Category {Name = "MS Product"};

            repository.Add(osCategory);
            repository.Add(msProductCategory);

            var p1 = new Product {Name = "Windows Seven Professional", Price = 100};
            p1.Categories.Add(osCategory);
            p1.Categories.Add(msProductCategory);
            repository.Add(p1);

            var p2 = new Product {Name = "Windows XP Professional", Price = 20};
            p2.Categories.Add(osCategory);
            p2.Categories.Add(msProductCategory);
            repository.Add(p2);

            var p3 = new Product {Name = "Windows Seven Home", Price = 80};
            p3.Categories.Add(osCategory);
            p3.Categories.Add(msProductCategory);
            repository.Add(p3);

            var p4 = new Product {Name = "Windows Seven Ultimate", Price = 110};
            p4.Categories.Add(osCategory);
            p4.Categories.Add(msProductCategory);
            repository.Add(p4);

            var p5 = new Product {Name = "Windows Seven Premium", Price = 150};
            p5.Categories.Add(osCategory);
            p5.Categories.Add(msProductCategory);
            repository.Add(p5);

            repository.UnitOfWork.SaveChanges();

            Console.Write("Saved five Products in 2 Category");
        }

        private void FindManyOrdersForJohnDoe()
        {
            Customer c = customerRepository.FindByName("John", "Doe");
            IEnumerable<Order> orders = repository.Find<Order>(x => x.Customer.Id == c.Id);

            Console.Write("Found {0} Orders with {1} OrderLines", orders.Count(), orders.ToList()[0].OrderLines.Count);
        }

        private void FindNewlySubscribed()
        {
            IList<Customer> newCustomers = customerRepository.NewlySubscribed();

            Console.Write("Found {0} new customers", newCustomers.Count);
        }

        private void AddOrders()
        {
            Customer c = customerRepository.FindByName("John", "Doe");

            var winXP = repository.FindOne<Product>(x => x.Name == "Windows XP Professional");
            var winSeven = repository.FindOne<Product>(x => x.Name == "Windows Seven Professional");

            var o = new Order
            {
                OrderDate = DateTime.Now,
                Customer = c,
                OrderLines = new List<OrderLine>
                {
                    new OrderLine {Price = 200, Product = winXP, Quantity = 1},
                    new OrderLine {Price = 699.99, Product = winSeven, Quantity = 5}
                }
            };

            repository.Add(o);
            repository.UnitOfWork.SaveChanges();
            Console.Write("Saved one order");
        }

        private void CreateCustomer()
        {
            customerRepository.UnitOfWork.BeginTransaction();

            var c = new Customer {Firstname = "John", Lastname = "Doe", Inserted = DateTime.Now};
            customerRepository.Add(c);

            customerRepository.UnitOfWork.CommitTransaction();
        }

        private void FindOneCustomer()
        {
            var c = repository.FindOne<Customer>(x => x.Firstname == "John" &&
                                                      x.Lastname == "Doe");

            Console.Write("Found Customer: {0} {1}", c.Firstname, c.Lastname);
        }

        private void GetProductsWithPaging()
        {
            List<Product> output = repository.Get<Product, string>(x => x.Name, 0, 5).ToList();
            Assert.IsTrue(output[0].Name == "Windows Seven Home");
            Assert.IsTrue(output[1].Name == "Windows Seven Premium");
            Assert.IsTrue(output[2].Name == "Windows Seven Professional");
            Assert.IsTrue(output[3].Name == "Windows Seven Ultimate");
            Assert.IsTrue(output[4].Name == "Windows XP Professional");
        }

        private void FindCategoryWithInclude()
        {
            Category category =
                repository.GetQuery<Category>(x => x.Name == "Operating System")
                    .Include(c => c.Products)
                    .SingleOrDefault();
            Assert.IsNotNull(category);
            Assert.IsTrue(category.Products.Count > 0);
        }

        private void UpdateProduct()
        {
            var output = repository.FindOne<Product>(x => x.Name == "Windows XP Professional");
            Assert.IsNotNull(output);

            output.Name = "Windows XP Home";
            repository.Update(output);
            repository.UnitOfWork.SaveChanges();

            var updated = repository.FindOne<Product>(x => x.Name == "Windows XP Home");
            Assert.IsNotNull(updated);
        }

        private static void DoAction(Expression<Action> action)
        {
            Console.Write("Executing {0} ... ", action.Body);

            Action act = action.Compile();
            act.Invoke();

            Console.WriteLine();
        }
    }
}