using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace Dema.BlenX.Parser
{
   //
   //Pi nodes
   //

   public abstract class PiProcess : Node
   {
      public PiProcess(Pos pos)
         : base(pos) { }

      //public abstract PiProcess Clone();      

      public override string ToString()
      {
         StringBuilder sb = new StringBuilder();
         this.GenerateText(sb);
         return sb.ToString();
      }
   }

   public abstract class PiBinaryOperator : PiProcess
   {
      public PiBinaryOperator(Pos pos)
         : base(pos) { }

      // puts the tree in a "normal" form, with all the children as a 
      // binary tree
      // es:
      //  P1 | P2 | (P3 | P4) 
      //  is represented as 
      // 
      //    par
      //  /  |  \
      // P1 P2  par
      //       P3 P4
      //
      // and must become
      //  par
      // p1  par
      //    p2  par
      //       p3 p4
      // NOTE: for now we do only one level
      // es:
      // P1 | (P2 | P3) | P4
      //
      // will become
      //  par
      // p1  par
      //    par  p4
      //   p2 p3 
      public void Normalize() 
      {
         if (Children.Count > 2)
         {
            var tempNodeList = new Queue<Node>(Children.Skip(1));
            //this.Children[0] remais as it is
            var currentNode = this;
            while (tempNodeList.Count > 2)
            {
               var node2 = tempNodeList.Dequeue();
               var opNode = Create();
               currentNode.Children[1] = opNode;
               opNode.Children[0] = node2;
               currentNode = opNode;
            }
            currentNode.Children[0] = tempNodeList.Dequeue();
            currentNode.Children[0] = tempNodeList.Dequeue();
            Debug.Assert(tempNodeList.Count == 0);
         }          
      }

      // TODO: we need it more generally? Or is it enough to have it here?
      public abstract PiBinaryOperator Create();
   }

   public class PiChoice : PiBinaryOperator
   {
      public PiChoice()
         : base(Pos.Empty)
      { }

      public PiChoice(Pos pos, Node seqOrSumlist, Node seq2)
         : base(pos)
      {
         childNodes.Add(seqOrSumlist);
         childNodes.Add(seq2);
      }

      public override PiBinaryOperator Create()
      {
         return new PiChoice();
      }

      public List<string> GetProcesses()
      {
         List<string> retval = new List<string>();

         foreach (Node pi in childNodes)
         {
            if (pi is PiChoice)
            {
               PiChoice choice = (PiChoice)childNodes[1];
               retval.AddRange(choice.GetProcesses());
            }
            else
            {
               retval.Add(pi.ToString());
            }
         }

         return retval;
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
         switch (childNodes.Count)
         {
            case 0:
               break;
            case 1:
               childNodes[0].GenerateText(sb);
               break;
            default:
               sb.Append("( ");
               childNodes[0].GenerateText(sb);
               for (int i = 1; i < childNodes.Count; ++i)
               {
                  sb.Append(" + ");
                  childNodes[i].GenerateText(sb);
               }
               sb.AppendLine(" )");
               break;
         }
      }

      public override Node Clone()
      {
         return new PiChoice(Position, ((PiProcess)childNodes[0]).Clone(), ((PiProcess)childNodes[1]).Clone());
      }
   }

   public class PiParallel : PiBinaryOperator
   {
      public PiParallel() : base(Pos.Empty) { }

      public PiParallel(Pos pos, Node piprocess1, Node piprocess2)
         : base(pos)
      {
         childNodes.Add(piprocess1);
         childNodes.Add(piprocess2);
      }

      public override PiBinaryOperator Create()
      {
         return new PiParallel();
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
         switch (childNodes.Count)
         {
            case 0:
               break;
            case 1:
               childNodes[0].GenerateText(sb);
               break;
            default:
               sb.Append("( ");
               childNodes[0].GenerateText(sb);
               for (int i = 1; i < childNodes.Count; ++i)
               {
                  sb.Append(" | ");
                  childNodes[i].GenerateText(sb);
               }
               sb.AppendLine(" )");
               break;
         }
      }

      public override Node Clone()
      {
         return new PiParallel(Position, ((PiProcess)childNodes[0]).Clone(), ((PiProcess)childNodes[1]).Clone());
      }
   }

   public class PiAction : PiProcess
   {
      public PiAction(Pos pos, Node action, Node piprocess)
         : base(pos)
      {
         childNodes.Add(action);
         childNodes.Add(piprocess);
      }

      public PiAction(Pos pos, Node action)
         : base(pos)
      {
         childNodes.Add(action);
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
         childNodes[0].GenerateText(sb);
         if (childNodes.Count > 1)
         {
            sb.Append(".");
            childNodes[1].GenerateText(sb);
         }
      }

      public override Node Clone()
      {
         if (childNodes.Count == 1)
            return new PiAction(Position, ((PiProcess)childNodes[0]).Clone());
         else
            return new PiAction(Position, ((PiProcess)childNodes[0]).Clone(), ((PiProcess)childNodes[1]).Clone());
      }
   }

   public class PiNil : PiProcess
   {
      public PiNil(Pos pos)
         : base(pos)
      { }

      public PiNil()
         : base(Pos.Empty)
      { }

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
         sb.Append("nil");
      }

      public override Node Clone()
      {
         return new PiNil(Position);
      }
   }

   public class PiId : PiProcess
   {
      string id;
      public string ID
      {
         get { return id; }
         set { id = value; }
      }

      public PiId(Pos pos, string id)
         : base(pos)
      {
         this.id = id;
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
         sb.Append(id);
      }

      public override Node Clone()
      {
         return new PiId(Position, id);
      }
   }

   public class PiReplication : PiProcess
   {
      public Node Action
      {
         get { return childNodes[0]; }
         set { childNodes[0] = value; }
      }

      public Node PiProcess
      {
         get { return childNodes[1]; }
         set { childNodes[1] = value; }
      }

      public PiReplication()
         : base(Pos.Empty) { childNodes.Add(null); childNodes.Add(null); }

      public PiReplication(Pos pos, Node action, Node piprocess)
         : base(pos)
      {
         childNodes.Add(action);
         childNodes.Add(piprocess);
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
         sb.Append("rep ");
         childNodes[0].GenerateText(sb);
         sb.Append(".");
         childNodes[1].GenerateText(sb);
      }

      public override Node Clone()
      {
         return new PiReplication(Position, ((PiProcess)childNodes[0]).Clone(), ((PiProcess)childNodes[1]).Clone());
      }
   }

   public class PiIfThenNode : PiProcess
   {
      public PiIfThenNode(Pos pos, Node expression, Node piprocess)
         : base(pos)
      {
         childNodes.Add(expression);
         childNodes.Add(piprocess);
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
         sb.Append("if ( ");
         childNodes[0].GenerateText(sb);
         sb.Append(" )  then ( ");
         childNodes[1].GenerateText(sb);
         sb.Append(" ) endif");
      }

      public override Node Clone()
      {
         return new PiIfThenNode(Position, ((PiProcess)childNodes[0]).Clone(), ((PiProcess)childNodes[1]).Clone());
      }
   }

}

