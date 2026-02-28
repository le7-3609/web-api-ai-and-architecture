using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Test
{
    public class TestBase
    {
        protected Mock<TContext> GetMockContext<TContext, TEntity>(List<TEntity> data,Expression<Func<TContext, DbSet<TEntity>>> dbSetSelector)where TContext : DbContext where TEntity : class
        {
            var mockContext = new Mock<TContext>();
            mockContext.Setup(dbSetSelector).ReturnsDbSet(data);
            return mockContext;
        }
    }
}