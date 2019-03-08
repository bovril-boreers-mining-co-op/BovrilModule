using System;
using System.Collections.Generic;
using System.Text;
using NModule;
using YahurrLexer;

namespace BovrilModule
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

			string system = ParseSystem();
			var planetMoon = ParsePlanetMoon();

			return new SystemMoon(system, planetMoon.planet, planetMoon.moon);
		}

		string ParseSystem()
		{
			Token<TokenType> token = Lexer.GetToken();

			while (token.Type != TokenType.Operator)
				token = Lexer.NextToken();

			Token<TokenType> prefix = Lexer.Peek(-1);
			Token<TokenType> postFix = Lexer.NextToken();

			string system = $"{prefix.Value}-{postFix.Value}";

			if (system.Length != 6)
				throw new Exception($"Invalid system: {system}");

			return system.ToUpper();
		}

		(int planet, int moon) ParsePlanetMoon()
		{
			Token<TokenType> token = Lexer.GetToken();

			while (token.Type != TokenType.Operator)
				token = Lexer.NextToken();

			Token<TokenType> planet = Lexer.Peek(-1);
			Token<TokenType> moon = Lexer.NextToken();

			if (moon.Type == TokenType.Operator)
				moon = Lexer.NextToken();

			if (planet.Type != TokenType.Roman && planet.Type != TokenType.Number)
				throw new Exception($"Invalid number: {planet.Value}");

			if (moon.Type != TokenType.Number)
				throw new Exception($"Invalid moon number: {moon.Value}");

			int planetNumber;
			if (planet.Type == TokenType.Roman)
				planetNumber = RomanToInteger(planet.Value);
			else
				planetNumber = int.Parse(planet.Value);

			int moonNumber = int.Parse(moon.Value);

			return (planetNumber, moonNumber);
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
