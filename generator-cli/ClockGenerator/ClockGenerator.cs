namespace WordWallGenerator
{
    public class ClockGenerator
    {
        public static void Main()
        {
            string [] digits = new[] { "", "one", "two",  "three" , "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
            string[] decades = new[] { "", "ten", "twenty", "thirty", "forty", "fifty", "sixty"};
            for (int h = 1; h <= 12; ++h)
            {
                Console.Write("It is ");
                Console.Write(digits[h]);
                Console.Write(" on the clock");
                Console.WriteLine();
                
               for (int m = 1; m <= 9; ++m)
               {
                   Console.Write("It is ");
                   Console.Write(digits[h]);
                   Console.Write(" oh ");
                   Console.Write(digits[m]);
                   Console.WriteLine();
               }
               
               for (int m = 10; m <= 19; ++m)
               {
                   Console.Write("It is ");
                   Console.Write(digits[h]); 
                   Console.Write(" ");
                   Console.Write(digits[m]);
                   Console.WriteLine();
               }
               
               for (int d = 2; d <= 5; ++d)
               {
                   for (int m = 0; m < 10; ++m)
                   {
                       Console.Write("It is ");
                       Console.Write(digits[h]); 
                       Console.Write(" ");
                       Console.Write(decades[d]);
                       if (!string.IsNullOrEmpty(digits[m]))
                       {
                           Console.Write(" ");
                           Console.Write(digits[m]);         
                       }
                       Console.WriteLine();
                   }
               }
            }
        }
    }
}