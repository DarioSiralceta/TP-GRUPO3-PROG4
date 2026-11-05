using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.DAL.Context;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
}
