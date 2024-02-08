using System;

namespace EeveeBot
{
    public class Program
    {
        private static BotLogic _logic;
        static void Main(string[] args)
        {
            try
            {
                do
                {
                    using (_logic = new BotLogic())
                        _logic.StartBotAsync().GetAwaiter().GetResult();
                }
                while (_logic.Relaunch);
            }
            catch (Exception)
            {
                Console.ReadLine();
            }
        }
    }
}
