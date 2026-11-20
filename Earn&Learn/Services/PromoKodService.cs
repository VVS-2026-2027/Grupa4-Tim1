
using System;
using System.Collections.Generic;

namespace Earn_Learn.Services
{
    public class PromoKodService
    {
       
        private static readonly Dictionary<string, decimal> Popusti =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "STUDENT20", 20m },
                { "BESPLATNO100", 100m }
            };

        public decimal IzracunajCijenu(
            decimal osnovnaCijena,
            string kod,
            DateTime datumIsteka)
        {
          
            if (string.IsNullOrWhiteSpace(kod))
            {
                throw new ArgumentException("Promo kod nije unesen.");
            }

         
            if (!Popusti.TryGetValue(kod.Trim(), out decimal procenatPopusta))
            {
                throw new ArgumentException("Nepostojeci promo kod.");
            }

            
            if (datumIsteka <= DateTime.UtcNow)
            {
                throw new ArgumentException("Promo kod je istekao.");
            }

            
            decimal novaCijena =
                osnovnaCijena - (osnovnaCijena * procenatPopusta / 100m);

          
            return Math.Max(0m, novaCijena);
        }
    }
}
