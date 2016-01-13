using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestProject
{
    class Program
    {
        static bool dead = false;
        static void goDeeper()
        {
            if (dead == true)
                return;
            goDeeper();
        }

        static void Main(string[] args)
        {
            goDeeper();
        }
    }
}
