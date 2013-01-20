
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
using System.IO;
using Dema.BlenX.VisualStudio;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace Babel
{
   public abstract class BabelLanguageService : LanguageService,
      IVsOutliningCapableLanguage
   {
      #region Custom Colors
      public override int GetColorableItem(int index, out IVsColorableItem item)
      {
         if (index <= Configuration.ColorableItems.Count)
         {
            item = Configuration.ColorableItems[index - 1];
            return Microsoft.VisualStudio.VSConstants.S_OK;
         }
         else
         {
            throw new ArgumentNullException("index");
         }
      }

      public override int GetItemCount(out int count)
      {
         count = Configuration.ColorableItems.Count;
         return Microsoft.VisualStudio.VSConstants.S_OK;
      }
      #endregion

      #region MPF Accessor and Factory specialisation
      private LanguagePreferences preferences;
      public override LanguagePreferences GetLanguagePreferences()
      {
         if (this.preferences == null)
         {
            this.preferences = new LanguagePreferences(this.Site,
                                          typeof(BlenXLanguageService).GUID,
                                          this.Name);
            this.preferences.Init();
            //to enable outlining
            this.preferences.MaxRegionTime = 1;
            this.preferences.AutoOutlining = true;
            this.preferences.ParameterInformation = true;
            this.preferences.EnableCodeSense = true;
         }

         return this.preferences;
      }

      public override Microsoft.VisualStudio.Package.Source CreateSource(IVsTextLines buffer)
      {
         return new Source(this, buffer, this.GetColorizer(buffer));
      }

      private IScanner scanner;
      public override IScanner GetScanner(IVsTextLines buffer)
      {
         if (scanner == null)
            this.scanner = new LineScanner();

         return this.scanner;
      }
      #endregion

      protected Babel.Parser.ErrorHandler handler = null;

      public BabelLanguageService(Package package)
         : base()
      {
         this.handler = new Babel.Parser.ErrorHandler(package);
      }

      public override void OnIdle(bool periodic)
      {
         // from IronPythonLanguage sample
         // this appears to be necessary to get a parse request with ParseReason = Check?
         Source src = (Source)GetSource(this.LastActiveTextView);
         if (src != null && src.LastParseTime >= Int32.MaxValue >> 12)
         {
            src.LastParseTime = 0;
         }
         base.OnIdle(periodic);
      }

      public override AuthoringScope ParseSource(ParseRequest req)
      {
         Source source = (Source)this.GetSource(req.FileName);
         //source.LastParseTime = 0;
         bool yyparseResult = false;

         Parser.Parser parser = new Parser.Parser();  // use noarg constructor

         //redirect
         //using (var stream = File.AppendText("C:\\temp\\out.txt"))
         //{
         //   Console.SetOut(stream);
         //   Console.SetError(stream);

         // req.DirtySpan seems to be set even though no changes have occurred
         // source.IsDirty also behaves strangely
         // might be possible to use source.ChangeCount to sync instead
            if (req.DirtySpan.iStartIndex != req.DirtySpan.iEndIndex
                || req.DirtySpan.iStartLine != req.DirtySpan.iEndLine)
            {
               Debug.Assert(handler != null);
               handler.Clear();

               Babel.Lexer.Scanner scanner = new Babel.Lexer.Scanner(); // string interface

               handler.SetFileName(req.FileName);

               scanner.Handler = handler;
               SymbolTable symbolTable = BlenXLibraryManager.SymbolTable;
               parser.SetParsingInfo(req.FileName, symbolTable, handler);
               parser.scanner = scanner;

               scanner.SetSource(req.Text, 0);

               parser.SetContext(req);
               yyparseResult = parser.Parse();

               // store the parse results
               // source.ParseResult = aast;
               source.ParseResult = null;
               source.Braces = parser.Braces;

               // for the time being, just pull errors back from the error handler
               if (handler.ErrNum > 0)
               {
                  foreach (Babel.Parser.Error error in handler.SortedErrorList())
                  {
                     TextSpan span = new TextSpan();
                     span.iStartLine = span.iEndLine = error.line - 1;
                     span.iStartIndex = error.column;
                     span.iEndIndex = error.column + error.length;
                     req.Sink.AddError(req.FileName, error.message, span, Severity.Error);
                  }
               }
            //}
         }

         switch (req.Reason)
         {
            case ParseReason.Check:
            case ParseReason.HighlightBraces:
            case ParseReason.MatchBraces:
            case ParseReason.MemberSelectAndHighlightBraces:
               // send matches to sink
               // this should (probably?) be filtered on req.Line / col
               if (source.Braces != null)
               {
                  foreach (TextSpan[] brace in source.Braces)
                  {
                     if (brace.Length == 2)
                     {
                        //if (req.Sink.HiddenRegions == true)
                        {
                           string first = source.GetText(brace[0]);
                           if (source.GetText(brace[0]).Contains("[") && source.GetText(brace[1]).Contains("]"))
                           {
                              //construct a TextSpan of everything between the braces
                              TextSpan hideSpan = new TextSpan();
                              hideSpan.iStartIndex = brace[0].iStartIndex;
                              hideSpan.iStartLine = brace[0].iStartLine;
                              hideSpan.iEndIndex = brace[1].iEndIndex;
                              hideSpan.iEndLine = brace[1].iEndLine;
                              req.Sink.ProcessHiddenRegions = true;
                              req.Sink.AddHiddenRegion(hideSpan);
                           }
                        }
                        req.Sink.MatchPair(brace[0], brace[1], 1);
                     }

                     else if (brace.Length >= 3)
                        req.Sink.MatchTriple(brace[0], brace[1], brace[2], 1);
                  }
               }



               break;
            default:
               break;
         }

         return new BlenXAuthoringScope(parser, source);
      }

      private SymbolTable GetSolutionSymbolTable()
      {
          throw new NotImplementedException();
          
      }

      public override string Name
      {
         get { return Configuration.Name; }
      }

      #region IVsOutliningCapableLanguage Members

      public int CollapseToDefinitions(IVsTextLines pTextLines, IVsOutliningSession pSession)
      {
         throw new NotImplementedException();
      }

      #endregion
   }
}
