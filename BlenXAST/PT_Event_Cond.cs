using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Dema.BlenX.Parser
{
   public struct EventState
   {
      public double value;
      public StateType stateType;
      public string entityName;
      //Entity* entityRef;

      public void GenerateText(StringBuilder sb)
      {
         sb.Append(entityName);
         switch (stateType)
         {
            case StateType.STATE_UP:
               sb.Append(" -> ");
               break;

            case StateType.STATE_DOWN:
               sb.Append(" <- ");
               break;
         }
         sb.Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
      }
   }

   //Base class for conditions
   public abstract class EventCond
   {
      protected Pos pos;

      public CondType condType;

      public EventCond(Pos p_pos, CondType p_type)
      {
         pos = p_pos;
         condType = p_type;
      }

      Pos GetPos() { return pos; }

      //virtual bool Init(Event& my_event) = 0;
      //virtual bool CanFire(Event& my_event) = 0;

      //virtual void GetEntityList(std::vector<Entity*>& list) = 0;
      //virtual void GetVarList(std::vector<Entity*>& list) = 0;

      //virtual EventCond* Clone() = 0;

      //size_t GetEntityCount();

      public abstract void GenerateText(StringBuilder sb);
   }

   public class EventCond_AtomStates : EventCond
   {
      private List<EventState> eventStates;

      public EventCond_AtomStates(Pos pos, CondType condType, Node state_list_node)
         : base(pos, condType)
      {
         this.condType = condType;

         PTN_EventCond_StateList stateList = (PTN_EventCond_StateList)state_list_node;
         eventStates = new List<EventState>();
         stateList.BuildList(eventStates);
      }

      public EventCond_AtomStates(Pos pos, CondType condType, List<EventState> states)
         : base(pos, condType)
      {
         eventStates = states;
      }

      public override void GenerateText(StringBuilder sb)
      {
         foreach (EventState e in eventStates)
            e.GenerateText(sb);
      }
   }


   public class EventCond_AtomCount : EventCond
   {
      private string entityName;
      //Entity* entityRef;
      private int entityValue;

      public EventCond_AtomCount(Pos pos, CondType condType) : base(pos, condType) { }

      public EventCond_AtomCount(Pos pos, CondType condType, string id, string value)
         : base(pos, condType)
      {
         entityName = id;
         entityValue = Int32.Parse(value);
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("|");
         sb.Append(entityName);
         sb.Append("|");
         switch (condType)
         {
            case CondType.COND_COUNT_GREATER:
               sb.Append(">");
               break;

            case CondType.COND_COUNT_LESS:
               sb.Append("<");
               break;

            case CondType.COND_COUNT_EQUAL:
               sb.Append("=");
               break;

            case CondType.COND_COUNT_NEQUAL:
               sb.Append("!=");
               break;
         }
         sb.Append(entityValue);
      }      
   }

   public class EventCond_AtomDet : EventCond
   {
      private double param;
      private EventCond_AtomDet(Pos pos, CondType condType) : base(pos, condType) { }

      public
         EventCond_AtomDet(Pos pos, CondType condType, string value)
         : base(pos, condType)
      {
          param = Double.Parse(value, CultureInfo.InvariantCulture);
      }

      public override void GenerateText(StringBuilder sb)
      {
         switch (condType)
         {
            case CondType.COND_STEP_EQUAL:
               sb.Append("step = ");
               sb.Append(((int)param).ToString());
               break;

            case CondType.COND_TIME_EQUAL:
               sb.Append("time = ");
               sb.Append(param.ToString("#0.0000", System.Globalization.CultureInfo.InvariantCulture));
               break;
         }         
      }
   }

   public class EventCond_And : EventCond
   {
      private EventCond left;
      private EventCond right;

      public EventCond_And(Pos pos, EventCond p_left, EventCond p_right)
         : base(pos, CondType.COND_EXPRESSION)
      {
         left = p_left;
         right = p_right;
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("(");
         left.GenerateText(sb);
         sb.Append(" and ");
         right.GenerateText(sb);
         sb.Append(")");
      }
   }

   public class EventCond_Or : EventCond
   {
      private EventCond left;
      private EventCond right;

      public EventCond_Or(Pos pos, EventCond p_left, EventCond p_right)
         : base(pos, CondType.COND_EXPRESSION)
      {
         left = p_left;
         right = p_right;
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("(");
         left.GenerateText(sb);
         sb.Append(" or ");
         right.GenerateText(sb);
         sb.Append(")");
      }

   }

   public class EventCond_Not : EventCond
   {
      private EventCond cond;

      public EventCond_Not(Pos pos, EventCond p_cond)
         : base(pos, CondType.COND_EXPRESSION)
      {
         cond = p_cond;
      }

      public override void GenerateText(StringBuilder sb)
      {
         sb.Append("(not ");
         cond.GenerateText(sb);
         sb.Append(")");
      }
   }
}


