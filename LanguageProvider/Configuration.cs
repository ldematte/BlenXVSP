using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Package;
using Babel.Parser;
using Microsoft.VisualStudio.TextManager.Interop;
using Dema.BlenX.VisualStudio;

namespace Babel
{
   public static partial class Configuration
   {
      public const string Name = BlenXPackageConstants.PLKProductName;
      public const string Extension1 = BlenXPackageConstants.btypeFileExtension;
      public const string Extension2 = BlenXPackageConstants.bprogFileExtension;
      public const string Extension3 = BlenXPackageConstants.bfuncFileExtension;

      public const string FormatList = "BlenX source File (*.prog, *.type, *.func)|*.prog; *.type; *.func";

      static CommentInfo bprogInfo;
      public static CommentInfo MyCommentInfo { get { return bprogInfo; } }

      static Configuration()
      {
         bprogInfo.BlockEnd = "*/";
         bprogInfo.BlockStart = "/*";
         bprogInfo.LineStart = "//";
         bprogInfo.UseLineComments = false;

         // default colors - currently, these need to be declared
         CreateColor("Keyword", COLORINDEX.CI_BLUE, COLORINDEX.CI_USERTEXT_BK);
         CreateColor("Comment", COLORINDEX.CI_DARKGREEN, COLORINDEX.CI_USERTEXT_BK);
         CreateColor("Identifier", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK);
         CreateColor("String", COLORINDEX.CI_RED, COLORINDEX.CI_USERTEXT_BK);
         CreateColor("Number", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK);
         CreateColor("Text", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK);

         TokenColor error = CreateColor("Error", COLORINDEX.CI_RED, COLORINDEX.CI_USERTEXT_BK, false, true);


         //None	 Used when no triggers are set. This is the default.
         //MemberSelect	A character that indicates that the start of a member selection has been parsed. In C#, this could be a period following a class name. In XML, this could be a < (the member select is a list of possible tags).
         //MatchBraces	The opening or closing part of a language pair has been parsed. For example, in C#, a { or } has been parsed. In XML, a < or > has been parsed.
         //MethodTip	This is a mask for the flags used to govern the IntelliSense Method Tip operation. This mask is used to isolate the values Parameter, ParameterStart, ParameterNext, and ParameterEnd.
         //ParameterStart	A character that marks the start of a parameter list has been parsed. For example, in C#, this could be an open parenthesis, "(".
         //ParameterNext	A character that separates parameters in a list has been parsed. For example, in C#, this could be a comma, ",".
         //ParameterEnd	A character that marks the end of a parameter list has been parsed. For example, in C#, this could be a close parenthesis, ")".
         //Parameter	A parameter in a method's parameter list has been parsed. 

         //
         // map tokens to color classes
         //
         ColorToken((int)Tokens.LLET, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LFUNCTION, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LFUNCTION, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LBETAPROCESS, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LPIPROCESS, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LMOLECULE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSTATEVAR, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LFUNCTION, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LCONST, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LPREFIX, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LTEMPLATE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LNAME, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LTYPE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LBNIL, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LRUN, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LRATE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LWHEN, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LIDENTITY, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LINIT, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LEXP, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LPOW, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LLOG, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSQRT, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LB1, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LB2, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LDELETE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LJOIN, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSPLIT, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LUPDATE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LNEW, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LDELTA, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LTIME, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSTEP, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSTEPS, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LTIMESPAN, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LBASERATE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LRCHANGE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LRHIDE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LREXPOSE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LRUNHIDE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);


         ColorToken((int)Tokens.LCHANGE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LEXPOSE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LDIE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LHIDE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LUNHIDE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LTAU, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LBANG, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LNIL, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LIF, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LTHEN, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LENDIF, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LCHANGED, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSTATEBOUND, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSTATEHIDE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);
         ColorToken((int)Tokens.LSTATEUNHIDE, TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int)Tokens.LREAL, TokenType.Literal, TokenColor.Number, TokenTriggers.None);
         ColorToken((int)Tokens.LDECIMAL, TokenType.Literal, TokenColor.Number, TokenTriggers.None);
         ColorToken((int)Tokens.LINF, TokenType.Literal, TokenColor.Number, TokenTriggers.None);

         //ColorToken((int)Tokens.LEX_WHITE, TokenType.Text, TokenColor.Text, TokenTriggers.MemberSelect);

         ColorToken((int)Tokens.LDOT, TokenType.Operator, TokenColor.Keyword, TokenTriggers.MemberSelect);
         ColorToken((int)Tokens.LPARALLEL, TokenType.Operator, TokenColor.Keyword, TokenTriggers.None); //TokenTriggers.MemberSelect
         ColorToken((int)Tokens.LCHOICE, TokenType.Operator, TokenColor.Keyword, TokenTriggers.None); //TokenTriggers.MemberSelect
         ColorToken((int)Tokens.LLEFTARROW, TokenType.Operator, TokenColor.Keyword, TokenTriggers.None);

         ColorToken((int) Tokens.LDDOT, TokenType.Operator, TokenColor.Keyword, TokenTriggers.ParameterNext);
         ColorToken((int)Tokens.LCOMMA, TokenType.Delimiter, TokenColor.Text, TokenTriggers.ParameterNext);

         ColorToken((int)Tokens.LGOPEN, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces);
         ColorToken((int)Tokens.LGCLOSE, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces);
         ColorToken((int)Tokens.LPOPEN, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces | TokenTriggers.ParameterStart );
         ColorToken((int)Tokens.LPCLOSE, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces | TokenTriggers.ParameterEnd);
         ColorToken((int)Tokens.LDAOPEN, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces | TokenTriggers.ParameterStart);
         ColorToken((int)Tokens.LDACLOSE, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces | TokenTriggers.ParameterEnd);
         ColorToken((int)Tokens.LSOPEN, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces);
         ColorToken((int)Tokens.LSCLOSE, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces);
         ColorToken((int)Tokens.LAOPEN, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces);
         ColorToken((int)Tokens.LACLOSE, TokenType.Delimiter, TokenColor.Text, TokenTriggers.MatchBraces);

         ColorToken((int)Tokens.LID, TokenType.Identifier, TokenColor.Identifier, TokenTriggers.None);

         //// Extra token values internal to the scanner
         ColorToken((int)Tokens.LEX_ERROR, TokenType.Text, error, TokenTriggers.None);
         ColorToken((int)Tokens.LEX_COMMENT, TokenType.Text, TokenColor.Comment, TokenTriggers.None);

      }
   }
}
