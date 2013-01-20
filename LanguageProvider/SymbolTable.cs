using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Babel.ParserGenerator;
using Dema.BlenX.Parser;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace Dema.BlenX.VisualStudio
{
   public enum SymbolType
   {
      Process,
      Box,
      Complex,
      Event,
      Sequence,
      ProcessTemplate,
      BoxTemplate,
      BinderIdentifier,
      Binder,
      Function,
      Constant,
      Var
   }

   [Flags]
   public enum BlenXScopeType
   {
      InsideBox = 1,
      InsidePi = 2,
      InsideBinderList = 4,
      InsideBoxList = 8,
      InsideParams = 16,
      InsideNothing = 32,
      InsideIf = 64,
      InsideFunc = 128
   }

   public struct ScopeInfo
   {
      public string Id;
      public TextSpan ScopeSpan;
      public string FileName;
      public BlenXScopeType ScopeType;

      public static ScopeInfo Empty = new ScopeInfo { ScopeType = BlenXScopeType.InsideNothing };
   }

   public class SymbolTable
   {

      internal TextSpan MkTSpan(LexLocation s)
      {
         return new TextSpan { iStartLine = s.sLin, iStartIndex = s.sCol, iEndLine = s.eLin, iEndIndex = s.eCol };
      }

      public Dictionary<string, TextSpan> SymbolToLocation = new Dictionary<string, TextSpan>();
      public Dictionary<string, string> SymbolToFile = new Dictionary<string, string>();
      public Dictionary<string, SymbolType> SymbolToType = new Dictionary<string, SymbolType>();
      public Dictionary<string, List<string>> BProcToBinders = new Dictionary<string, List<string>>();
      public List<ScopeInfo> Scopes = new List<ScopeInfo>();

      private Dictionary<string, List<PTN_Dec_Temp_Elem>> templateSignature = new Dictionary<string, List<PTN_Dec_Temp_Elem>>();

      public void RemoveFile(string fileName)
      {
         var symbolsToRemove = SymbolToFile.Where(kv => kv.Value == fileName).Select(kv => kv.Key).ToList();
         foreach (var symbol in symbolsToRemove)
         {
            bool retval;
            retval = SymbolToLocation.Remove(symbol);
            Debug.Assert(retval);
            retval = SymbolToType.Remove(symbol);
            Debug.Assert(retval);
            retval = SymbolToFile.Remove(symbol);
            Debug.Assert(retval);

            //it may be a template
            if (templateSignature.ContainsKey(symbol))
               templateSignature.Remove(symbol);

            Scopes.RemoveAll(e => e.FileName == fileName);
         }
      }

      private void AddSymbol(LexLocation loc, string fileName, string id,
         LexLocation scopeStart, LexLocation scopeEnd, BlenXScopeType scopeType,
         SymbolType symbolType)
      {
         TextSpan symbolSpan = MkTSpan(loc);
         SymbolToLocation.Add(id, symbolSpan);

         TextSpan scopeSpan = new TextSpan { iStartLine = scopeStart.sLin, iStartIndex = scopeStart.sCol, iEndLine = scopeEnd.eLin, iEndIndex = scopeEnd.eCol };

         Scopes.Add(new ScopeInfo { ScopeSpan = scopeSpan, Id = id, FileName = fileName, ScopeType = scopeType });
         SymbolToFile.Add(id, fileName);
         SymbolToType.Add(id, symbolType);
      }

      public void AddPProc(LexLocation loc, string fileName, string id, LexLocation scopeStart, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, scopeStart, scopeEnd, BlenXScopeType.InsidePi, SymbolType.Process);
      }

      public void AddBProc(LexLocation loc, string fileName, string id, TextSpan? bindersSpan, LexLocation scopeStart, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, scopeStart, scopeEnd, BlenXScopeType.InsideBox, SymbolType.Box);
         if (bindersSpan.HasValue)
            Scopes.Add(new ScopeInfo { ScopeSpan = bindersSpan.Value, Id = id, FileName = fileName, ScopeType = BlenXScopeType.InsideBinderList });

         // add binders
         BProcToBinders[id] = tempBinderList.Select(v => v.First).ToList();
         tempBinderList = new List<Pair<string, LexLocation>>();
      }

      //incomplete version
      public void AddBProc(LexLocation loc, string fileName, string id, TextSpan bindersSpan)
      {
         AddSymbol(loc, fileName, id, loc, loc, BlenXScopeType.InsideBox, SymbolType.Box);
         Scopes.Add(new ScopeInfo { ScopeSpan = bindersSpan, Id = id, FileName = fileName, ScopeType = BlenXScopeType.InsideBinderList });

         // add binders
         BProcToBinders[id] = tempBinderList.Select(v => v.First).ToList();
         tempBinderList = new List<Pair<string, LexLocation>>();
      }

      public void AddMolecule(LexLocation loc, string fileName, string id, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, loc, scopeEnd, BlenXScopeType.InsideBoxList, SymbolType.Complex);
      }

      public void AddSequence(LexLocation loc, string fileName, string id, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, loc, scopeEnd, BlenXScopeType.InsidePi, SymbolType.Complex);
      }

      public void AddEvent(LexLocation scopeStart, string fileName, LexLocation scopeEnd)
      {
         TextSpan scopeSpan = new TextSpan { iStartLine = scopeStart.sLin, iStartIndex = scopeStart.sCol, iEndLine = scopeEnd.eLin, iEndIndex = scopeEnd.eCol };
         Scopes.Add(new ScopeInfo { ScopeSpan = scopeSpan, Id = "Event", FileName = fileName, ScopeType = BlenXScopeType.InsideBoxList });
      }

      public void AddTemplatePProc(LexLocation loc, string fileName, string id, PTN_Dec_Temp_List tempList, LexLocation scopeStart, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, scopeStart, scopeEnd, BlenXScopeType.InsidePi, SymbolType.ProcessTemplate);
         List<PTN_Dec_Temp_Elem> paramList = new List<PTN_Dec_Temp_Elem>();
         tempList.BuildList(paramList);
         templateSignature[id] = paramList;
      }

      public void AddTemplateBProc(LexLocation loc, string fileName, string id, PTN_Dec_Temp_List tempList,
         LexLocation scopeStart, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, scopeStart, scopeEnd, BlenXScopeType.InsideBox, SymbolType.BoxTemplate);
         List<PTN_Dec_Temp_Elem> paramList = new List<PTN_Dec_Temp_Elem>();
         tempList.BuildList(paramList);
         templateSignature[id] = paramList;

         // add binders
         BProcToBinders[id] = tempBinderList.Select(v => v.First).ToList();
         tempBinderList = new List<Pair<string, LexLocation>>();
      }

      public void AddBinderIdentifier(LexLocation loc, string fileName, string id)
      {
         AddSymbol(loc, fileName, id, loc, loc, BlenXScopeType.InsideNothing, SymbolType.BinderIdentifier);
      }

      public void AddFunction(LexLocation loc, string fileName, string id, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, loc, scopeEnd, BlenXScopeType.InsideFunc, SymbolType.Function);
      }

      public void AddConstant(LexLocation loc, string fileName, string id, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, loc, scopeEnd, BlenXScopeType.InsideFunc, SymbolType.Constant);
      }

      public void AddVar(LexLocation loc, string fileName, string id, LexLocation scopeEnd)
      {
         AddSymbol(loc, fileName, id, loc, scopeEnd, BlenXScopeType.InsideFunc, SymbolType.Var);
      }

      public List<Pair<string, LexLocation>> tempBinderList = new List<Pair<string, LexLocation>>();

      public void AddBinder(LexLocation loc, string fileName, string id, string type)
      {
         tempBinderList.Add(new Pair<string, LexLocation>(id, loc));
      }

      public void AddAffinity(LexLocation loc, string fileName, string id1, string id2)
      {
         //todo
      }

      public IEnumerable<string> GetSymbols(SymbolType symbolType)
      {
         var symbols = SymbolToType.Where(kv => kv.Value == symbolType).Select(kv => kv.Key);
         return symbols;
      }

      public IList<PTN_Dec_Temp_Elem> GetTemplateArguments(string name)
      {
         Debug.Assert(TypeOf(name) == SymbolType.ProcessTemplate ||
            TypeOf(name) == SymbolType.BoxTemplate);

         return templateSignature[name];
      }

      public bool HasSymbol(string symbolName)
      {
         return SymbolToType.ContainsKey(symbolName);
      }

      public SymbolType TypeOf(string symbolName)
      {
         return SymbolToType[symbolName];
      }

      public ScopeInfo FindScope(int line, int col)
      {
         //correction on line numbering
         ++line;
         foreach (var scopeInfo in Scopes)
         {
            if (scopeInfo.ScopeSpan.iStartLine == line && line == scopeInfo.ScopeSpan.iEndLine)
            {
               if (scopeInfo.ScopeSpan.iStartIndex <= col && col <= scopeInfo.ScopeSpan.iEndIndex)
               {
                  return scopeInfo;
               }
            }
            else if (scopeInfo.ScopeSpan.iStartLine <= line && line <= scopeInfo.ScopeSpan.iEndLine)
            {
               return scopeInfo;
            }
         }

         return ScopeInfo.Empty;
      }

      internal IEnumerable<string> GetBinders(ScopeInfo scope)
      {
         Debug.Assert(TypeOf(scope.Id) == SymbolType.Box ||
                      TypeOf(scope.Id) == SymbolType.BoxTemplate);

         return BProcToBinders[scope.Id];

         //return SymbolToLocation.Where((kv) =>
         //   {
         //      if (scope.ScopeSpan.iStartLine == kv.Value.iStartLine && kv.Value.iStartLine == scope.ScopeSpan.iEndLine)
         //      {
         //         if (scope.ScopeSpan.iStartIndex <= kv.Value.iStartIndex && kv.Value.iEndIndex <= scope.ScopeSpan.iEndIndex)
         //            return true;
         //      }
         //      else if (scope.ScopeSpan.iStartLine <= kv.Value.iStartLine && kv.Value.iEndIndex <= scope.ScopeSpan.iEndLine)
         //      {
         //         return true;
         //      }
         //      return false;
         //   }).Select((kv) => kv.Key);
      }
   }

   public class Pair<T0, T1>
   {
      private T0 first;
      private T1 second;

      public Pair(T0 first, T1 second)
      {
         this.first = first;
         this.second = second;
      }

      public T0 First
      {
         get { return first; }
         set { first = value; }
      }

      public T1 Second
      {
         get { return second; }
         set { second = value; }
      }
   }
}
