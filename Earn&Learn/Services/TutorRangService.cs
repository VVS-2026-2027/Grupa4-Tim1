using System;

namespace Earn_Learn.Services
{
    public class TutorRangService
    {
        public bool DaLiJeIstaknutiTutor(int brojOdrzanihCasova, double prosjecnaOcjena)
        {
            return brojOdrzanihCasova >= 10 && prosjecnaOcjena >= 4.5;
        }
    }
}