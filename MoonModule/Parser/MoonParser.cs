using Modules;
using Org.BouncyCastle.Asn1.Smime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YahurrLexer;

namespace Modules.MoonParser
{
	class MoonParser
	{
		static Dictionary<char, int> RomanMap = new Dictionary<char, int>()
		{
			{'I', 1},
			{'V', 5},
			{'X', 10},
			{'L', 50},
			{'C', 100},
			{'D', 500},
			{'M', 1000}
		};

		Lexer<TokenType> Lexer { get; }

		public MoonParser()
		{
			Lexer = new Lexer<TokenType>();

			Lexer.AddRule(new Rule("(?<Operator>-|moon)"));
			Lexer.AddRule(new Rule("^(?<Roman>M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3}))$"));
			Lexer.AddRule(new Rule("^(?<Number>[0-9]+)$"));
			Lexer.AddRule(new Rule("^(?<Text>[^ ]+)$"));
		}

		public SystemMoon Parse(string[] input)
		{
			Lexer.Lex(input);

			Token<TokenType> token = Lexer.GetToken();
			if (token.Type == TokenType.Operator)
				Lexer.NextToken();

			if (!ParseSystem(out string system))
				throw new Exception(system);

			if (!ParsePlanetMoon(out int planet, out int moon, out string error))
				throw new Exception(error);

			return new SystemMoon(system, planet, moon);
		}

		public bool TryParse(string[] input, out SystemMoon result)
		{
			if (input.Length == 0 || string.IsNullOrEmpty(input[0]))
			{
				result = null;
				return false;
			}

			Lexer.Lex(input);

			Token<TokenType> token = Lexer.GetToken();
			if (token.Type == TokenType.Operator)
				Lexer.NextToken();

			result = null;
			if (!ParseSystem(out string system) || !ParsePlanetMoon(out int planet, out int moon, out _))
				return false;

			result = new SystemMoon(system, planet, moon);
			return true;
		}

		bool ParseSystem(out string result)
		{
			Token<TokenType> token = Lexer.GetToken();
			while (token.Type != TokenType.Operator)
			{
				token = Lexer.NextToken();

				if (token == null)
				{
					result = "No valid moon name specified.";
					return false;
				}
			}

			Token<TokenType> prefix = Lexer.Peek(-1);
			Token<TokenType> postFix = Lexer.NextToken();

			string system = $"{prefix.Value}-{postFix.Value}";

			if (system.Length != 6)
			{
				result = $"Invalid system: {system}";
				return false;
			}

			result = system.ToUpper();
			return true;
		}

		bool ParsePlanetMoon(out int planet, out int moon, out string error)
		{
			Token<TokenType> token = Lexer.GetToken();

			while (token.Type != TokenType.Operator)
				token = Lexer.NextToken();

			Token<TokenType> planetToken = Lexer.Peek(-1);
			Token<TokenType> moonToken = Lexer.NextToken();

			if (moonToken.Type == TokenType.Operator)
				moonToken = Lexer.NextToken();

			if (planetToken.Type != TokenType.Roman && planetToken.Type != TokenType.Number)
			{
				planet = -1;
				moon = -1;
				error = $"Invalid number: {planetToken.Value}";
				return false;
			}

			if (moonToken.Type != TokenType.Number)
			{
				planet = -1;
				moon = -1;
				error = $"Invalid moon number: {moonToken.Value}";
				return false;
			}

			int planetNumber;
			if (planetToken.Type == TokenType.Roman)
				planetNumber = RomanToInteger(planetToken.Value);
			else
				planetNumber = int.Parse(planetToken.Value);

			int moonNumber = int.Parse(moonToken.Value);

			planet = planetNumber;
			moon = moonNumber;
			error = "";
			return true;
		}

		/// <summary>
		/// https://stackoverflow.com/questions/14900228/roman-numerals-to-integers
		/// </summary>
		/// <param name="roman"></param>
		/// <returns></returns>
		int RomanToInteger(string roman)
		{
			int number = 0;
			for (int i = 0; i < roman.Length; i++)
			{
				if (i + 1 < roman.Length && RomanMap[roman[i]] < RomanMap[roman[i + 1]])
				{
					number -= RomanMap[roman[i]];
				}
				else
				{
					number += RomanMap[roman[i]];
				}
			}
			return number;
		}
	}
}
