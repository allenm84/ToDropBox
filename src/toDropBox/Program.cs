using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace toDropBox
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.Out.WriteLine("Beginning toDropBox");
      Console.Out.WriteLine("===============================");
      Console.Out.WriteLine("\tReceived: [{0}]", string.Join(", ", args));

      if (args.Length != 2)
      {
        Console.Error.WriteLine("Invalid number of arguments. Usage: dropcopy <Source> <Destination>");
        Console.Error.WriteLine("\t<Source>: The full path of the directory to copy to DropBox");
        Console.Error.WriteLine("\t<Destination>: The relative path of the directory to copy to. (Relative to DropBox).");
        Environment.Exit(1);
      }
      else
      {
        new DropCopy(args[0], args[1]).Run();
      }
    }
  }
}
