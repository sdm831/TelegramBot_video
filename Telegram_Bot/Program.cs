using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Telegram.Bot.Types.InputFiles;
// @YouTube_Video_Functions_Bot

var botClient = new TelegramBotClient(token: "5264626683:AAHw2q73yM8s1tebQZTlQVQt3Y095qWaT5c"); // токен бота
using var cts = new CancellationTokenSource(); // токен отмены
var receiverOptions = new ReceiverOptions { // настройки получения обновлений
    AllowedUpdates = { }
};

botClient.StartReceiving( // начало получение обновлений
    // параметры
    HandleUpdatesAsync,   
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);

var me = await botClient.GetMeAsync();
Console.WriteLine($"{me.Username} запущен"); // сигнал о том, что бот запущен и работает
Console.ReadLine();
cts.Cancel();

async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
    if(update.Type == UpdateType.Message && update?.Message?.Text != null) {
        await HandleMessage(botClient, update.Message);
        return;
    }
    if(update!.Type == UpdateType.CallbackQuery) {
        await HandleCallbackQuery(botClient, update!.CallbackQuery!);
        return;
    }
}

async Task HandleCallbackQuery (ITelegramBotClient botClient, CallbackQuery callbackQuery) {
    await botClient.SendTextMessageAsync(callbackQuery!.Message!.Chat.Id, text: $"{callbackQuery.Data}"); 
    return; 
}

async Task HandleMessage(ITelegramBotClient botClient, Message message) { 
    if (message.Text == "/start") {
        ReplyKeyboardMarkup keyboard = new(new[] {
            new KeyboardButton[] { "YouTube ✅" }
        }) {
            ResizeKeyboard = true
        };
        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Нажмике ниже для получения подробной инструкции 🧐", replyMarkup: keyboard); // сообщение при начале работы бота, показ клавиатуры пользователю        
        return;
    }

    
    if (message.Text == "YouTube ✅") {
        InlineKeyboardMarkup keyboard = new(new[] // клавиатура в сообщении
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Video (MP4) 🎬", callbackData: "Ссылка на видео + \"видео\""),
                InlineKeyboardButton.WithCallbackData(text: "Audio (MP3) 🎧", callbackData: "Ссылка на видео + \"аудио\"")
            },
        });    
        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Выберите в какой формат сконвертировать ссылку, затем отправьте ссылку и через пробел введите \"видео\" или \"аудио\" соответственно ❗️");
        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Пример с получением видео: https://youtu.be/videoId видео \nПример с получением аудио: https://youtu.be/videoId аудио", replyMarkup: keyboard);
        return;
    }
    if (message.Text != null && message.Text.EndsWith("видео") && message.Text.Length > 7) { // проверка, что пользователь запросил видео
        string link = message.Text.Substring(0, message.Text.Length - 6);
        string filePath = $@"D:\testVideo\video.mp4";

        // Получение ссылки на видео от пользователя,
        // сохранение этого видео на диск с заменой существующего и отправка его в телеграм
        var client = new YoutubeClient();
        
        FileInfo fileInf = new FileInfo($@"D:\testVideo\video.mp4");   
        if (fileInf.Exists) {
            fileInf.Delete();
        }
        try {
            var streamManifest = await client.Videos.Streams.GetManifestAsync(link);
            var streamInfo = (MuxedStreamInfo)streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
            await client.Videos.Streams.DownloadAsync(streamInfo, filePath: $@"D:\testVideo\video.mp4");
            using FileStream fileStream = new(filePath, FileMode.Open);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
            await botClient.SendVideoAsync(chatId: message.Chat.Id,
                                                  video: new InputOnlineFile(fileStream, fileName));
        }
        catch {
            await botClient.SendTextMessageAsync(message.Chat.Id, text: "Произошла ошибка 😕. Возможные причины: \n1. Объем файла слишком велик ❌ \n2. Отправленная ссылка некорректна ❌ ");
        }
        // Получение ссылки на видео от пользователя,
        // сохранение этого видео на диск с заменой существующего и отправка его в телеграм
        return;       
    }

    else if (message.Text != null && message.Text.EndsWith("аудио")) { // проверка, что пользователь запросил аудио
        string link = message.Text.Substring(0, message.Text.Length - 6);
        string filePath = $@"D:\testVideo\audio.mp3";
        // Получение ссылки на видео от пользователя,
        // сохранение аудио этого видео на диск с заменой существующего и отправка его в телеграм
        var client = new YoutubeClient();       
        FileInfo fileInf = new FileInfo($@"D:\testVideo\audio.mp3");
        if (fileInf.Exists) {
            fileInf.Delete();
        }
        try {
            var streamManifest = await client.Videos.Streams.GetManifestAsync(link);
            var streamInfo = (AudioOnlyStreamInfo)streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            await client.Videos.Streams.DownloadAsync(streamInfo, filePath: $@"D:\testVideo\audio.mp3");
            using FileStream fileStream = new(filePath, FileMode.Open);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
            await botClient.SendAudioAsync(chatId: message.Chat.Id,
                                                  audio: new InputOnlineFile(fileStream, fileName));
        }
        catch {
            await botClient.SendTextMessageAsync(message.Chat.Id, text: "Произошла ошибка 😕. Возможные причины: \n1. Объем файла слишком велик ❌ \n2. Отправленная ссылка некорректна ❌ ");
        }
        return;
        // Получение ссылки на видео от пользователя,
        // сохранение аудио этого видео на диск с заменой существующего и отправка его в телеграм
    }
    await botClient.SendTextMessageAsync(message.Chat.Id, text: "Неизвестная команда 😐 ");
}

Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken) { // обработка ошибок
    var ErrorMessage = exception switch {
        ApiRequestException apiRequestException
        => $"Ошибка телеграм АПИ:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage); // вывод текста ошибки в консоль
    return Task.CompletedTask;
}