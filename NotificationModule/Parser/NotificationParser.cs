using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrLexer;

namespace Modules.NotificationParser
{
    class NotificationParser
    {
        NotificationModuleConfig Config { get; }

        Lexer<TokenType> Lexer { get; }

        public NotificationParser(NotificationModuleConfig config)
        {
            Config = config;

            Lexer = new Lexer<TokenType>();
            Lexer.AddRule(new Rule(@"^(?<Separator>on|say|to|in)$"));

            Lexer.AddRule(new Rule(@"^(?<Number>[0-9]+)?(?<TimeSpecifier>day(?:s)?|d)$"));
            Lexer.AddRule(new Rule(@"^(?<Number>[0-9]+)?(?<TimeSpecifier>hour(?:s)?|h)$"));
            Lexer.AddRule(new Rule(@"^(?<Number>[0-9]+)?(?<TimeSpecifier>minute(?:s)?|m|min(?:s)?)$"));
            Lexer.AddRule(new Rule(@"^(?<Number>[0-9]+)?(?<TimeSpecifier>second(?:s)?|s)$"));

            Lexer.AddRule(new Rule(@"(?<Selector><|>)+"));
            Lexer.AddRule(new Rule(@"^(?<Number>[0-9]+)$"));
            Lexer.AddRule(new Rule(@"(?<Text>[^ ]+)"));
        }

        /// <summary>
        /// Parse a DateTime from a string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public NotificationJob Parse(string[] input, string author)
        {
            Lexer.Lex(input);
            ParseState state = ParseState.Time;

            bool timeDone = false;
            bool channelDone = false;

            string message = "";
            DateTime dateTime = DateTime.UtcNow;
            TimeSpan timeSpan = new TimeSpan();
            List<string> channels = new List<string>();

            Token<TokenType> token = Lexer.GetToken();
            while (token != null)
            {
                if (token.Type == TokenType.Separator)
                {
                    if (!TryChangeState(token, out state, out string error))
                        throw new Exception(error);

                    Lexer.NextToken();
                }

                switch (state)
                {
                    case ParseState.Date:
                        timeDone = true;
                        if (!TryParseDateTime(Lexer, Config.InputTimeFormats, out dateTime, out string error))
                            throw new Exception(error);

                        break;
                    case ParseState.Time:
                        timeDone = true;
                        if (!TryParseTimespan(Lexer, out timeSpan, out error))
                            throw new Exception(error);

                        break;
                    case ParseState.Channel:
                        channelDone = true;
                        if (!TryParseChannel(Lexer, out channels, out error))
                            throw new Exception(error);

                        break;
                    case ParseState.Text:
                        if (timeDone && channelDone)
						{
                            if (!TryParseText(Lexer, out message))
                                throw new Exception(message);
                        }
                        else
                            throw new Exception("Time and Channels must be defined before say");
                        break;
                }

                // Update token
                token = Lexer.GetToken();
            }

            return new NotificationJob(author, (dateTime - DateTime.UtcNow) + timeSpan, message, channels);
        }

        /// <summary>
        /// Parse a DateTime from a string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool TryParse(string[] input, string author, out NotificationJob notification)
        {
            notification = null;
            if (string.IsNullOrEmpty(string.Join(' ', input)))
                return false;

            Lexer.Lex(input);
            ParseState state = ParseState.Time;

            bool timeDone = false;
            bool channelDone = false;

            string message = "";
            DateTime dateTime = DateTime.UtcNow;
            TimeSpan timeSpan = new TimeSpan();
            List<string> channels = new List<string>();

            Token<TokenType> token = Lexer.GetToken();
            while (token != null)
            {
                if (token.Type == TokenType.Separator)
                {
                    if (!TryChangeState(token, out state, out _))
                        return false;

                    Lexer.NextToken();
                }

                switch (state)
                {
                    case ParseState.Date:
                        timeDone = true;
                        if (!TryParseDateTime(Lexer, Config.InputTimeFormats, out dateTime, out _))
                            return false;

                        break;
                    case ParseState.Time:
                        timeDone = true;
                        if (!TryParseTimespan(Lexer, out timeSpan, out _))
                            return false;

                        break;
                    case ParseState.Channel:
                        channelDone = true;
                        if (!TryParseChannel(Lexer, out channels, out _))
                            return false;

                        break;
                    case ParseState.Text:
                        if (timeDone && channelDone)
                        {
                            if (!TryParseText(Lexer, out message))
                                return false;
                        }
                        else
                            return false;

                        break;
                }

                // Update token
                token = Lexer.GetToken();
            }

            notification = new NotificationJob(author, timeSpan, message, channels);
            return true;
        }

        /// <summary>
        /// Change the state according to token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        bool TryChangeState(Token<TokenType> token, out ParseState state, out string error)
        {
            error = "";
            if (token.Type != TokenType.Separator)
			{
                state = default;
                error = $"Separator expected got {token.Type}: {token.Value}";
                return false;
			}

            switch (token.Value)
            {
                case "to":
                    state = ParseState.Channel;
                    return true;
                case "on":
                    state = ParseState.Date;
                    return true;
                case "say":
                    state = ParseState.Text;
                    return true;
                case "in":
                    state = ParseState.Time;
                    return true;
                default:
                    state = default;
                    error = $"Unknown state specifier {token.Value}";
                    return false;
            }
        }

        /// <summary>
        /// Parse a timespan from user input.
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
        bool TryParseTimespan(Lexer<TokenType> lexer, out TimeSpan timeSpan, out string error)
        {
            error = "";

            Token<TokenType> token = lexer.GetToken();
            if (token.Type != TokenType.Number)
			{
                timeSpan = default;
                error = $"Number expected got {token.Type}: {token.Value}";
                return false;
            }

            timeSpan = new TimeSpan();
            while (token != null && (token.Type == TokenType.TimeSpecifier || token.Type == TokenType.Number))
            {
                if (token.Type == TokenType.Number)
                {
                    Token<TokenType> timeSpecifier = lexer.NextToken();
                    if (timeSpecifier.Type != TokenType.TimeSpecifier)
                        throw new Exception($"Timespecifier expected got {token.Type}: {token.Value}");

                    if (!int.TryParse(token.Value, out int number))
                        throw new Exception($"{token.Value} is not a valid integer");

                    if (timeSpecifier.Value == "day" || timeSpecifier.Value == "days" || timeSpecifier.Value == "d")
                        timeSpan += new TimeSpan(number, 0, 0, 0, 0);

                    if (timeSpecifier.Value == "hour" || timeSpecifier.Value == "hours" || timeSpecifier.Value == "h")
                        timeSpan += new TimeSpan(0, number, 0, 0, 0);

                    if (timeSpecifier.Value == "minute" || timeSpecifier.Value == "minutes" || timeSpecifier.Value == "m")
                        timeSpan += new TimeSpan(0, 0, number, 0, 0);

                    if (timeSpecifier.Value == "second" || timeSpecifier.Value == "seconds" || timeSpecifier.Value == "s")
                        timeSpan += new TimeSpan(0, 0, 0, number, 0);
                }
				else
				{
                    error = $"Number expected got {token.Type}: {token.Value}";
                    timeSpan = default;
                    return false;
                }

                token = lexer.NextToken();
            }

            return true;
        }

        /// <summary>
        /// Parse text for user input
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
		bool TryParseText(Lexer<TokenType> lexer, out string message)
        {
            Token<TokenType> token = lexer.GetToken();
            message = "";

            while (token != null)
            {
                message += $" {token.Value}";

                token = lexer.NextToken();
            }

            message = message.Substring(1);
            return true;
        }

        /// <summary>
        /// Get list of all specified channels
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
        bool TryParseChannel(Lexer<TokenType> lexer, out List<string> channels, out string error)
        {
            Token<TokenType> token = lexer.GetToken();
            channels = new List<string>();

            while (token != null && token.Type != TokenType.Separator)
            {
                if (token.Type == TokenType.Selector)
				{
                    if (!TryParseSelector(lexer, out string group))
					{
                        error = group;
                        return false;
					}

                    channels.Add(group);
                }
                else if (token.Value != "and")
                    channels.Add(token.Value);

                token = lexer.NextToken();
            }

            error = "";
            return true;
        }

        /// <summary>
        /// Get DateTime from string
        /// </summary>
        /// <param name="lexer"></param>
        /// <param name="formats"></param>
        /// <returns></returns>
        bool TryParseDateTime(Lexer<TokenType> lexer, string[] formats, out DateTime dateTime, out string error)
        {
            Token<TokenType> token = lexer.GetToken();
            string dateTimeString = "";

            while (token != null && token.Type != TokenType.Separator)
            {
                dateTimeString += " " + token.Value;
                token = lexer.NextToken();
            }

			try
			{
                error = "";
                dateTime = DateTime.ParseExact(dateTimeString.Substring(1), formats, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();
                return true;
            }
			catch (Exception e)
			{
                error = e.Message;
                dateTime = default;
                return false;
			}
        }

        /// <summary>
        /// Groups stuff between < and > together
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
        bool TryParseSelector(Lexer<TokenType> lexer, out string group)
        {
            Token<TokenType> token = lexer.GetToken();
            string str = token.Value;

            token = lexer.NextToken();
            while (token != null && token.Type != TokenType.Selector)
            {
                str += token.Value;

                token = lexer.NextToken();
            }

            if (token.Value != ">")
			{
                group = "Expected closing bracket >";
                return false;
            }

            group = str + token.Value;
            return true;
        }
    }
}
