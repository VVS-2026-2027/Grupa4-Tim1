
using System;
using System.Collections.Generic;

namespace Earn_Learn.Services
{
    public class PromoKodService
    {
        private sealed record PromoKodInfo(decimal ProcenatPopusta, DateTime DatumIsteka);
        private static readonly DateTime DatumIstekaPrimjera =
            new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        private static readonly Dictionary<string, PromoKodInfo> PromoKodovi =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "STUDENT20", new PromoKodInfo(20m, DatumIstekaPrimjera) },
                { "BESPLATNO100", new PromoKodInfo(100m, DatumIstekaPrimjera) }
            };

        public decimal IzracunajCijenu(decimal osnovnaCijena, string? kod)
        {
            var promoKod = ValidirajKod(kod);
            ValidirajRok(promoKod.DatumIsteka);
            return IzracunajCijenu(osnovnaCijena, promoKod.ProcenatPopusta);
        }

        public decimal IzracunajCijenu(
            decimal osnovnaCijena,
            string? kod,
            DateTime datumIsteka)
        {
            var promoKod = ValidirajKod(kod);
            ValidirajRok(datumIsteka);
            return IzracunajCijenu(osnovnaCijena, promoKod.ProcenatPopusta);
        }

        private static PromoKodInfo ValidirajKod(string? kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                throw new ArgumentException("Promo kod nije unesen.");

            if (!PromoKodovi.TryGetValue(kod.Trim(), out var promoKod))
                throw new ArgumentException("Nepostojeci promo kod.");

            return promoKod;
        }

        private static void ValidirajRok(DateTime datumIsteka)
        {
            if (datumIsteka <= DateTime.UtcNow)
                throw new ArgumentException("Promo kod je istekao.");
        }

        private static decimal IzracunajCijenu(decimal osnovnaCijena, decimal procenatPopusta)
        {
            var novaCijena = osnovnaCijena - (osnovnaCijena * procenatPopusta / 100m);
            return Math.Max(0m, novaCijena);
        }
    }
}
