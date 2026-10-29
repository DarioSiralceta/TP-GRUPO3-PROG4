using Payment.DAL.Context;
using Payment.DAL.Interfaces;
using Payment.Domain.Entities;

namespace Payment.DAL.Repositories;

// Implementacion del repositorio - (Federico Diaz)
public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    // Metodo que se encarga de persistir el PaymentAttempt - (Federico Diaz)
    public async Task AddAsync(PaymentAttempt attempt)
    {
        _context.PaymentAttempts.Add(attempt);
        await _context.SaveChangesAsync();
    }
}
