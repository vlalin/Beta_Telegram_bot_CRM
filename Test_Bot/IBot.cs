namespace Test_Bot
{
    interface IBot
    {
        void StopBot();
        void StartBot();
        void SendFile(string chatId, string documentId, string title = "", string text ="", string dataAdd ="");
        void SendText(long chatId, string text);
    }
}
