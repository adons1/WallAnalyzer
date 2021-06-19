using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace ConsoleApp5
{
    public partial class Program
    {
        static bool log = true;
        static int[] quantity;
        
        static void Main(string[] args)
        {
            
            string access_token = string.Empty;
            Console.Write("\naccess_token вашего аккаунта:");
            access_token = Console.ReadLine();

            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = access_token
            });

            while (true)
            {
                try
                {
                    long? id = 0;//2436112
                    Console.Write("ID пользователя для анализа. Например, 2436112:  ");
                    id = Convert.ToInt32(Console.ReadLine());

                    ulong postsToAnalyze = 0;
                    Console.Write("\nКоличество анализируемых постов:  ");
                    postsToAnalyze = (ulong)Convert.ToInt32(Console.ReadLine());

                    bool log = false;
                    Console.Write("\nЛогировать? (0/1):  ");
                    log = Convert.ToBoolean(Convert.ToInt32(Console.ReadLine()));

                    string dictionary = string.Empty;
                    Console.Write("\nВведите файл словаря (должен быть в дирекотории с программой):  ");
                    dictionary = Console.ReadLine();

                    Analyze(api, id, postsToAnalyze, log, dictionary);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
                
            }
            Console.ReadLine();
        }
    }
}
