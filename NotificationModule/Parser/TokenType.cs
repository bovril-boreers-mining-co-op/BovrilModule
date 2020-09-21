using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.NotificationParser
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
