using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Dema.BlenX.VisualStudio;

namespace Babel
{
   public class Resolver : Babel.IASTResolver
   {
      public IList<Babel.Declaration> FindCompletions(object result, int line, int col)
      {
         var declList =  new List<Babel.Declaration>();

         //TODO: use the map and initials
         foreach (var k in Babel.Lexer.Scanner.piKeywords)
            declList.Add(new Declaration(k.Description, k.Id, 18, k.Id));

         return declList;
      }

      public IList<Babel.Declaration> FindMembers(object result, int line, int col)
      {
         var members = new List<Babel.Declaration>();

         var parser = result as Babel.Parser.Parser;
         if (parser != null)
         {
            SymbolTable st = parser.SymbolTable;
            var scope = st.FindScope(line, col);
            //BlenXScopeType scopeType = parser.CurrentScope; 

            switch (scope.ScopeType)
            {
               case BlenXScopeType.InsideBox: //PiScope + Binders
                  {
                     AddPiScope(st, members);
                     foreach (var v in st.GetBinders(scope))
                        members.Add(new Declaration(v, v, 41, v));
                  }
                  break;

               case BlenXScopeType.InsidePi:
                  {
                     AddPiScope(st, members);
                  }
                  break;

               case BlenXScopeType.InsideBinderList:
                  {
                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.BinderIdentifier))
                        members.Add(new Declaration(v, v, 41, v));
                  }
                  break;

               case BlenXScopeType.InsideBoxList: //LRUN, LENTITY...
                  {
                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.Box))
                        members.Add(new Declaration(v, v, 0, v));

                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.BoxTemplate))
                        members.Add(new Declaration(v, v, 2, v));
                  }
                  break;

               case BlenXScopeType.InsideParams:
                  {
                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.Process))
                        members.Add(new Declaration(v, v, 26, v));
                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.BinderIdentifier))
                        members.Add(new Declaration(v, v, 41, v));
                  }
                  break;

               case BlenXScopeType.InsideFunc:
                  {
                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.Constant))
                        members.Add(new Declaration(v, v, 6, v));
                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.Var))
                        members.Add(new Declaration(v, v, 37, v));
                     foreach (var v in parser.SymbolTable.GetSymbols(SymbolType.Function))
                        members.Add(new Declaration(v, v, 41, v));
                  }
                  break;

               case BlenXScopeType.InsideNothing:
                  break;
            }

            // add keywords as well!
            foreach (var k in Babel.Lexer.Scanner.piKeywords)
               if ((k.ValidScope & scope.ScopeType) == scope.ScopeType)
                  members.Add(new Declaration(k.Description, k.Id, 18, k.Id));

         }
         members.Sort((x, y) => x.DisplayText.CompareTo(y.DisplayText));
         return members;
      }

      private static void AddPiScope(SymbolTable st, List<Declaration> members)
      {
         foreach (var v in st.GetSymbols(SymbolType.Process))
            members.Add(new Declaration(v, v, 26, v));
         
         //members.Add(new Declaration("Choice operator", "+", 6, "+"));
         //members.Add(new Declaration("Parallel operator", "|", 6, "|"));
      }

      public string FindQuickInfo(object result, int line, int col)
      {
         return "unknown";
      }

      public IList<Babel.Method> FindMethods(object result, int line, int col, string name)
      {
         var methods = new List<Babel.Method>();
         var parser = result as Parser.Parser;
         if (parser == null)
            return methods;

         SymbolTable st = parser.SymbolTable;

         

         if (st.HasSymbol(name))
         {
            var symbolType = st.TypeOf(name);
            switch (symbolType)
            {

               case SymbolType.BoxTemplate:
               case SymbolType.ProcessTemplate:
                  {
                     var parameters = new List<Parameter>();
                     var signature = st.GetTemplateArguments(name);
                     foreach (var elem in signature)
                     {
                        parameters.Add(new Parameter()
                                          {
                                             Description =
                                                "A template argument of type " + elem.GetTempType(),
                                             Display = elem.GetId() + ": " + elem.GetTempType(),
                                             Name = elem.GetId()
                                          });
                     }
                     methods.Add(new Method()
                                    {
                                       Description = name,
                                       Name = name,
                                       Parameters = parameters
                                    });
                  }
                  break;
            }
         }
         else
         {
            if (specialParameters.ContainsKey(name))
               return specialParameters[name];
            else
            {
               //it must be a channel
               return MakeSingleOption(
                              "channel",
                              name,
                              MakeParamList(new Parameter[]
                                               {
                                                  new Parameter()
                                                     {
                                                        Display = "Name",
                                                        Description = "The (optional) name to be sent over this channel",
                                                        Name = "Name"
                                                     }
                                               }
                                 ));
            }
         }
         return methods;
      }

      private static Dictionary<string, List<Method>> specialParameters = new Dictionary<string, List<Method>>();

      static Resolver()
      {
         specialParameters.Add("Event", 
            MakeSingleOption(
              "Event",
              "",
              MakeParamList(new Parameter[] { 
               new Parameter() { Display ="Entity List", Description = "The list of entities involved", Name ="Entity List"},
               new Parameter() { Display ="Condition", Description = "A boolean condition" },
               new Parameter() { Display ="Rate", Description = "The rate of this event" }
              }
            ))
         );

         specialParameters.Add("rate",
            MakeSingleOption(
              "rate",
              "",
              MakeParamList(new Parameter[] { 
               new Parameter() { Display ="Rate constant", Description = "A rate constant defined in the func file" }
              }
            ))
         );

         specialParameters.Add("die",
           MakeSingleOption(
             "Die action",
             "die",
             MakeParamList(new Parameter[] { 
               new Parameter() { Display ="Rate", Description = "The rate of this event" }
              }
           ))
        );

         specialParameters.Add("#",
           MakeMultipleOption(new Method[] {
               new Method {
                  Description = "Binder definition",
                  Name = "#",
                  Parameters = MakeParamList(new Parameter[] {
                  new Parameter() { Display ="Id", Description = "The binder subject", Name="Id"}, 
                  new Parameter() { Display ="Rate", Description = "The INTRA rate for the binder name" },
                  new Parameter() { Display ="BinderId", Description = "The binder identifier", Name="BinderId"}

               })},
               new Method {
                  Description = "Binder definition",
                  Name = "#",
                  Parameters = MakeParamList(new Parameter[] {
                  new Parameter() { Display ="Id", Description = "The binder subject", Name="Id"}, 
                  new Parameter() { Display ="BinderId", Description = "The binder identifier", Name="BinderId"}
               })}})
        );

         specialParameters.Add("hide",
            MakeMultipleOption(new Method[] {
               new Method {
                  Description = "Hide action",
                  Name = "hide",
                  Parameters = MakeParamList(new Parameter[] { 
                  new Parameter() { Display ="Rate", Description = "The rate of this event" },
                  new Parameter() { Display ="Id", Description = "The binder to hide", Name="Id"}
               })},
               new Method {
                  Description = "Hide action",
                  Name = "hide",
                  Parameters = MakeParamList(new Parameter[] {
                  new Parameter() { Display ="Id", Description = "The binder to hide", Name="Id"}
               })}})
         );

         specialParameters.Add("unhide",
           MakeMultipleOption(new Method[] {
               new Method {
                  Description = "Unhide action",
                  Name = "unhide",
                  Parameters = MakeParamList(new Parameter[] { 
                  new Parameter() { Display ="Rate", Description = "The rate of this event" },
                  new Parameter() { Display ="Id", Description = "The binder to unhide", Name="Id"}
               })},
               new Method {
                  Description = "Unhide action",
                  Name = "unhide",
                  Parameters = MakeParamList(new Parameter[] {
                  new Parameter() { Display ="Id", Description = "The binder to unhide", Name="Id"}
               })}})
        );

         specialParameters.Add("ch",
           MakeMultipleOption(new Method[] {
               new Method {
                  Description = "Change action",
                  Name = "ch",
                  Parameters = MakeParamList(new Parameter[] { 
                  new Parameter() { Display ="Rate", Description = "The rate of this event" },
                  new Parameter() { Display ="Id", Description = "The binder subject", Name="Id"},
                  new Parameter() { Display ="BinderId", Description = "The new binder identifier", Name="BinderId"}
               })},
               new Method {
                  Description = "Change action",
                  Name = "ch",
                  Parameters = MakeParamList(new Parameter[] {
                  new Parameter() { Display ="Id", Description = "The binder subject", Name="Id"},
                  new Parameter() { Display ="BinderId", Description = "The new binder identifier", Name="BinderId"}
               })}})
        );
      }

      private static List<Method> MakeMultipleOption(Method[] methods)
      {
         return new List<Method>(methods);
      }
    
      private static List<Method> MakeSingleOption(string description, string name, IList<Parameter> parameters)
      {
         var list =  new List<Method>();
         list.Add(new Method { Description = description, Name = name, Parameters = parameters });
         return list;
      }

      private static IList<Parameter> MakeParamList(Parameter[] parameters)
      {
         return new List<Parameter>(parameters);
      }
   }

   //class SpecialMethodInfo :
   //{
   //   public List<Parameter> Parameters = new List<Parameter>();
   //   public 
   //}

}
