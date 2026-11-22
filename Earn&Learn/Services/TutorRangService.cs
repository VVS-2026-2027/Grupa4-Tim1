using System;

namespace Earn_Learn.Services
{
    public class TutorRangService
    {
        private const int MinimalnoCasova = 10;
        private const double MinimalnaOcjena = 4.5;
        private const double PovlastenaProvizijaStopa = 0.05;
        private const double StandardnaProvizijaStopa = 0.10;

        public bool DaLiJeIstaknutiTutor(int brojOdrzanihCasova, double prosjecnaOcjena)
        {
            return brojOdrzanihCasova >= MinimalnoCasova && prosjecnaOcjena >= MinimalnaOcjena;
        }

        public double IzracunajProviziju(double cijenaTermina, int brojOdrzanihCasova, double prosjecnaOcjena)
        {
            if (cijenaTermina < 0)
                throw new ArgumentException("Cijena termina ne može biti negativna.", nameof(cijenaTermina));

            var stopa = DaLiJeIstaknutiTutor(brojOdrzanihCasova, prosjecnaOcjena)
                ? PovlastenaProvizijaStopa
                : StandardnaProvizijaStopa;

            return Math.Round(cijenaTermina * stopa, 2);
        }
    }
}
