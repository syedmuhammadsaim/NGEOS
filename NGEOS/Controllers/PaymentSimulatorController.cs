using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NGEOS.Data;

namespace NGEOS.Controllers
{
    [Route("payments/simulator")]
    public class PaymentSimulatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentSimulatorController> _logger;

        public PaymentSimulatorController(ApplicationDbContext context, ILogger<PaymentSimulatorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? @ref, [FromQuery] string? gateway, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(@ref))
            {
                return RedirectToAction("Index", "Donate");
            }

            var txn = await _context.PaymentTransactions
                .Include(t => t.Donation)
                .ThenInclude(d => d!.Cause)
                .FirstOrDefaultAsync(t => t.InternalReference == @ref, cancellationToken);

            if (txn == null || txn.Donation == null)
            {
                TempData["ErrorMessage"] = "The requested transaction reference was not found.";
                return RedirectToAction("Index", "Donate");
            }

            ViewBag.Txn = txn;
            ViewBag.Donation = txn.Donation;
            ViewBag.Gateway = string.IsNullOrWhiteSpace(gateway) ? txn.GatewayName : gateway;

            return View("~/Views/Donate/PaymentSimulator.cshtml");
        }

        [HttpPost("process")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(
            [FromForm] string @ref,
            [FromForm] string gateway,
            [FromForm] string actionType,
            [FromForm] string? accountNo,
            [FromForm] string? pinCode,
            CancellationToken cancellationToken)
        {
            var txn = await _context.PaymentTransactions
                .Include(t => t.Donation)
                .FirstOrDefaultAsync(t => t.InternalReference == @ref, cancellationToken);

            if (txn == null || txn.Donation == null)
            {
                return RedirectToAction("Index", "Donate");
            }

            if (actionType == "cancel")
            {
                _logger.LogInformation("Donor cancelled simulated payment for reference {Ref}", @ref);
                return RedirectToAction("Index", "Donate", new { causeId = txn.Donation.CauseId, cancelled = true });
            }

            if (actionType == "fail")
            {
                _logger.LogWarning("Donor simulated failed payment for reference {Ref}", @ref);
                return RedirectToAction("Callback", "PaymentCallback", new
                {
                    gateway = gateway.ToLowerInvariant(),
                    @ref = @ref,
                    simulated = "true",
                    status = "Failed",
                    amount = txn.Amount.ToString("F2")
                });
            }

            // Success action
            var gRef = txn.GatewayReference;
            if (string.IsNullOrWhiteSpace(gRef))
            {
                gRef = $"{gateway.ToUpper()}-SIM-{Guid.NewGuid():N}".Substring(0, 24);
                txn.GatewayReference = gRef;
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Simulated successful payment processing for {Gateway} Ref {Ref}", gateway, @ref);

            return RedirectToAction("Callback", "PaymentCallback", new
            {
                gateway = gateway.ToLowerInvariant(),
                @ref = @ref,
                simulated = "true",
                gatewayRef = gRef,
                amount = txn.Amount.ToString("F2")
            });
        }
    }
}
