﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Tests.Data.Domain;
using Infrastructure.Data.Specification;
using Infrastructure.Tests.Data.Specification;
using Infrastructure.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Infrastructure.Tests.Data
{
    [TestClass]
    public class UseMyDbContextTest
    {
        private ICustomerRepository customerRepository;
        private IRepository repository { get { return genericRepository; }}
        private GenericRepository genericRepository;
        private MyDbContext context;

        [TestInitialize]
        public void SetUp()
        {
            Database.SetInitializer(new DataSeedingInitializer());
            context = new MyDbContext("DefaultDb");
            
            customerRepository = new CustomerRepository(context);
            genericRepository = new GenericRepository(context);
        }

        [TestMethod]
        public void GenerateDatabaseScriptTest()
        {
            string script = ((IObjectContextAdapter)context).ObjectContext.CreateDatabaseScript();
            // for debugging
            Console.WriteLine(script);
            Assert.IsTrue(!string.IsNullOrEmpty(script));
        }

        [TestCleanup]
        public void TearDown()
        {
            if (null != genericRepository)
            {
                genericRepository.Close();
            }
        }

        [TestMethod]
        public void Test()
        {
            DoAction(() => FindOneCustomer());
            DoAction(() => FindCategoryWithInclude());
            DoAction(() => FindManyOrdersForJohnDoe());
            DoAction(() => FindNewlySubscribed());
            DoAction(() => FindBySpecification());
            DoAction(() => FindByCompositeSpecification());
            DoAction(() => FindByConcretSpecification());
            DoAction(() => FindByConcretCompositeSpecification());
            DoAction(() => UpdateProduct());
        }
        
        private void FindCategoryWithInclude()
        {
            var category = repository.GetQuery<Category>(x => x.Name == "Operating System").Include(c => c.Products).SingleOrDefault();
            Assert.IsNotNull(category);
            Assert.IsTrue(category.Products.Count > 0);
        }        

        private void FindManyOrdersForJohnDoe()
        {
            var customer = customerRepository.FindByName("John", "Doe");
            var orders = repository.Find<Order>(x => x.Customer.Id == customer.Id);

            Console.Write("Found {0} Orders with {1} OrderLines", orders.Count(), orders.ToList()[0].OrderLines.Count);
        }

        private void FindNewlySubscribed()
        {
            var newCustomers = customerRepository.NewlySubscribed();

            Console.Write("Found {0} new customers", newCustomers.Count);
        }

        private void FindBySpecification()
        {
            Specification<Product> specification = new Specification<Product>(p => p.Price < 100);
            IEnumerable<Product> productsOnSale = repository.Find<Product>(specification);
            Assert.AreEqual(2, productsOnSale.Count());
        }

        private void FindByCompositeSpecification()
        {
            IEnumerable<Product> products = repository.Find<Product>(
                new Specification<Product>(p => p.Price < 100).And(new Specification<Product>(p => p.Name == "Windows XP Professional")));
            Assert.AreEqual(1, products.Count());
        }

        private void FindByConcretSpecification()
        {
            ProductOnSaleSpecification specification = new ProductOnSaleSpecification();
            IEnumerable<Product> productsOnSale = repository.Find<Product>(specification);
            Assert.AreEqual(2, productsOnSale.Count());
        }

        private void FindByConcretCompositeSpecification()
        {
            IEnumerable<Product> products = repository.Find<Product>(
                new AndSpecification<Product>(
                    new ProductOnSaleSpecification(),
                    new ProductByNameSpecification("Windows XP Professional")));
            Assert.AreEqual(1, products.Count());
        }

        private void FindOneCustomer()
        {
            var c = repository.FindOne<Customer>(x => x.Firstname == "John" &&
                                                    x.Lastname == "Doe");

            Console.Write("Found Customer: {0} {1}", c.Firstname, c.Lastname);
        }

        private void GetProductsWithPaging()
        {
            var output = repository.Get<Product, string>(x => x.Name, 0, 5).ToList();
            Assert.IsTrue(output[0].Name == "Windows Seven Home");
            Assert.IsTrue(output[1].Name == "Windows Seven Premium");
            Assert.IsTrue(output[2].Name == "Windows Seven Professional");
            Assert.IsTrue(output[3].Name == "Windows Seven Ultimate");
            Assert.IsTrue(output[4].Name == "Windows XP Professional");           
        }

        private void UpdateProduct()
        {
            repository.UnitOfWork.BeginTransaction();

            var output = repository.FindOne<Product>(x => x.Name == "Windows XP Professional");
            Assert.IsNotNull(output);

            output.Name = "Windows XP Home";
            repository.Update<Product>(output);
            repository.UnitOfWork.CommitTransaction();

            var updated = repository.FindOne<Product>(x => x.Name == "Windows XP Home");
            Assert.IsNotNull(updated);
        }

        private static void DoAction(Expression<Action> action)
        {
            Console.Write("Executing {0} ... ", action.Body.ToString());

            var act = action.Compile();
            act.Invoke();

            Console.WriteLine();
        }        
    }
}
