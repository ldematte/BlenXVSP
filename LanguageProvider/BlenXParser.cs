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
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Package;

namespace Babel.Parser
{
   public static class TextSpanUtils
   {
      public static bool IsBefore(this TextSpan me, TextSpan other)
      {
         if (me.iEndLine > other.iStartLine)
            return false;
         if (me.iEndLine == other.iStartLine && me.iEndIndex > other.iStartIndex)
            return false;
         return true;
      }
   }

	public partial class Parser
	{
		public void SetContext(ParseRequest parseRequest)
		{
			this.request = parseRequest;
		   this.currentSpan = null;
			braces = new List<TextSpan[]>();
		}

		ParseRequest request;
	   private TextSpan? currentSpan;
		IList<TextSpan[]> braces;

		public IList<TextSpan[]> Braces
		{
			get { return this.braces; }
		}

		public ParseRequest Request
		{
			get { return this.request; }
		}

		public AuthoringSink Sink
		{
			get { return this.request.Sink; }
		}

		// brace matching, pairs and triples
		public void DefineMatch(int priority, params TextSpan[] locations)
		{			
			if (locations.Length == 2)
				braces.Add(new TextSpan[] { locations[0], 
					locations[1]});

			else if (locations.Length >= 3)
				braces.Add(new TextSpan[] { locations[0], 
					locations[1],
					locations[2]});
		}

      public void DefineMatch(params TextSpan[] locations)
      {
         DefineMatch(0, locations);
      }

      public void StartName(TextSpan textSpan, string name)
      {
         if (request != null)
         {
            if (currentSpan == null)
            {
               currentSpan = textSpan;
               this.Sink.StartName(textSpan, name);
            }
         }
      }

	   public void StartParameters(TextSpan context)
      {
         if (request != null)
         {
            if (currentSpan.HasValue && currentSpan.Value.IsBefore(context))
               this.Sink.StartParameters(context);
         }
      }

      public void NextParameter(TextSpan context)
      {
         if (request != null)
         {
            if (currentSpan.HasValue && currentSpan.Value.IsBefore(context))
               this.Sink.NextParameter(context);
         }
      }

      public void EndParameters(TextSpan context)
      {
         if (request != null)
         {
            currentSpan = null;
            this.Sink.EndParameters(context);
         }
      }

		// hidden regions - not working?
		public void DefineRegion(TextSpan span)
		{
			Sink.AddHiddenRegion(span);
		}

		// auto hidden?
		// public void DefineHiddenRegion
		// etc. see NewHiddenRegion structure


		// error reporting
		public void ReportError(TextSpan span, string message, Severity severity)
		{
			Sink.AddError(request.FileName, message, span, severity);
		}

		#region Error Overloads (Severity)
		public void ReportError(TextSpan location, string message)
		{
			ReportError(location, message, Severity.Error);
		}

		public void ReportFatal(TextSpan location, string message)
		{
			ReportError(location, message, Severity.Fatal);
		}

		public void ReportWarning(TextSpan location, string message)
		{
			ReportError(location, message, Severity.Warning);
		}

		public void ReportHint(TextSpan location, string message)
		{
			ReportError(location, message, Severity.Hint);
		}
		#endregion

		#region TextSpan Conversion
		public TextSpan TextSpan(int startLine, int startIndex, int endIndex)
		{
			return TextSpan(startLine, startIndex, startLine, endIndex);
		}

		public TextSpan TextSpan(int startLine, int startIndex, int endLine, int endIndex)
		{
			TextSpan ts;
			ts.iStartLine = startLine - 1;
			ts.iStartIndex = startIndex;
			ts.iEndLine = endLine - 1;
			ts.iEndIndex = endIndex;
			return ts;
		}
		#endregion
	}
}