using MDS.Core;

namespace MDS.Client
{
    public static class ClientAdminDetection
    {
        private static bool loginAttempt = false;
        public static void TrySpawnAdminCheck()
        {
            if (!loginAttempt)
            {
                loginAttempt = true;
                // A refusal is the expected answer for anyone who is not an admin, so it is not worth a warning.
                CommandExecutor.TryExecute("rc serverAdmin playerlist", out _);
                CommandExecutor.TryExecute("cls", out _);
                Logger.Log("Spawn login check initiated from client.", LogLevel.INFO);
            }
        }

    }
}
