using Telegram.Bot;

namespace Test_Bot
{
    
    class TeleBot : IBot
    {
        private TeleBot() { }

        private static TeleBot _instance;
        private static TelegramBotClient _bot;
        private static readonly object _lock = new object();
        public string telegramKey { get; private set; }

        public static TeleBot GetInstance(string value)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new TeleBot();
                        _instance.telegramKey = value;
                        _bot = new TelegramBotClient(_instance.telegramKey);
                    }
                }
            }
            return _instance;
        }

        public void SendFile(string chatId, string documentId, string title = "", string text = "", string dataAdd = "")
        {
            //throw new System.NotImplementedException();
        }

        public async void SendText(long chatId, string text)
        {
            await _bot.SendTextMessageAsync(chatId, text);
        }

        public void StopBot()
        {
            _bot.StopReceiving();
        }

        public void StartBot()
        {
            _bot.StartReceiving();
        }
    }

}
