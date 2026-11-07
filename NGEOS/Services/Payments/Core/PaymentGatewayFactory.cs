using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NGEOS.Services.Payments.Gateways;

namespace NGEOS.Services.Payments.Core
{
    public interface IPaymentGatewayFactory
    {
        IPaymentGateway GetGateway(string gatewayName);
        IEnumerable<IPaymentGateway> GetAllGateways();
        IEnumerable<IPaymentGateway> GetConfiguredGateways();
    }

    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;
        private readonly IServiceProvider _serviceProvider;

        public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways, IServiceProvider serviceProvider)
        {
            _gateways = gateways ?? Enumerable.Empty<IPaymentGateway>();
            _serviceProvider = serviceProvider;
        }

        public IPaymentGateway GetGateway(string gatewayName)
        {
            if (string.IsNullOrWhiteSpace(gatewayName))
            {
                throw new ArgumentNullException(nameof(gatewayName), "Payment gateway name must be provided.");
            }

            var cleanName = gatewayName.Trim();

            // 1. Try resolving from injected collection
            var match = _gateways.FirstOrDefault(g => g.GatewayName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }

            // 2. Direct ServiceProvider fallback
            return cleanName.ToLowerInvariant() switch
            {
                "safepay" => _serviceProvider.GetRequiredService<SafepayPaymentService>(),
                "jazzcash" => _serviceProvider.GetRequiredService<JazzCashPaymentService>(),
                "easypaisa" => _serviceProvider.GetRequiredService<EasypaisaPaymentService>(),
                "payfast" => _serviceProvider.GetRequiredService<PayFastPaymentService>(),
                _ => throw new NotSupportedException($"Payment gateway '{gatewayName}' is not registered or supported in NGEOS.")
            };
        }

        public IEnumerable<IPaymentGateway> GetAllGateways()
        {
            var list = _gateways.ToList();
            if (!list.Any())
            {
                // Fallback to resolving each known gateway
                list.Add(_serviceProvider.GetRequiredService<SafepayPaymentService>());
                list.Add(_serviceProvider.GetRequiredService<JazzCashPaymentService>());
                list.Add(_serviceProvider.GetRequiredService<EasypaisaPaymentService>());
                list.Add(_serviceProvider.GetRequiredService<PayFastPaymentService>());
            }
            return list;
        }

        public IEnumerable<IPaymentGateway> GetConfiguredGateways()
        {
            return GetAllGateways().Where(g => g.IsConfigured);
        }
    }
}
