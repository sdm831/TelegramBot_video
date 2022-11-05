using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Telegram.Bot.Types.InputFiles;
using File = System.IO.File;

var token = "token";
var botClient = new TelegramBotClient(token: $"{token}"); // токен бота
using var cts = new CancellationTokenSource(); // токен отмены

var receiverOptions = new ReceiverOptions
{ // настройки получения обновлений
    AllowedUpdates = { }
};

// начало получения обновлений
botClient.StartReceiving( 
    HandleUpdatesAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);

var me = await botClient.GetMeAsync();
Console.WriteLine($"Bot: \"{me.Username}\" is runing.\n");
Console.ReadLine();
cts.Cancel();

async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Type == UpdateType.Message && update?.Message?.Text != null)
    { // проверка на то, что пользователь отправил сообщение
        await HandleMessage(botClient, update.Message);
        return;
    }

    // проверка на то, что пользователь нажал на inline-кнопку
    if (update!.Type == UpdateType.CallbackQuery)
    {
        await HandleCallbackQuery(botClient, update!.CallbackQuery!);
        return;
    }
}

// метод для отправки сообщений пользователю после нажатия на inline-кнопку
async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    await botClient.SendTextMessageAsync(callbackQuery!.Message!.Chat.Id, text: $"{callbackQuery.Data}");
    return;
}

async Task HandleMessage(ITelegramBotClient botClient, Message message)
{
    if (message.Text == "/start")
    { // команда запуска/обновления бота
        ReplyKeyboardMarkup keyboard = new(new[] { new KeyboardButton[] { "YouTube ✅" }})
        {
            ResizeKeyboard = true
        };

        // сообщение при начале работы бота, показ клавиатуры пользователю
        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Нажмите ниже для получения подробной инструкции 🧐", replyMarkup: keyboard);
        
        return;
    }

    if (message.Text == "YouTube ✅")
    {
        InlineKeyboardMarkup keyboard = new(new[] // реализация inline клавиатура под сообщением 
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Video (MP4) 🎬", callbackData: "Ссылка на видео + \"видео\""),
                InlineKeyboardButton.WithCallbackData(text: "Audio (MP3) 🎧", callbackData: "Ссылка на видео + \"аудио\"")
            },
        });

        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Выберите в какой формат сконвертировать ссылку, затем введите ссылку и через пробел введите \"видео\" или \"аудио\" соответственно. Отправьте введённое сообщение❗️");
        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Пример с получением видео: https://youtu.be/videoId видео \nПример с получением аудио: https://youtu.be/videoId аудио", replyMarkup: keyboard);
        
        return;
    }

    // Получение ссылки на видео от пользователя
    // проверка, что пользователь запросил видео
    if (message.Text != null && message.Text.EndsWith("видео") && message.Text.Length > 6)
    {
        var dirPath = @"D:\testVideo";
        var filePath = $@"{dirPath}\video.mp4";
        var link = message.Text.Split()[0];

        // сохранение этого видео на диск с заменой существующего и отправка его в телеграм
        var client = new YoutubeClient();

        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);            
            if (File.Exists(filePath)) File.Delete(filePath);
        }        

        // используем try catch для того, чтобы предотвратить остановку программы в случае получения неверных данных от пользователя
        try
        {
            var streamManifest = await client.Videos.Streams.GetManifestAsync(link);
            var streamInfo = (MuxedStreamInfo)streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

            await client.Videos.Streams.DownloadAsync(streamInfo, filePath: filePath);
            using FileStream fileStream = new(filePath, FileMode.Open);
            
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
            await botClient.SendVideoAsync(chatId: message.Chat.Id,
                                                  video: new InputOnlineFile(fileStream, fileName));
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, text: "Произошла ошибка 😕. Возможные причины: \n1. Объем файла слишком велик ❌ \n2. Отправленная ссылка некорректна ❌ ");
        }
        
        return;
    }

    // Получение ссылки на видео от пользователя
    else if (message.Text != null && message.Text.EndsWith("аудио"))
    { // проверка, что пользователь запросил аудио

        string link = message.Text.Substring(0, message.Text.Length - 6);
        string filePath = $@"D:\testVideo\audio.mp3";

        // сохранение аудио этого видео на диск с заменой существующего и отправка его в телеграм
        var client = new YoutubeClient();
        FileInfo fileInf = new FileInfo($@"D:\testVideo\audio.mp3");
        if (fileInf.Exists)
        {
            fileInf.Delete();
        }
        // используем try catch для того, чтобы предотвратить остановку программы в случае получения неверных данных от пользователя
        try
        {
            var streamManifest = await client.Videos.Streams.GetManifestAsync(link);
            var streamInfo = (AudioOnlyStreamInfo)streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            await client.Videos.Streams.DownloadAsync(streamInfo, filePath: $@"D:\testVideo\audio.mp3");
            using FileStream fileStream = new(filePath, FileMode.Open);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
            await botClient.SendAudioAsync(chatId: message.Chat.Id,
                                                  audio: new InputOnlineFile(fileStream, fileName));
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, text: "Произошла ошибка 😕. Возможные причины: \n1. Объем файла слишком велик ❌ \n2. Отправленная ссылка некорректна ❌ ");
        }
        return;
    }
    await botClient.SendTextMessageAsync(message.Chat.Id, text: "Неизвестная команда 😐 ");
}

Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
{ // обработка ошибок
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
        => $"Ошибка телеграм АПИ:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage); // вывод текста ошибки в консоль
    return Task.CompletedTask;
}