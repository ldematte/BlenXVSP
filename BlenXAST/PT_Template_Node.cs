
using System;
using System.Collections.Generic;
using Wintellect.PowerCollections;

namespace Dema.BlenX.Parser
{
   ////////////////////////////////////////////////
   // CLASS PTN_DEC_TEMP_ELEM DEFINITION
   ////////////////////////////////////////////////

   public class PTN_Dec_Temp_Elem : NodeListElem<PTN_Dec_Temp_Elem>
   {
      private TempType param_type;
      private string id;

      public PTN_Dec_Temp_Elem(Pos pos, TempType p_param_type, string p_id)
         : base(pos)
      {
         param_type = p_param_type;
         id = p_id;
      }

      public override void BuildList(ICollection<PTN_Dec_Temp_Elem> list)
      {
         list.Add(this);
      }

      public TempType GetTempType() { return param_type; }
      public string GetId() { return id; }

      public override void GenerateText(System.Text.StringBuilder sb)
      {
         switch (param_type)
         {
            case TempType.TEMP_NAME:
               sb.Append("name ");
               break;

            case TempType.TEMP_PPROC:
               sb.Append("pproc ");
               break;

            case TempType.TEMP_SEQ:
               sb.Append("prefix ");
               break;

            case TempType.TEMP_RATE:
               sb.Append("rate ");
               break;

            case TempType.TEMP_TYPE:
               sb.Append("binder ");
               break;
         }

         sb.Append(id);
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
         return new PTN_Dec_Temp_Elem(Position, param_type, id);
      }
   }


   ////////////////////////////////////////////////
   // CLASS PTN_DEC_TEMP_LIST DEFINITION
   ////////////////////////////////////////////////

   public class PTN_Dec_Temp_List : NodeList<PTN_Dec_Temp_Elem, PTN_Dec_Temp_Elem>
   {
      public PTN_Dec_Temp_List(Pos pos, Node rate)
         : base(pos)
      {
         childNodes.Add(rate);
      }

      public PTN_Dec_Temp_List(Pos pos, Node rate, Node rateList)
         : base(pos)
      {
         childNodes.Add(rate);
         childNodes.Add(rateList);
      }

      public override void GenerateText(System.Text.StringBuilder sb)
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
            return new PTN_Dec_Temp_List(Position, childNodes[0]);
         else
            return new PTN_Dec_Temp_List(Position, childNodes[0], childNodes[1]);
      }
   }



   ////////////////////////////////////////////////
   // CLASS PTN_TEMPLATE DEFINITION
   ////////////////////////////////////////////////

   public class PTN_Template : Node
   {
      private EntityType temp_type;
      private string id;

      public List<PTN_Dec_Temp_Elem> DecList = new List<PTN_Dec_Temp_Elem>();

      public PTN_Template(Pos pos, EntityType p_temp_type, string p_id, Node dec_temp_list, Node par_or_sum)
         : base(pos)
      {
         id = p_id;
         temp_type = p_temp_type;
         childNodes.Add(dec_temp_list);
         childNodes.Add(par_or_sum);

         ((PTN_Dec_Temp_List)dec_temp_list).BuildList(DecList);
      }

      public override void GenerateText(System.Text.StringBuilder sb)
      {
         sb.Append("template ");
         sb.Append(id);
         sb.Append(" : ");

         switch (temp_type)
         {
            case EntityType.BPROC:
               sb.Append("bproc");
               break;

            case EntityType.PPROC:
               sb.Append("pproc");
               break;
         }
         sb.Append(" << ");
         childNodes[0].GenerateText(sb);
         sb.Append(" >> = ");
         childNodes[1].GenerateText(sb);
         sb.AppendLine(";");
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
         return new PTN_Template(Position, temp_type, id, childNodes[0], childNodes[1]);
      }
   }

   ////////////////////////////////////////////////
   // CLASS PTN_DEC_TEMP_ELEM DEFINITION
   ////////////////////////////////////////////////

   public class PTN_Inv_Temp_Elem : NodeListElem<PTN_Inv_Temp_Elem>
   {
      private string id;
      private BinderState state;

      public PTN_Inv_Temp_Elem(Pos pos, BinderState p_state, string p_id) //NAME_TEMPLATE_REF
         : base(pos)
      {
         state = p_state;
         id = p_id;
      }

      public PTN_Inv_Temp_Elem(Pos pos, BinderState p_state, string p_id, Node inner_template)
         : base(pos)
      {
         state = p_state;
         id = p_id;
         childNodes.Add(inner_template);
      }


      public BinderState Get_Type() { return state; }
      public string Get_Id() { return id; }

      public string GetLabel()
      {
         string label = id + ":" + state.ToString();
         if (childNodes.Count > 0)
         {
            label = label + "<<" + ((PTN_PiInv_Template)childNodes[0]).GetLabel() + ">>";
         }
         return label;
      }


      public override void BuildList(ICollection<PTN_Inv_Temp_Elem> list)
      {
         list.Add(this);
      }

      public override void Accept(INodeVisitor visitor)
      {
         if (visitor.Visit(this))
         {
            foreach (Node childNode in childNodes)
               childNode.Accept(visitor);
         }
      }

      public override void GenerateText(System.Text.StringBuilder sb)
      {
         if (state == BinderState.STATE_NOT_SPECIFIED)
         {
            // recursive template invocation has a child.
            // In this case, do not print the id: the PTN_PiInv_Template
            // will print it
            if (childNodes.Count > 0)
               childNodes[0].GenerateText(sb);
            else 
               // in all other cases, print it and continue
               sb.Append(id);
         }
         else
         {
            //LPOPEN LID LCOMMA LSTATEUNHIDE LPCLOSE
            sb.Append("(");
            sb.Append(id);
            sb.Append(", ");
            switch (state)
            {
               case BinderState.STATE_BOUND:
                  sb.Append("bound");
                  break;

               case BinderState.STATE_HIDDEN:
                  sb.Append("hidden");
                  break;

               case BinderState.STATE_UNHIDDEN:
                  sb.Append("unhidden");
                  break;
            }
            sb.Append(")");
         }         
      }

      public override Node Clone()
      {
         throw new NotImplementedException();
      }
   }



   ////////////////////////////////////////////////
   // CLASS PTN_INV_TEMP_LIST DEFINITION
   ////////////////////////////////////////////////

   public class PTN_Inv_Temp_List : NodeList<PTN_Inv_Temp_Elem, PTN_Inv_Temp_Elem>
   {
      public PTN_Inv_Temp_List(Pos pos, Node node)
         : base(pos)
      {
         childNodes.Add(node);
      }

      public PTN_Inv_Temp_List(Pos pos, Node node, Node nodeList)
         : base(pos)
      {
         childNodes.Add(node);
         childNodes.Add(nodeList);
      }

      public override void GenerateText(System.Text.StringBuilder sb)
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
         throw new NotImplementedException();
      }
   }


   ////////////////////////////////////////////////
   // CLASS PTN_PIINV_TEMPLATE DEFINITION
   ////////////////////////////////////////////////

   public class PTN_PiInv_Template : Node
   {
      private string id;
      public PTN_PiInv_Template(Pos pos, string p_id, Node node)
         : base(pos)
      {
         id = p_id;
         childNodes.Add(node);
      }

      public string GetLabel()
      {
         string label = id;
         Set<PTN_Inv_Temp_Elem> elem_list = new Set<PTN_Inv_Temp_Elem>();
         ((PTN_Inv_Temp_List)childNodes[0]).BuildList(elem_list);

         foreach (PTN_Inv_Temp_Elem elem in elem_list)
         {
            label = label + ":" + elem.GetLabel();
         }
         return label;
      }

      public override void GenerateText(System.Text.StringBuilder sb)
      {
         //LID LDAOPEN inv_temp_list LDACLOSE               

         sb.Append(id);
         sb.Append("<< ");
         childNodes[0].GenerateText(sb);
         sb.Append(" >>");
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
         throw new NotImplementedException();
      }
   }

   ////////////////////////////////////////////////
   // CLASS PTN_PIINV_TEMPLATE DEFINITION
   ////////////////////////////////////////////////
   public class PTN_BpInv_Template : Node
   {
      private string templateId;
      private string number;
      private string bprocId;

      private string BuildTemplateName(List<PTN_Inv_Temp_Elem> InvList)
      {
         string template_name = templateId + "<<";

         bool first = true;
         foreach (PTN_Inv_Temp_Elem elem in InvList)
         {
            if (first == true)
            {
               template_name = template_name + elem.GetLabel();
               first = false;
            }
            else
            {
               template_name = template_name + ":" + elem.GetLabel();
            }
         }

         template_name = template_name + ">>";
         return template_name;
      }

      //contructor for "run list" style: template instantiation inline bound to a number 
      public PTN_BpInv_Template(Pos pos, string p_templateId, string p_number, Node node)
         : base(pos)
      {
         templateId = p_templateId;
         bprocId = BlenX.ConstantSymbols.NO_PROCESS;
         number = p_number;
         childNodes.Add(node);
      }

      //constructor for "namespace binding" style: create a "shortcut" for referring to that 
      //process
      public PTN_BpInv_Template(Pos pos, string p_templateId, Node node, string p_betaId)
         : base(pos)
      {
         templateId = p_templateId;
         bprocId = p_betaId;
         childNodes.Add(node);
      }

      public override void GenerateText(System.Text.StringBuilder sb)
      {
         //LID LDAOPEN inv_temp_list LDACLOSE LDOTCOMMA

         //if (bprocId == Env.Constants.NO_PROCESS)
         sb.Append(templateId);
         sb.Append("<< ");
         childNodes[0].GenerateText(sb);
         sb.AppendLine(" >>");
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
         throw new NotImplementedException();
      }
   }
}
