using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
    public class UseMyDbContextTest
    {
        private MyDbContext context;
        private ICustomerRepository customerRepository;

        private GenericRepository genericRepository;

        private IRepository repository
        {
            get { return genericRepository; }
        }

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
            string script = ((IObjectContextAdapter) context).ObjectContext.CreateDatabaseScript();
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
            Category category =
                repository.GetQuery<Category>(x => x.Name == "Operating System")
                    .Include(c => c.Products)
                    .SingleOrDefault();
            Assert.IsNotNull(category);
            Assert.IsTrue(category.Products.Count > 0);
        }

        private void FindManyOrdersForJohnDoe()
        {
            Customer customer = customerRepository.FindByName("John", "Doe");
            IEnumerable<Order> orders = repository.Find<Order>(x => x.Customer.Id == customer.Id);

            Console.Write("Found {0} Orders with {1} OrderLines", orders.Count(), orders.ToList()[0].OrderLines.Count);
        }

        private void FindNewlySubscribed()
        {
            IList<Customer> newCustomers = customerRepository.NewlySubscribed();

            Console.Write("Found {0} new customers", newCustomers.Count);
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

        private void UpdateProduct()
        {
            repository.UnitOfWork.BeginTransaction();

            var output = repository.FindOne<Product>(x => x.Name == "Windows XP Professional");
            Assert.IsNotNull(output);

            output.Name = "Windows XP Home";
            repository.Update(output);
            repository.UnitOfWork.CommitTransaction();

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