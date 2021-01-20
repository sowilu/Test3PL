using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using HSNXT.DSharpPlus.Extended.Emoji;
using DSharpPlus.Interactivity;
using unirest_net.http;
using Newtonsoft.Json;
using System.Collections.Generic;
using DSharpPlus.Entities;
using System.Linq;
using SimMetrics.Net.Metric;

namespace _3PL1_DiscordBot
{
    class Test
    {
        public int test { get; set; }
    }


    public class DadJoke
    {


        public string id { get; set; }
        public string joke { get; set; }

        public int status { get; set; }
    }

    class Program
    {
        static DiscordClient discord;
        static CommandsNextModule commands;
        static InteractivityModule interactivity;

        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = "Nzc4NjU0ODU5NjQ5MjIwNjQ5.X7VI2Q.dHlgWrXfDuKnktv1y55fPFo32Pw",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });

            discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().Contains("ping"))
                    await e.Message.RespondAsync("PONG!");
            };

            commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefix = ";;",
                CaseSensitive = false
            }) ;
            commands.RegisterCommands<MyCommands>();

            interactivity = discord.UseInteractivity(new InteractivityConfiguration());



            await discord.ConnectAsync();

            await Task.Delay(-1);
        }
    }


    public class MyCommands
    {
        bool CompareString(string original, params string[] correct)
        {
            var jmetric = new JaroWinkler();

            foreach (var item in correct)
            {
                var jx = jmetric.GetSimilarity(item, original.ToLower().Trim());

                if (jx >= 0.85f)
                    return true;
            }

            return false;
        }


        [Command("hi")]
        [Aliases("hello", "hey", "ahoy", "ehlo")]
        [Description("Hold a short conversation")]
        public async Task Hi(CommandContext commandInfo)
        {
            await commandInfo.RespondAsync($"{Emoji.Wave} Hi, {commandInfo.User.Mention}!");

            var interactivity = commandInfo.Client.GetInteractivityModule();

            var msg = await interactivity.WaitForMessageAsync(response => response.Author.Id == commandInfo.User.Id && CompareString(response.Content, "how are you?", "whats up?", "hows life?"), TimeSpan.FromMinutes(1));

            if (msg != null)
            {
                await commandInfo.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(3));

                await commandInfo.RespondAsync($"I'm fine, thank you {Emoji.Cat}");
            }

        }

        [Command("random")]
        [Description("Get a random number")]
        public async Task Random(CommandContext commandInfo, [Description("Smalles possible number")]int min,[Description("Highest possible number")]int max)
        {
            var rnd = new Random();
            await commandInfo.RespondAsync($"{Emoji.FlowerPlayingCards} Your random number is: {rnd.Next(min, max)}");
        }

        [Command("dad")]
        [Aliases("joke", "telljoke")]
        [Description("Get a dad joke")]
        public async Task DadJoke(CommandContext commandInfo)
        {
            var response = await Unirest.get(@"https://icanhazdadjoke.com/")
                .header("Accept", "application/json")
                .asJsonAsync<String>();

            DadJoke dad = JsonConvert.DeserializeObject<DadJoke>(response.Body);

            await commandInfo.TriggerTypingAsync();
            await Task.Delay(TimeSpan.FromSeconds(3));

            await commandInfo.RespondAsync($"{Emoji.OldMan} {dad.joke} {Emoji.Laughing}");


            var interactivity = commandInfo.Client.GetInteractivityModule();

            while (true)
            {
                var reactionResult = await interactivity.WaitForReactionAsync(emoji =>
                {
                    
                    return emoji.Name == Emoji.Thumbsup;
                },
                           TimeSpan.FromSeconds(60)
                            );

                if (reactionResult != null)
                {
                    await commandInfo.RespondAsync($"{Emoji.CocktailGlass} {reactionResult.User.Mention} has good taste!");
                }
            }

        }

        [Command("play_game")]
        [Aliases("game")]
        [Description("Play a number guessing game")]
        public async Task Game(CommandContext commandInfo)
        {
            var number = new Random().Next(0, 100);

            await commandInfo.RespondAsync($"{Emoji.WavingHand} I have number in mind, try to guess it");

            var interactivity = commandInfo.Client.GetInteractivityModule();

            while (true)
            {
                var msg = await interactivity.WaitForMessageAsync(response => response.Author.Id == commandInfo.User.Id, TimeSpan.FromMinutes(5));

                if (msg != null)
                {
                    var guess = Convert.ToInt32(msg.Message.Content);

                    if (guess == number)
                    {
                        await commandInfo.RespondAsync($"{Emoji.ConfettiBall} YOU WIN!!!");
                        return;
                    }
                    else if (guess > number)
                    {
                        await commandInfo.RespondAsync("Lower");
                    }
                    else
                    {
                        await commandInfo.RespondAsync("Higher");
                    }
                }
            }
        }

        [Command("simple_poll")]
        [Description("Create a simple emoji poll")]
        public async Task Poll(CommandContext commandInfo,[Description("Time until result counting")]TimeSpan duration, [Description("Poll reactions")]params DiscordEmoji[] emojiOptions)
        {
            var interactivity = commandInfo.Client.GetInteractivityModule();

            var options = emojiOptions.Select(e => e.ToString());

            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = "Poll",
                Description = string.Join(' ', options),
                Color = DiscordColor.Orange
            };

            var pollMessage = await commandInfo.Channel.SendMessageAsync(embed: pollEmbed);

            //put emojis
            foreach (var option in emojiOptions)
            {
                await pollMessage.CreateReactionAsync(option);
            }

            //store result
            var reactions = await interactivity.CollectReactionsAsync(pollMessage, duration);

            var results = reactions.Reactions.Select(e => $"{e.Key} {e.Value}");

            await commandInfo.Channel.SendMessageAsync(string.Join('\n', results));
        }


        [Command("poll")]
        [Description("Create a yes/no or multiple choise poll")]
        public async Task Poll(CommandContext commandInfo, [Description("Time until result counting")] TimeSpan duration, [Description("Poll question")]string question, [Description("Poll answers, not needed for yes/no questions")]params string[] answers)
        {
            var interactivity = commandInfo.Client.GetInteractivityModule();

            List<DiscordEmoji> optionEmojis = new List<DiscordEmoji>();

            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = $"Poll: {question}",
                Color = DiscordColor.Yellow
            };

            if (answers.Length == 0)
            {
                optionEmojis.Add(DiscordEmoji.FromUnicode(Emoji.Thumbsup));
                optionEmojis.Add(DiscordEmoji.FromUnicode(Emoji.Thumbsdown));
            }
            else
            {
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":one:"));
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":two:"));
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":three:"));
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":four:"));
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":five:"));
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":six:"));
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":seven:"));
                optionEmojis.Add(DiscordEmoji.FromName(commandInfo.Client, ":eight:"));

                var size = (answers.Length > 8) ? 8 : answers.Length;

                var description = "";
                for (int i = 0; i < size; i++)
                {
                    description += $"{optionEmojis[i]} {answers[i]}\n";
                }

                pollEmbed.Description = description;

                if (size < 8)
                    optionEmojis.RemoveRange(size, optionEmojis.Count - size);
            }

            pollEmbed.Description += duration;

            var pollMessage = await commandInfo.Channel.SendMessageAsync(embed: pollEmbed);

            //put emojis
            foreach (var options in optionEmojis)
            {
                await pollMessage.CreateReactionAsync(options);
            }

            //store results
            var result = interactivity.CollectReactionsAsync(pollMessage, duration);

            while (!result.IsCompleted)
            {
                var index = pollEmbed.Description.LastIndexOf('\n');

                duration = duration.Subtract(TimeSpan.FromSeconds(1));

                pollEmbed.Description = $"{pollEmbed.Description.Substring(0, index)}\n{duration}";

                pollMessage.ModifyAsync(embed: pollEmbed);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            var reactions = result.Result;

            var statistics = reactions.Reactions.Select(e => $"{e.Key} {e.Value}");

            await commandInfo.Channel.SendMessageAsync(string.Join('\n', statistics));
        }


        [Command("dm_me")]
        [Description("Bot sends you a dm")]
        public async Task Dm(CommandContext commandInfo)
        {
            var dm = await commandInfo.Client.CreateDmAsync(commandInfo.User);

            await dm.SendMessageAsync("Hi there");
        }

        [Command("dm")]
        [Description("Bot sends your message to selected user")]
        public async Task Dm(CommandContext commandInfo,[Description("Text to be send")]string message, [Description("user/s to receive the message")]params DiscordUser[] users)
        {
            foreach (var item in users)
            {
                var dm = await commandInfo.Client.CreateDmAsync(item);

                await dm.SendMessageAsync(message);
            }

        }
    
    
        [Command("show")]
        [Description("See movie selection/ info about specific movie")]
        public async Task Show(CommandContext commandInfo,[Description("Movie id")]int id=-1)
        {
            var description = "";

           
            if(id==-1)
            {
                MySqlClient.Connect();
                var query = "SELECT * FROM movie;";
                var movieReader = MySqlClient.GetDataReader(query);

                if (movieReader == null) return;

                while(movieReader.Read())
                {
                    var m_id = movieReader.GetUInt64("id");
                    var name = movieReader.GetString("name");
                    var date = movieReader.GetValue(2);

                    description += $"**{m_id}** {name} {date}\n";
                }
            }
            else
            {
                MySqlClient.Connect();
                var query = $"SELECT * FROM movie WHERE id={id};";

                var movieReader = MySqlClient.GetDataReader(query);

                if (movieReader == null) return;

                movieReader.Read();

                var name = movieReader.GetString("name");
                var year = movieReader.GetValue(2);
                var director = movieReader.GetString("producer");
                var cast = movieReader.GetString("cast");
                var synopsis = movieReader.GetString("description");
                var genre = movieReader.GetString("genre");
                var upvotes = movieReader.GetInt32("upvotes");
                var downvotes = movieReader.GetInt32("downvotes");

                description = $"**Title:** {name}\n**Year:**{year}\n**Director:**{director}\n**Cast:**{cast}\n**Synopsis:**{synopsis}\n**Genre:**{genre}\n\n{Emoji.Thumbsup}{upvotes}{Emoji.Thumbsdown}{downvotes}";
            }

            var movieEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Chartreuse,
                Title = "Pirate Bay",
                Description = description,
                ImageUrl = @"https://images.spot.im/v1/production/hppagfv8dv4mo3glkrdx"
            };

            await commandInfo.Channel.SendMessageAsync(embed: movieEmbed);
        }
    
    
       [Command("play")]
       [Description("Sends movie link and video info")]
        public async Task PlayMovie(CommandContext commandInfo, [Description("Movie name")]string name)
        {
            //get movie id from name
            var query = $"SELECT id FROM movie WHERE name='{name}';";

            MySqlClient.Connect();
            var result = MySqlClient.GetResult(query);

            if (result == null)
            {
                await commandInfo.Channel.SendMessageAsync("No such movie found");
                return;
            }

            var id = Convert.ToInt32(result);

            //TODO: if more trhan one movie found - let user chose

            MySqlClient.Connect();

            query = $"SELECT m.name, e.m_language, e.quality, e.link FROM entry e, movie m WHERE e.movie_id={id} AND m.id={id};";

            var reader = MySqlClient.GetDataReader(query);

            if (reader == null) return;

            reader.Read();

            var movieEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Goldenrod,
                Title = reader.GetString("name"),
                Description = $"**Language:**{reader.GetString("m_language")}\n**Quality:**{reader.GetString("quality")}"
            };

            await commandInfo.Channel.SendMessageAsync(content: reader.GetString("link"), embed: movieEmbed);
        }
    
        [Command("find")]
        [Description("Find info about a specific movie")]
        public async Task FindMovie(CommandContext commandInfo,[Description("Movie name")] string name)
        {
            MySqlClient.Connect();

            var query = $"SELECT * FROM movie WHERE name LIKE CONCAT('%','{name}','%');";

            var reader = MySqlClient.GetDataReader(query);

            if (reader == null) return;

            var description = "";
            while (reader.Read())
            {
                var id = reader.GetUInt64("id");
                var m_name = reader.GetString("name");
                var year = reader.GetValue(2);

                description += $"**{id}** {m_name} {year}\n";
            }

            var movieEmbed = new DiscordEmbedBuilder
            {
                Title = "Pirate Bay",
                Color = DiscordColor.Blurple,
                Description = description
            };

            await commandInfo.Channel.SendMessageAsync(content: $"Results for `{name}`", embed: movieEmbed);
        }

        [Command("add")]
        [Description("Add a new movie")]
        public async Task AddMovie(CommandContext commandInfo,[Description("Movie name")] string name, [Description("Movie release year")] string year, [Description("Movie link")] string link, [Description("Movie producer")] string producer="John Doe", [Description("Movie cast")] string cast="Jane Doe", [Description("Movie description")] string description="Is a movie", [Description("Movie genre")] string genre="unknown", [Description("Video language")] string language="human", [Description("Video quality (1-240, 2-480, 3-720)")] int quality=2)
        {
            //check if movie already exists
            var query = $"SELECT COUNT(ID) FROM movie WHERE name = '{name}' AND r_year={year};";

            MySqlClient.Connect();
            var result = MySqlClient.GetResult(query);

            if (result == null) return;

            if(Convert.ToInt32(result) > 0)
            {
                await commandInfo.Channel.SendMessageAsync($"This movie already exists {Emoji.FacePalm}");
                return;
            }

            //add movie
            query = $"INSERT movie VALUES(NULL,'{name}', {year}, '{producer}', '{cast}', '{description}', '{genre}', 0, 0);";

            MySqlClient.ExecuteCommand(query);

            //get movie id
            query = $"SELECT id FROM movie WHERE name='{name}' AND r_year={year};";

            MySqlClient.Connect();
            result = MySqlClient.GetResult(query);

            if (result == null) return;

            var id = Convert.ToInt32(result);

            //add entry
            query = $"INSERT entry VALUES(NULL, {id}, '{language}', {quality}, 'youtube', '{link}');";

            MySqlClient.ExecuteCommand(query);

            await commandInfo.Channel.SendMessageAsync($"{name}({year}) added to movie list {Emoji.Popcorn}");
        }


        [Command("delete")]
        [Description("Delete a movie")]
        public async Task DeleteMovie(CommandContext commandInfo, [Description("Movie name")] string name, [Description("Movie release year")] string year)
        {
            //get role of the person calling the command
            var roles = commandInfo.Member.Roles;
            foreach (var r in roles)
            {
                Console.WriteLine(r.Name);
            }

            //TODO: if list of movies - ask user which one to delete

            var query = $"DELETE FROM movie WHERE name='{name}' AND r_year={year};";

            MySqlClient.Connect();
            MySqlClient.ExecuteCommand(query);

            //TODO: check if movie was deleted
            await commandInfo.Channel.SendMessageAsync($"**{name}({year})** was deleted {Emoji.Church}");
        }


        [Command("kill")]
        public async Task ClearTask(CommandContext commandInfo)
        {
            var messagesToDelete = await commandInfo.Channel.GetMessagesAsync(2);

            foreach (var item in messagesToDelete)
            {
                await item.DeleteAsync();
            }
        }
    }
}
