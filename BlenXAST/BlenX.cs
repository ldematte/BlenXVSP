using System;

namespace Dema.BlenX.Parser
{
   public class BlenX
   {
      public class ConstantSymbols
      {
         public const string NIL_BPROC = "$Nil";
         public const string NO_PROCESS = "$X";
         public const string EMPTY_OUTPUT = "$empty_output";
         public const string INF_RATE = "inf";
         public const string INTERNAL_PROCESS = "$pproc";
      }

      public class Rate
      {
         public static string ToString(double r)
         {
            if (Double.IsInfinity(r) ||
                Double.IsNaN(r))
               return ConstantSymbols.INF_RATE;
            return r.ToString(System.Globalization.CultureInfo.InvariantCulture);
         }
      }
   }
}

