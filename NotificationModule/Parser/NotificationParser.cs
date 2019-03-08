using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YahurrFramework;
using YahurrFramework.Attributes;
using YahurrLexer;

namespace NModule.Parser
{
	class NotificationParser
	{
		NotificationConfig Config { get; }

		Lexer<TokenType> Lexer { get; }

		public NotificationParser(NotificationConfig config)
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
		public Notification Parse(string[] input, string author)
		{
			Lexer.Lex(input);
			ParseState state = ParseState.Time;

			bool timeDone = false;
			bool channelDone = false;

            string message = "";
            DateTime dateTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
            TimeSpan timeSpan = new TimeSpan();
			List<string> channels = new List<string>();

			Token<TokenType> token = Lexer.GetToken();
			while (token != null)
			{
				if (token.Type == TokenType.Separator)
				{
                    state = ChangeState(token);
                    Lexer.NextToken();
				}

				switch (state)
				{
					case ParseState.Date:
						timeDone = true;
                        dateTime = ParseDateTime(Lexer, Config.InputTimeFormats);
                        break;
					case ParseState.Time:
						timeDone = true;
                        timeSpan = ParseTimespan(Lexer);
						break;
					case ParseState.Channel:
						channelDone = true;
                        channels = ParseChannel(Lexer);
						break;
					case ParseState.Text:
						if (timeDone && channelDone)
							message = ParseText(Lexer);
						else
							throw new Exception("Time and Channels must be defined before say");
						break;
					default:
						token = Lexer.NextToken();
						break;
				}

				// Update token
				token = Lexer.GetToken();
			}

			return new Notification(author, dateTime + timeSpan, message, channels);
		}

        /// <summary>
        /// Change the state according to token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        ParseState ChangeState(Token<TokenType> token)
        {
            if (token.Type != TokenType.Separator)
                throw new Exception($"Separator expected got {token.Type}: {token.Value}");

            switch (token.Value)
            {
                case "to":
                    return ParseState.Channel;
                case "on":
                    return ParseState.Date;
                case "say":
                    return ParseState.Text;
                case "in":
                    return ParseState.Time;
                default:
                    throw new Exception($"Unknown state specifier {token.Value}");
            }
        }

        /// <summary>
        /// Parse a timespan from user input.
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
        TimeSpan ParseTimespan(Lexer<TokenType> lexer)
		{
			Token<TokenType> token = lexer.GetToken();
            TimeSpan timeSpan = new TimeSpan();

            if (token.Type != TokenType.Number)
                 throw new Exception($"Number expected got {token.Type}: {token.Value}");

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
                    throw new Exception($"Number expected got {token.Type}: {token.Value}");

                token = lexer.NextToken();
			}

			return timeSpan;
		}

        /// <summary>
        /// Parse text for user input
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
		string ParseText(Lexer<TokenType> lexer)
		{
			Token<TokenType> token = lexer.GetToken();
			string message = "";

			while (token != null)
			{
				message += $" {token.Value}";

				token = lexer.NextToken();
			}

			return message.Substring(1);
		}

        /// <summary>
        /// Get list of all specified channels
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
        List<string> ParseChannel(Lexer<TokenType> lexer)
		{
			Token<TokenType> token = lexer.GetToken();
            List<string> messages = new List<string>();

			while (token != null && token.Type != TokenType.Separator)
			{
                if (token.Type == TokenType.Selector)
                    messages.Add(ParseSelector(lexer));
                else if (token.Value != "and")
                    messages.Add(token.Value);

				token = lexer.NextToken();
			}

            return messages;
        }

        /// <summary>
        /// Get DateTime from string
        /// </summary>
        /// <param name="lexer"></param>
        /// <param name="formats"></param>
        /// <returns></returns>
        DateTime ParseDateTime(Lexer<TokenType> lexer, string[] formats)
        {
            Token<TokenType> token = lexer.GetToken();
            string dateTimeString = "";

            while (token != null && token.Type != TokenType.Separator)
            {
                dateTimeString += " " + token.Value;
                token = lexer.NextToken();
            }

            return DateTime.ParseExact(dateTimeString.Substring(1), formats, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Groups stuff between < and > together
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
        string ParseSelector(Lexer<TokenType> lexer)
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
                throw new Exception("Expected closing bracket >");

            str += token.Value;
            return str;
        }
	}
}
