using DriverPlannerShared;
using System;

namespace DriverPlannerUi {
    class Program {
        static void Main() {
            if (DevConfig.DebugThrowExceptions) {
                ConfigHandler.InitAllConfigs();
                Run();
                Console.WriteLine("\n*** Program finished ***");
            } else {
                try {
                    ConfigHandler.InitAllConfigs();
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
            Console.WriteLine("Running UI");
            UiHandler.Run();
        }
    }
}
