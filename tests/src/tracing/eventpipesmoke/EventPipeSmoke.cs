using System;
using System.IO;
using Tracing.Tests.Common;

namespace Tracing.Tests
{
    class EventPipeSmoke
    {
        private static int allocIterations = 10000;
        private static int trivialSize = 0x100000;

        static int Main(string[] args)
        {
            bool keepOutput = false;
            // Use the first arg as an output filename if there is one
            string outputFilename = null;
            if (args.Length >= 1) {
                outputFilename = args[0];
                keepOutput = true;
            }
            else {
                outputFilename = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".netperf";
            }

            Console.WriteLine("\tStart: Enable tracing.");
            TraceControl.EnableDefault(outputFilename);
            Console.WriteLine("\tEnd: Enable tracing.\n");

            Console.WriteLine("\tStart: Allocation.");
            // Allocate for allocIterations iterations.
            for(int i=0; i<allocIterations; i++)
            {
                GC.KeepAlive(new object());
            }
            Console.WriteLine("\tEnd: Allocation.\n");

            Console.WriteLine("\tStart: Disable tracing.");
            TraceControl.Disable();
            Console.WriteLine("\tEnd: Disable tracing.\n");

            FileInfo outputMeta = new FileInfo(outputFilename);
            Console.WriteLine("\tCreated {0} bytes of data", outputMeta.Length);

            bool pass = false;
            if (outputMeta.Length > trivialSize){
                pass = true;
            }

            if (keepOutput)
            {
                Console.WriteLine(String.Format("\tOutput file: {0}", outputFilename));
            }
            else
            {
                System.IO.File.Delete(outputFilename);
            }

            return pass ? 100 : 0;
        }
    }
}
