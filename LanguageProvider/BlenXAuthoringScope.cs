/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Package;

namespace Dema.BlenX.VisualStudio
{
   public class BlenXAuthoringScope : AuthoringScope
   {
      readonly Babel.Parser.Parser parser;
      readonly Babel.IASTResolver resolver;
      readonly Source source;

      public BlenXAuthoringScope(Babel.Parser.Parser parser, Babel.Source source)
      {
         this.parser = parser;
         this.source = source;

         // how should this be set?
         this.resolver = new Babel.Resolver();
      }


      // ParseReason.QuickInfo
      public override string GetDataTipText(int line, int col, out TextSpan span)
      {
         TokenInfo tokenInfo = source.GetTokenInfo(line, col);
         span = new TextSpan
                   {
                      iStartLine = line,
                      iEndLine = line,
                      iStartIndex = tokenInfo.StartIndex,
                      iEndIndex = tokenInfo.EndIndex + 1
                   };

         string tokenText = source.GetText(span);

         if (parser.SymbolTable != null)
         {
            if (parser.SymbolTable.HasSymbol(tokenText))
               return tokenText + ": " + parser.SymbolTable.TypeOf(tokenText).ToString();
         }

         foreach (var v in Babel.Lexer.Scanner.piKeywords)
            if (v.Id.Equals(tokenText))
               return v.Description;

         return null;
      }

      // ParseReason.CompleteWord
      // ParseReason.DisplayMemberList
      // ParseReason.MemberSelect
      // ParseReason.MemberSelectAndHilightBraces
      public override Declarations GetDeclarations(IVsTextView view, int line, int col, TokenInfo info, ParseReason reason)
      {
         IList<Babel.Declaration> declarations;
         switch (reason)
         {
            case ParseReason.CompleteWord:
               declarations = resolver.FindCompletions(parser, line, col);
               break;
            case ParseReason.DisplayMemberList:
            case ParseReason.MemberSelect:
            case ParseReason.MemberSelectAndHighlightBraces:
               declarations = resolver.FindMembers(parser, line, col);
               break;
            default:
               throw new ArgumentException("reason");
         }

         return new Babel.Declarations(declarations);
      }

      // ParseReason.GetMethods
      public override Methods GetMethods(int line, int col, string name)
      {
         return new BlenXMethods(resolver.FindMethods(parser, line, col, name));
      }

      // ParseReason.Goto
      public override string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
      {
         //TODO: if (cmd == Microsoft.VisualStudio.VSConstants.VSStd97CmdID.GotoRef)
         TokenInfo tokenInfo = source.GetTokenInfo(line, col);
         var sourceSpan = new TextSpan
                             {
                                iStartLine = line,
                                iEndLine = line,
                                iStartIndex = tokenInfo.StartIndex,
                                iEndIndex = tokenInfo.EndIndex + 1
                             };

         string tokenText = source.GetText(sourceSpan);

         if (parser.SymbolTable.HasSymbol(tokenText))
         {
            span = parser.SymbolTable.SymbolToLocation[tokenText];
            span.iStartLine--;
            span.iEndLine--;
            return parser.SymbolTable.SymbolToFile[tokenText]; //source.GetFilePath();
         }
         else
         {
            span = new TextSpan();
            return null;
         }
      }
   }
}