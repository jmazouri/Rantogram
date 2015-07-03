using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rant;
using TelegramBotSharp;
using TelegramBotSharp.Types;

namespace Rantogram
{
    public class Program
    {
        private static TelegramBot _bot;
        private static RantEngine _engine;
        private static Dictionary<string, RantPattern> historyDictionary; 

        static void Main()
        {
            _bot = new TelegramBot(File.ReadAllText("apikey.txt"));
            _engine = new RantEngine();
            _engine.LoadPackage("Rantionary.rantpkg");

            Console.WriteLine("Rant Loaded! {0} dictionaries.", _engine.Dictionary.GetTables().Count());

            historyDictionary = new Dictionary<string, RantPattern>();

            new Task(HandleMessages).Start();

            Console.WriteLine("Press enter to quit.");
            Console.ReadLine();
        }

        static async void HandleMessages()
        {
            while (true)
            {
                var result = await _bot.GetMessages();
                foreach (Message m in result)
                {
                    if (m.Date < DateTime.Now.AddMinutes(-1)) { continue; }
                    if (m.Text == null) { continue; }

                    MessageTarget target = (m.Chat ?? (MessageTarget)m.From);

                    if (m.Text.StartsWith("/rant"))
                    {
                        string[] parm = m.Text.Split(new [] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (parm.Length < 2) { continue; }

                        try
                        {
                            RantPattern pattern = RantPattern.FromString(parm[1]);

                            RantOutput output = _engine.Do(pattern, 1000, 5);

                            _bot.SendMessage(target, output.Main, true, m);

                            historyDictionary[m.From.Username] = pattern;
                        }
                        catch (Exception ex)
                        {
                            _bot.SendMessage(target, "Rant had an error: " + ex.Message, true, m);
                        }

                    }

                    if (m.Text.StartsWith("/again"))
                    {
                        if (historyDictionary.ContainsKey(m.From.Username))
                        {
                            RantOutput output = _engine.Do(historyDictionary[m.From.Username]);
                            _bot.SendMessage(target, output.Main, true, m);
                        }
                        else
                        {
                            _bot.SendMessage(target, "You haven't sent anything yet!", true, m);
                        }
                    }
                }
            }
        }
    }
}
