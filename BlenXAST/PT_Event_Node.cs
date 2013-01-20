using System;
using System.Collections.Generic;
using System.Text;
using Wintellect.PowerCollections;
using System.Globalization;

namespace Dema.BlenX.Parser
{
   public class PTN_Event : Node
   {
      public PTN_Event(Pos pos, Node condition, Node verb)
         : base(pos)
      {
         childNodes.Add(condition);
         childNodes.Add(verb);
      }

      public PTN_Event_Cond Cond
      {
         get { return (PTN_Event_Cond)childNodes[0]; }
      }

      public PTN_Event_Verb Verb
      {
         get { return (PTN_Event_Verb)childNodes[1]; }
      }

      public override void GenerateText(StringBuilder sb)
      {
         //LWHEN LPOPEN cond LPCLOSE verb LDOTCOMMA  
         sb.Append("when (");
         childNodes[0].GenerateText(sb);
         sb.Append(") ");
         childNodes[1].GenerateText(sb);
         sb.Append(";");
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override Node Clone()
      {
         return new PTN_Event(Position, childNodes[0].Clone(), childNodes[1].Clone());
      }
   }

   public class PTN_Event_Cond : Node
   {
      private EventCond cond;

      public CondType conditionType;
      public string rateOrFun;
      public List<string> originating_entities_names = new List<string>();

      public PTN_Event_Cond(Pos pos, CondType conditionType, Node entity_list, EventCond cond, string rateOrFun)
         : base(pos)
      {
         this.conditionType = conditionType;
         this.cond = cond;
         this.rateOrFun = rateOrFun;

         if (entity_list != null)
         {
            ((PTN_Event_EntityList)entity_list).BuildList(originating_entities_names);
         }
         childNodes.Add(entity_list);
      }

      //TODO!!
      public PTN_Event_Cond(Pos pos, CondType conditionType, Node entity_list, EventCond cond, string paramA, string paramB)
         : base(pos)
      {
         this.conditionType = conditionType;
         this.cond = cond;
         this.rateOrFun = paramA + ", " + paramB;

         if (entity_list != null)
         {
            ((PTN_Event_EntityList)entity_list).BuildList(originating_entities_names);
         }
         childNodes.Add(entity_list);
      }

      //TODO!!
      public PTN_Event_Cond(Pos pos, CondType conditionType, Node entity_list, EventCond cond, Node parameters)
         : base(pos)
      {
         this.conditionType = conditionType;
         this.cond = cond;
         this.rateOrFun = "";

         if (entity_list != null)
         {
            ((PTN_Event_EntityList)entity_list).BuildList(originating_entities_names);
         }
         childNodes.Add(entity_list);
         childNodes.Add(parameters);
      }

      public EventCond GetEventCond() { return cond; }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         childNodes[0].GenerateText(sb);
         sb.Append(" : ");
         if (cond != null)
         {
            cond.GenerateText(sb);
         }
         sb.Append(" : ");

         if (rateOrFun != BlenX.ConstantSymbols.INF_RATE)
            sb.Append(rateOrFun);
         else
            sb.Append(BlenX.ConstantSymbols.INF_RATE); //??
      }

      public override Node Clone()
      {
         return new PTN_Event_Cond(Position, conditionType, childNodes[0].Clone(), cond, rateOrFun);
      }
   }

   public class PTN_Event_Verb : Node
   {
      public VerbType verbType;
      public List<Pair<string, int>> affected_list = new List<Pair<string,int>>();

      public PTN_Event_Verb(Pos pos, string name1, string name2, VerbType verbType)
         : base(pos)
      {
         this.verbType = verbType;
         affected_list.Add(new Pair<string, int>(name1, 1));
         affected_list.Add(new Pair<string, int>(name2, 1));
      }

      public PTN_Event_Verb(Pos pos, string name, int count, VerbType verbType)
         : base(pos)
      {
         this.verbType = verbType;
         affected_list.Add(new Pair<string, int>(name, count));
      }

      public PTN_Event_Verb(Pos pos, int count, VerbType verbType)
         : base(pos)
      {
         this.verbType = verbType;
         affected_list.Add(new Pair<string, int>(BlenX.ConstantSymbols.NO_PROCESS, count));
      }

      public PTN_Event_Verb(Pos pos, VerbType verbType)
         : base(pos)
      {
         this.verbType = verbType;
      }

      public PTN_Event_Verb(Pos pos, string name, VerbType verbType)
         : base(pos)
      {
         this.verbType = verbType;
         affected_list.Add(new Pair<string, int>(name, 1));
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         switch (verbType)
         {
            case VerbType.VERB_SPLIT:
               //LSPLIT LPOPEN betaid LCOMMA betaid LPCLOSE 
               sb.Append("split(");
               sb.Append(affected_list[0].First);
               sb.Append(", ");
               sb.Append(affected_list[1].First);
               sb.Append(")");
               break;

            case VerbType.VERB_NEW:
               //LNEW LPOPEN LDECIMAL LPCLOSE
               sb.Append("new");
               if (affected_list.Count > 0)
               {
                  sb.Append("(");
                  sb.Append(affected_list[0].Second);
                  sb.Append(")");
               }
               break;

            case VerbType.VERB_DELETE:
               //LDELETE LPOPEN LDECIMAL LPCLOSE 
               sb.Append("delete");
               if (affected_list.Count > 0)
               {
                  sb.Append("(");
                  sb.Append(affected_list[0].Second);
                  sb.Append(")");
               }
               break;

            case VerbType.VERB_JOIN:
               //LJOIN LPOPEN betaid LPCLOSE 
               sb.Append("join");
               if (affected_list.Count > 0)
               {
                  sb.Append("(");
                  sb.Append(affected_list[0].First);
                  sb.Append(")");
               }
               break;
              
            case VerbType.VERB_UPDATE:
               //LUPDATE LPOPEN LID LCOMMA LID LPCLOSE 
               sb.Append("update(");
               sb.Append(affected_list[0].First);
               sb.Append(", ");
               sb.Append(affected_list[1].First);
               sb.Append(")");
               break;
         }
      }

      public override Node Clone()
      {
         PTN_Event_Verb newVerb = new PTN_Event_Verb(Position, verbType);
         newVerb.affected_list.AddRange(this.affected_list);
         return newVerb;
      }
   }

   //
   // CONDITIONS
   //
   public class PTN_EventCond_State : NodeListElem<EventState>
   {
      private EventState eventState = new EventState();

      public PTN_EventCond_State(Pos pos, string id, string str_val, StateType stateType)
         : base(pos)
      {
         //TODO: check id! (in generateST?)
          double value = Double.Parse(str_val, CultureInfo.InvariantCulture);
         eventState.entityName = id;
         eventState.stateType = stateType;
         eventState.value = value;
      }

      public PTN_EventCond_State(Pos pos, EventState p_eventState)
         : base(pos)
      {
         //TODO: check id! (in generateST?)
         eventState.entityName = p_eventState.entityName;
         eventState.stateType = p_eventState.stateType;
         eventState.value = p_eventState.value;
      }

      public override void BuildList(ICollection<EventState> list)
      {
         list.Add(eventState);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         eventState.GenerateText(sb);
      }

      public override Node Clone()
      {
         return new PTN_EventCond_State(Position, eventState);
      }
   }

   public class PTN_EventCond_StateList : NodeList<PTN_EventCond_State, EventState>
   {
      public PTN_EventCond_StateList(Pos pos, Node state, Node otherStates)
         : base(pos)
      {
         childNodes.Add(state);
         childNodes.Add(otherStates);
      }

      public PTN_EventCond_StateList(Pos pos, Node state)
         : base(pos)
      {
         childNodes.Add(state);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (childNodes.Count == 1)
            childNodes[0].GenerateText(sb);
         else
         {
            childNodes[0].GenerateText(sb);
            sb.Append(", ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new PTN_EventCond_StateList(Position, childNodes[0].Clone());
         else
            return new PTN_EventCond_StateList(Position, childNodes[0].Clone(), childNodes[1].Clone());

      }
   }

   public class PTN_Event_Entity : NodeListElem<string>
   {
      public string name;

      public PTN_Event_Entity(Pos pos, string id)
         : base(pos)
      {
         name = id;
      }

      public override void BuildList(ICollection<string> list)
      {
         list.Add(name);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append(name);
      }

      public override Node Clone()
      {
         return new PTN_Event_Entity(Position, name);
      }
   }

   public class PTN_Event_EntityList : NodeList<PTN_Event_Entity, string>
   {
      public PTN_Event_EntityList(Pos pos, Node entity, Node otherEntities)
         : base(pos)
      {
         childNodes.Add(entity);
         childNodes.Add(otherEntities);
      }

      public PTN_Event_EntityList(Pos pos, Node entity)
         : base(pos)
      {
         childNodes.Add(entity);
      }

      public override void GenerateText(StringBuilder sb)
      {
         if (childNodes.Count == 1)
            childNodes[0].GenerateText(sb);
         else
         {
            childNodes[0].GenerateText(sb);
            sb.Append(", ");
            childNodes[1].GenerateText(sb);
         }
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new PTN_Event_EntityList(Position, childNodes[0].Clone());
         else
            return new PTN_Event_EntityList(Position, childNodes[0].Clone(), childNodes[1].Clone());
      }
   }
}


