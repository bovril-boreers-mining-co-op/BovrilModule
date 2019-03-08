using System;
using System.Collections.Generic;
using System.Text;

namespace NModule.Parser
{
	enum TokenType
	{
		Number,
		Text,
		Separator,
        Selector,

		TimeSpecifier,
		DateSpecifier,
		DateSeparator,
	}
}
