using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Test_Bot
{
    class FirstInstaller
    {
        public string PathToDataBase { get; private set; }
        public FirstInstaller(string path)
        {
            try
            {
                this.PathToDataBase = path;
                CreateDataBase();
                CreateTables();
            }
            catch (Exception ex)
            {
                LoggingError(ex);
            }

        }
        /// <summary>
        /// Create DataBase
        /// if same wrong - return exception
        /// </summary>
        private void CreateDataBase()
        {
            try
            {
                if (!System.IO.Directory.Exists(this.PathToDataBase))
                {
                    this.PathToDataBase = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    throw new UriFormatException("Your path was not correct! Then why we place your database in User//Documents//");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.PathToDataBase += "\\TestDB.db";
                SQLiteConnection.CreateFile(this.PathToDataBase);
            }
        }
        /// <summary>
        /// Create Tables in dataBase
        /// if same wrong - return exception
        /// </summary>
        private void CreateTables()
        {
            try
            {
                using (SQLiteConnection m_dbConnection = new SQLiteConnection($"Data Source={PathToDataBase};Version=3;"))
                {
                    m_dbConnection.Open();
                    using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE User(ID INTEGER PRIMARY KEY AUTOINCREMENT,[Login] VARCHAR(52) NOT NULL,[NAME] VARCHAR(52) NOT NULL,[SurNAME] VARCHAR(52) NOT NULL); ", m_dbConnection))
                        command.ExecuteNonQuery();
                    using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE Categories(ID INTEGER PRIMARY KEY AUTOINCREMENT,[NAME] VARCHAR(52) NOT NULL);", m_dbConnection))
                        command.ExecuteNonQuery();
                    using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE Orders(ID INTEGER PRIMARY KEY AUTOINCREMENT,[ID_USER] INTEGER NOT NULL,[ID_GOODS] INTEGER NOT NULL);", m_dbConnection))
                        command.ExecuteNonQuery();
                    using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE Goods(ID INTEGER PRIMARY KEY AUTOINCREMENT,[NAME] VARCHAR(52) NOT NULL,[PRICE] DOUBLE NOT NULL,[ID_CATEGORI] INTEGER NOT NULL,CONSTRAINT FK_CATEGORIES FOREIGN KEY(ID_CATEGORI) REFERENCES Categories(ID));", m_dbConnection))
                        command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// Logging the error in txt file on user Desktop
        /// </summary>
        /// <param name="ex">Inner exception from class methods</param>
        private void LoggingError(Exception ex)
        {
            System.IO.File.WriteAllText(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\CRM_Install_log.txt", $"--- {DateTime.Now.ToString()}\r\n{ex.ToString()}");
        }
    }
    class Categories
    {
        public string Name { get; set; }
        public long ID { get; set; }

        public Categories(long id, string name)
        {
            this.ID = id;
            this.Name = name;
        }
        public override string ToString()
        {
            return ("\nКатегория: " + Name);
        }
    }
    class Order
    {
        public long ID { get; set; }
        public long ID_USER { get; set; }
        public long ID_GOODS { get; set; }
        public long ID_CATEGORIES { get; set; }
        public bool IS_SEND { get; set; }
        public string ADD_TIME { get; set; }

        public Order(long id, long id_user, long id_good, long id_categorie, bool is_send, string add_time)
        {
            this.ADD_TIME = add_time;
            this.ID = id;
            this.ID_CATEGORIES = id_categorie;
            this.ID_GOODS = id_good;
            this.ID_USER = id_user;
            this.IS_SEND = is_send;
        }
    }
    class Goods
    {
        public Goods(long id, string name, double price, long id_categories)
        {
            this.ID = id;
            this.NAME = name;
            this.PRICE = price;
            this.ID_CATEGORIES = id_categories;
        }

        public long ID { get; set; }
        public string NAME { get; set; }
        public double PRICE { get; set; }
        public long ID_CATEGORIES { get; private set; }

        public override string ToString()
        {
            return ("Товар: № " + ID + "\n Название: " + NAME + "\n Цена: " + PRICE);
        }
    }
    class Program
    {
        static HashSet<string> IdFiles;
        static HashSet<long> IdUsers;
        static TelegramBotClient client;
        //static TeleBot bot = TeleBot.GetInstance("1030839993:AAE4MGBW4xMGnCn8heupNj-hyyfgaWHDWAw");
        static List<Goods> goods_list;
        static List<Order> orders_list;
        static List<Categories> categories_list;
        private static readonly ChatId adminID = 351024190;

        static void Main(string[] args)
        {

            //bot.StartBot();
            IdFiles = new HashSet<string>();
            IdUsers = new HashSet<long>();
            string pathToDatabase = "TestDB.db";
            if (!System.IO.File.Exists(pathToDatabase))
            {
                FirstInstaller installer = new FirstInstaller("");
                pathToDatabase = installer.PathToDataBase;
            }

            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source={pathToDatabase}")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("SELECT * FROM Files;", conn);
                using (var reader = command.ExecuteReader())
                {
                    foreach (DbDataRecord record in reader)
                    {
                        IdFiles.Add(record.GetString(0));
                    }
                }
            }
            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source={pathToDatabase}")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("SELECT * FROM User;", conn);
                using (var reader = command.ExecuteReader())
                {
                    foreach (DbDataRecord record in reader)
                    {
                        IdUsers.Add(record.GetInt64(0));
                    }
                }
            }
            goods_list = new List<Goods>();

            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source={pathToDatabase}")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("SELECT* FROM Goods;", conn);
                using (var reader = command.ExecuteReader())
                {
                    foreach (DbDataRecord record in reader)
                    {

                        goods_list.Add(new Goods(record.GetInt64(0), record.GetString(1), record.GetDouble(2), record.GetInt64(3)));
                    }
                }
            }

            orders_list = new List<Order>();
            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source={pathToDatabase}")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("SELECT* FROM Orders;", conn);
                using (var reader = command.ExecuteReader())
                {
                    foreach (DbDataRecord record in reader)
                    {
                        orders_list.Add(new Order(
                            record.GetInt64(0),
                            record.GetInt64(1),
                            record.GetInt64(2),
                            record.GetInt64(3),
                            record.GetInt64(4).ToString() == "1",
                            record.GetDateTime(5).ToString()
                            ));
                    }
                }
            }

            categories_list = new List<Categories>();
            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source={pathToDatabase}")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("SELECT* FROM CATEGORIES;", conn);
                using (var reader = command.ExecuteReader())
                {
                    foreach (DbDataRecord record in reader)
                    {
                        categories_list.Add(new Categories(record.GetInt64(0), record.GetString(1)));
                    }
                }
            }

            client = new TelegramBotClient("1030839993:AAE4MGBW4xMGnCn8heupNj-hyyfgaWHDWAw");
            client.OnMessage += getMessage;
            client.OnCallbackQuery += categori_selected;
            client.StartReceiving();
            //bot.SendText(IdUsers.ElementAt(0), "Test");

            while (client.IsReceiving) ;
        }
        private static void AddCategorie(Categories categories)
        {
            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source=TestDB.db")))
            {
                conn.Open();
                SQLiteCommand command;
                try
                {
                    command = new SQLiteCommand("INSERT INTO Categories (NAME)VALUES(@name);", conn);
                    command.Parameters.Add(new SQLiteParameter("@name", categories.Name));
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //throw;
                }
            }
        }
        private static void AddGoods(Goods addGood)
        {
            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source=TestDB.db")))
            {
                conn.Open();
                SQLiteCommand command;
                try
                {
                    command = new SQLiteCommand("INSERT INTO Goods (NAME,PRICE,ID_CATEGORI)VALUES(@name, @price, @id_categories);", conn);
                    command.Parameters.Add(new SQLiteParameter("@name", addGood.NAME));
                    command.Parameters.Add(new SQLiteParameter("@price", addGood.PRICE));
                    command.Parameters.Add(new SQLiteParameter("@id_categories", addGood.ID_CATEGORIES));
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //throw;
                }
            }
        }
        private static void categori_selected(object sender, CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery.Message.Type != MessageType.Text)
            {
                client.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Можно отправлять только текстовые сообщения!");
                //    client.KickChatMemberAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.From.Id);
            }

            if (e.CallbackQuery.Message.Text.Equals("/categories"))
            {
                var keyboardMarkup = new InlineKeyboardMarkup(GetInlineKeyboard_Goods(long.Parse(e.CallbackQuery.Data)));
                //client.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Вот товары с Вашей категории", replyMarkup: keyboardMarkup);

                client.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "/goods", replyMarkup: keyboardMarkup);

            }
            if (e.CallbackQuery.Message.Text.Equals("/goods"))
            {
                var keyboardMarkup1 = new InlineKeyboardMarkup(GetInlineKeyboard_AssertOrder(e.CallbackQuery.Data));
                client.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                client.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "/contract", replyMarkup: keyboardMarkup1);
            }
            if (e.CallbackQuery.Message.Text.Contains("/contract"))
            {
                client.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                if (e.CallbackQuery.Data.Contains("_"))
                {
                    long cat_id = long.Parse(e.CallbackQuery.Data.Split(new char[] { '_' })[1]);
                    long goods_id = long.Parse(e.CallbackQuery.Data.Split(new char[] { '_' })[0]);

                    using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source=TestDB.db")))
                    {
                        conn.Open();
                        SQLiteCommand command;
                        try
                        {
                            command = new SQLiteCommand("INSERT INTO Orders (ID_USER,ID_GOODS,ID_CATEGORIES)VALUES(@id_user, @id_goods, @id_cat);", conn);
                            command.Parameters.Add(new SQLiteParameter("@id_user", e.CallbackQuery.Message.Chat.Id));
                            command.Parameters.Add(new SQLiteParameter("@id_goods", goods_id.ToString()));
                            command.Parameters.Add(new SQLiteParameter("@id_cat", cat_id.ToString()));
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    var d = goods_list.Where(x => x.ID_CATEGORIES == cat_id && x.ID == goods_id);
                    client.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, $"Ваш {d.ElementAt(0).ToString()}\n\nДобавлен в очередь отправки!\nНаш менеджер скоро с Вами свяжется...");
                    orders_list.Add(new Order(orders_list.Last().ID + 1, e.CallbackQuery.Message.Chat.Id, goods_id, cat_id, false, DateTime.Now.ToString()));

                    client.SendTextMessageAsync(adminID, $"ЗАКАЗ!\n\n{categories_list[int.Parse((cat_id - 1).ToString())].ToString()}\n{d.ElementAt(0).ToString()}\n{DateTime.Now.ToString()}\nОт @{e.CallbackQuery.Message.Chat.Username}");
                }

            }

        }

        private static async void getMessage(object sender, MessageEventArgs e)
        {


            // Console.WriteLine(e.Message.Text.ToString());

            if (e.Message.Type != MessageType.Text)
            {
                await client.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
                //  await client.SendTextMessageAsync(e.Message.Chat.Id, "U+1F601");
                //    await client.KickChatMemberAsync(e.Message.Chat.Id, e.Message.From.Id/*, untilDate: DateTime.Now.AddHours(1)*/);
            }

            Console.WriteLine(e.Message.Type.ToString());
            if (e.Message.Type == MessageType.Document)
            {
                Document d = e.Message.Document;
                Console.WriteLine(d.FileName);
                Console.WriteLine(d.FileId.Length);
                if (!IdFiles.Contains(d.FileName))
                    IdFiles.Add(d.FileId);
            }
            if (e.Message.Type == MessageType.Text)
            {
                if (e.Message.Text.Equals("/addCat"))
                {
                    categories_list.Add(new Categories(categories_list.Last().ID + 1, "вейп"));
                    AddCategorie(categories_list.Last());
                }
                if (e.Message.Text.Equals("/addGood"))
                {
                    goods_list.Add(new Goods(goods_list.Last().ID + 1, "same", 12.33, 1));
                    AddGoods(goods_list.Last());
                }
                if (e.Message.Text.Equals("/start"))
                {
                    using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source=TestDB.db")))
                    {
                        conn.Open();
                        SQLiteCommand command;

                        try
                        {
                            command = new SQLiteCommand("INSERT INTO User (Login, Name, Surname, Login_id) VALUES(@login, @name, @surname, @login_id);", conn);
                            command.Parameters.Add(new SQLiteParameter("login_id", e.Message.Chat.Id));
                            string tmp = e.Message.Chat.Username;
                            if (String.IsNullOrEmpty(tmp))
                                tmp = String.Empty;
                            command.Parameters.Add(new SQLiteParameter("@login", tmp));
                            tmp = e.Message.Chat.FirstName;
                            if (String.IsNullOrEmpty(tmp))
                                tmp = String.Empty;
                            command.Parameters.Add(new SQLiteParameter("@name", tmp));
                            tmp = e.Message.Chat.LastName;
                            if (String.IsNullOrEmpty(tmp))
                                tmp = String.Empty;
                            command.Parameters.Add(new SQLiteParameter("@surname", tmp));

                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine(ex.Message);
                        }



                    }

                    e.Message.Text = "/categories";
                }
                if (e.Message.Text.Equals("/categories"))
                {
                    var keyboardMarkup = new InlineKeyboardMarkup(GetInlineKeyboard_Categories());
                    client.SendTextMessageAsync(e.Message.Chat.Id, e.Message.Text, replyMarkup: keyboardMarkup);
                }
                if (e.Message.Text.Equals("/files"))
                {
                    foreach (var item in IdFiles)
                    {
                        client.SendDocumentAsync(e.Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(item));
                    }
                }

                if (e.Message.Text.Equals("/test"))
                {
                    client.SendDocumentAsync(e.Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(IdFiles.ElementAt(1)), GetInfoAboutFIle(IdFiles.ElementAt(1)));
                    //client.StopReceiving();

                }
            }
        }

        private static InlineKeyboardButton[][] GetInlineKeyboard_Categories()
        {
            int counter = 1;
            var b = categories_list.GroupBy(_ => counter++ / 1).Select(v => v.ToArray());

            var keyboardInline = new InlineKeyboardButton[b.Count()][];
            Console.WriteLine(b.Count().ToString());
            for (int i = 0; i < b.Count(); i++)
            {
                var keyboardButtons = new InlineKeyboardButton[1];
                //for (var j = 0; j < 1; j++)
                //{
                keyboardButtons[0] = new InlineKeyboardButton
                {
                    Text = categories_list[i].Name,
                    CallbackData = categories_list[i].ID.ToString()
                };

                keyboardInline[i] = keyboardButtons;
            }

            return keyboardInline;
        }

        private static InlineKeyboardButton[][] GetInlineKeyboard_AssertOrder(string goods_categorie_id)
        {
            var keyboardInline = new InlineKeyboardButton[2][];
            var yes_btn = new InlineKeyboardButton[1];
            yes_btn[0] = new InlineKeyboardButton() { CallbackData = goods_categorie_id, Text = "хочу" };
            var no_btn = new InlineKeyboardButton[1];
            no_btn[0] = new InlineKeyboardButton() { CallbackData = "no", Text = "не хочу" };

            keyboardInline[0] = yes_btn;
            keyboardInline[1] = no_btn;

            return keyboardInline;
        }
        private static InlineKeyboardButton[][] GetInlineKeyboard_Goods(long categories_id)
        {
            int counter = 1;
            var b = goods_list.Where(x => x.ID_CATEGORIES == categories_id).GroupBy(_ => counter++ / 1).ToList();


            var keyboardInline = new InlineKeyboardButton[b.Count()][];

            for (int i = 0; i < b.Count(); i++)
            {
                var keyboardButtons = new InlineKeyboardButton[1];
                //for (var j = 0; j < 1; j++)
                //{
                keyboardButtons[0] = new InlineKeyboardButton
                {
                    Text = b[i].ElementAt(0).NAME,
                    CallbackData = b[i].ElementAt(0).ID.ToString() + "_" + categories_id.ToString(),
                };
                //    Console.WriteLine();

                //}

                keyboardInline[i] = keyboardButtons;
            }

            return keyboardInline;
        }



        static string GetInfoAboutFIle(string file_id)
        {
            StringBuilder builder = new StringBuilder();


            using (SQLiteConnection conn = new SQLiteConnection(string.Format($"Data Source=TestDB.db;")))
            {
                conn.Open();
                SQLiteCommand command = new SQLiteCommand("SELECT * FROM Files WHERE FileId = '" + file_id + "';", conn);
                using (var reader = command.ExecuteReader())
                {
                    foreach (DbDataRecord record in reader)
                    {
                        if (!reader.IsDBNull(1))
                        {
                            builder.Append("Заголовок: ");
                            builder.Append(reader.GetString(1));
                        }
                        if (!reader.IsDBNull(2))
                        {
                            builder.Append("\nОписание: ");
                            builder.Append(reader.GetString(2));
                        }
                        builder.Append("\nДата добавления: ");
                        builder.Append(reader.GetString(3));
                    }
                }
            }

            return builder.ToString();
        }
    }
}
