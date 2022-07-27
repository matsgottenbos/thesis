using DriverPlannerShared;
using System;
using System.Collections.Generic;

namespace DriverPlannerUi {
    class Program {
        static void Main() {
            ConfigHandler.InitAllConfigs();

            if (DevConfig.DebugThrowExceptions) {
                Run();
                Console.WriteLine("\n*** Program finished ***");
            } else {
                try {
                    Run();
                    Console.WriteLine("\n*** Program finished ***");
                } catch (Exception exception) {
                    Console.WriteLine("\n*** Program exited with error ***\n{0}", exception.Message);
                }
            }

            Console.WriteLine("\nPress enter to exit");

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < 20; i++) GC.Collect();

            Console.ReadLine();
        }

        static void Run() {
            Console.WriteLine("\nRunning UI");
            UiHandler.Run();
        }
    }
}
