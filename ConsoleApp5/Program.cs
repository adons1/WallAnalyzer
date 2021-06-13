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
    enum PostRepostComment
    {
        Post = 0 ,
        Repost = 1,
        Comment = 2
    }

    public class ConclusionComparer : IComparer<ConcusionPair>
    {
        public int Compare(ConcusionPair val1, ConcusionPair val2)
        {
            if (val1.value < val2.value)
                return 1;
            else if (val1.value > val2.value)
                return -1;
            else
                return 0;
        }
    }

    public class ConcusionPair
    {
        public ConcusionPair(string key, int value)
        {
            this.key = key;
            this.value = value;
        }
        public string key { get; set; }
        public int value { get; set; }
    }

    class Program
    {
        //Поисх всех вхождений подстроки
        static int AllMatches(string key, string soup)
        {
            var indices = new List<int>();

            int index = soup.IndexOf(key, 0);
            while (index > -1)
            {
                indices.Add(index);
                index = soup.ToLower().IndexOf(key.ToLower(), index + key.Length);
            }
            return indices.Count;
        }

        static bool log = true;
        static int[] quantity;
        //Анализ вхождения ключевых слов
        static string AnalyzeKeyWords(string[] keys, string post, string messageAnalyze)
        {
            post.Replace('ё', 'е');
            post = post.ToLower();

            string conclusion = string.Empty;

            int i = 0;
            foreach (var key in keys)
            {
                int matches = AllMatches(key, post);
                quantity[i] += matches;
                if (post.Length > 0 && matches>0)
                {
                    conclusion += string.Format("\tКлюч: {0} количество {1}\n", key.ToLower(), matches);
                }
                i++;
            }
            if (log)
            {
                if (conclusion.Length > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(messageAnalyze);
                    Console.WriteLine("\n\t" + conclusion);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(messageAnalyze);
                    Console.WriteLine("\t Совпадения не найдены");
                }
                Console.ForegroundColor = ConsoleColor.White;
            }
            
            return conclusion;
        }
       //Анализ фотографий
        static string PythonAnalisys()
        {
            string textFromFile = string.Empty;
            try
            {
                string command = "analyzer.py";
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python";
                start.Arguments = string.Format("{0}", command);
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();

                        FileStream file = new FileStream("alalisys_photos.txt", FileMode.Open);
                        byte[] array = new byte[file.Length];
                        file.Read(array, 0, array.Length);
                        textFromFile = System.Text.Encoding.Default.GetString(array);
                        file.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return textFromFile;
        }
        
        static void AnalyzeReposts(VkApi api, Post post, string[] key_words)
        {
            if (log)
            {

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("\t" + post.CopyHistory.Count.ToString() + " репостов(а)");
                Console.ForegroundColor = ConsoleColor.White;
            }

            foreach (var repost in post.CopyHistory)
            {
                if (log)
                    Console.WriteLine("\t" + repost.Text);

                AnalyzeKeyWords(key_words, repost.Text, "\t" + "Анализ текста репоста:");

                List<string> repostPhotos = new List<string>();
                foreach (var attachment in repost.Attachments)
                {
                    if (attachment.Type.Name == "Photo")
                    {
                        repostPhotos.Add(attachment.Instance.Id.ToString());
                    }
                }

                if (repostPhotos.Count > 0)
                {
                    GetAndAnalyzePhotos(api, repost.OwnerId, PostRepostComment.Repost, key_words, repostPhotos);
                }
            }
        }

        static void GetAndAnalyzePhotos(VkApi api, long? id, PostRepostComment type, string[] key_words, List<string> photos)
        {
            var photosExtended = api.Photo.Get(new PhotoGetParams
            {
                OwnerId = id,
                AlbumId = VkNet.Enums.SafetyEnums.PhotoAlbumType.Wall,
                PhotoIds = photos

            });
            int count = 0;
            foreach (var photo in photosExtended)
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        client.DownloadFile(photo.Sizes.Last().Url.AbsoluteUri, "userphotos/" + "count" + Convert.ToString(count) + ".jpg");
                    }
                    catch
                    {

                    }
                }
                count++;
            }
            //Thread.Sleep(500);
            string result = PythonAnalisys();

            if (log)
                Console.WriteLine("\t'" + result + "'");

            if (log)
                Console.ForegroundColor = ConsoleColor.Red;
            switch (type)
            {
                case PostRepostComment.Post:
                    AnalyzeKeyWords(key_words, result, "Анализ фотографий поста:");
                    break;
                case PostRepostComment.Repost:
                    AnalyzeKeyWords(key_words, result, "\t" + "Анализ фотографий репоста: ");
                    break;
            }
            if (log)
                Console.ForegroundColor = ConsoleColor.White;

            count = 0;
            foreach (var photo in photosExtended)
            {
                File.Delete("userphotos/" + "count" + Convert.ToString(count) + ".jpg");
                count++;
            }
        }
        static void GetAndAnalyzeComments(VkApi api, long? id, string[] key_words, Post post)
        {
            var comments = api.Wall.GetComments(new WallGetCommentsParams
            {
                OwnerId = id,
                PostId = post.Id.Value
            });

            if (log)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\t" + comments.Items.Count.ToString() + " комментариев(я)");
                Console.ForegroundColor = ConsoleColor.White;
            }
                


            StringBuilder commentsBuilder = new StringBuilder();
            foreach (var comment in comments.Items)
            {
                if (log)
                    Console.WriteLine("\t\t" + comment.OwnerId.ToString() + "\t" + comment.Text + "\n");
                commentsBuilder.Append(comment.Text + " ");
                if (comment.Thread.Count > 0)
                {
                    var comments1 = api.Wall.GetComments(new WallGetCommentsParams
                    {
                        OwnerId = comment.OwnerId,
                        CommentId = comment.Id
                    });
                    foreach (var comment1 in comments1.Items)
                    {
                        if (log)
                            Console.WriteLine("\t\t\t" + comment1.OwnerId.ToString() + "\t" + comment1.Text + "\n");
                        commentsBuilder.Append(comment1.Text + " ");
                    }
                }
            }

            AnalyzeKeyWords(key_words, commentsBuilder.ToString(), "\tАнализ комментариев поста:");

            commentsBuilder.Clear();
        }
        static void Analyze(VkApi api, long? id, ulong postsToAnalyze, bool loger)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo("userphotos/");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            string[] key_words;
            using (FileStream fstream = File.OpenRead("SLOva.txt"))
            {
                byte[] array = new byte[fstream.Length];
                fstream.Read(array, 0, array.Length);
                key_words = System.Text.Encoding.UTF8.GetString(array).Split(',').Select(x => x.ToLower()).ToArray();
            }

            log = loger;

            quantity = new int[key_words.Length];
            quantity.Initialize();

            var timer = new System.Timers.Timer(1000);
            int i = 0;
            int n = 0;
            if (log == false)
            {
                timer.Elapsed += delegate {
                    int remained_posts = postsToAnalyze > 100 ? 100 - i : (int)postsToAnalyze - i;
                    Console.Write("\r Processing: {0} seconds. There are {1} to analyze...", n++, remained_posts);
                };
                timer.AutoReset = true;
                timer.Enabled = true;
                timer.Start();
            }


            var records = api.Wall.Get(new WallGetParams
            {
                OwnerId = id,
                Count = postsToAnalyze
            });

            
            foreach (var post in records.WallPosts)
            {
                if (log)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Пост" + i);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(post.Text);
                }
                AnalyzeKeyWords(key_words, post.Text, "Анализ текста поста:");

                List<string> photos = new List<string>();
                foreach (var attachment in post.Attachments)
                {
                    if (attachment.Type.Name == "Photo")
                    {
                        photos.Add(attachment.Instance.Id.ToString());
                    }
                }
                Thread.Sleep(500);

                if (photos.Count > 0)
                {
                    GetAndAnalyzePhotos(api, id, PostRepostComment.Post, key_words, photos);
                }
                i++;
                photos.Clear();
                if (log)
                    Console.WriteLine("\t" + "_____________________________________________________________________________________________________");

                AnalyzeReposts(api, post, key_words);
                if (log)
                    Console.WriteLine("\t" + "_____________________________________________________________________________________________________");


                GetAndAnalyzeComments(api, id, key_words, post);
                if (log)
                    Console.WriteLine("\t" + "_____________________________________________________________________________________________________");
                Thread.Sleep(1000);
            }

            timer.Stop();

            List<ConcusionPair> conclusion = new List<ConcusionPair>();
            for (int j = 0; j < key_words.Length; j++)
            {
                conclusion.Add(new ConcusionPair(key_words[j], quantity[j]));
            }

            ConcusionPair[] conclusion_as_array = conclusion.ToArray();
            Array.Sort(conclusion_as_array, new ConclusionComparer());

            Console.WriteLine($"\n\n\n\n\n\t\t\t\t\t ЗАКЛЮЧЕНИЕ. Выполнено за {n} секунд");

            foreach (var pair in conclusion_as_array)
            {
                Console.WriteLine("\t\t\t\t\t\tКлюч {0}. Найдено {1}", pair.key, pair.value);
            }
        }
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
                    Console.Write("ID пользователя для анализа. Например, 2436112:");
                    id = Convert.ToInt32(Console.ReadLine());

                    ulong postsToAnalyze = 0;
                    Console.Write("\nКоличество анализируемых постов (не более 100):");
                    postsToAnalyze = (ulong)Convert.ToInt32(Console.ReadLine());

                    bool log = false;
                    Console.Write("\nЛогировать? (0/1):");
                    log = Convert.ToBoolean(Convert.ToInt32(Console.ReadLine()));

                    Analyze(api, id, postsToAnalyze, log);
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
