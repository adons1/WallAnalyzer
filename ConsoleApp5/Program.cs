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
        Post =0 ,
        Repost = 1,
        Comment = 2
    }
    public class WallPost
    {
        public WallPost(long? PostId, string Text, List<string> Comments)
        {
            this.PostId = PostId;
            this.Text = Text;
            this.Comments = Comments;
        }

        public long? PostId { get; set; }
        public string Text { get; set; }
        public List<string> Comments { get; set; }
        public List<string> PhotoNames { get; set; }
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
        static void Main(string[] args)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo("userphotos/");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            long? id = 2436112;
            string access_token = "4fc507a59d36dce191e6ebe4892ae198506ac28e4d2b191981d711a28390bca51d30e9de0f0d296a59030";
            string[] key_words;
            ulong postsToAnalyze = 20;
            //Console.WriteLine("ID пользователя для анализа:");
            //access_token = Console.ToInt32(Console.ReadLine());
            //Console.WriteLine("access_token вашего аккаунта:");
            //id = Convert.ToInt32(Console.ReadLine());
            //Console.WriteLine("Ключевые слова через пробел:");
            using (FileStream fstream = File.OpenRead("dictionary.txt"))
            {
                byte[] array = new byte[fstream.Length];
                fstream.Read(array, 0, array.Length);
                key_words = System.Text.Encoding.UTF8.GetString(array).Split(',').Select(x=>x.ToLower()).ToArray();
            }

            log = false;

            quantity = new int[key_words.Length];
            quantity.Initialize();

            var api = new VkApi();

            api.Authorize(new ApiAuthParams
            {
                AccessToken = access_token
            });

            var timer = new System.Timers.Timer(1000);
            int n = 0;
            if (log == false)
            {
                timer.Elapsed += delegate {
                    Console.Write("\r Processing: {0} seconds.", n++);
                };
                timer.AutoReset = true;
                timer.Enabled = true;
                timer.Start();
            }
                

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var records = api.Wall.Get(new WallGetParams
            {
                OwnerId = id,
                Count = postsToAnalyze
            });

            int i = 0;
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
                foreach(var attachment in post.Attachments)
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
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            timer.Stop();

            Console.WriteLine($"\n\n\n\n\n\t\t\t\t\t ЗАКЛЮЧЕНИЕ. Выполнено за {ts.Seconds} секунд");
            for (int j = 0; j < key_words.Length; j++){
                Console.WriteLine("\t\t\t\t\t\tКлюч {0}. Найдено {1}", key_words[j], quantity[j]);
            }

            
            Console.ReadLine();
        }
    }
}
